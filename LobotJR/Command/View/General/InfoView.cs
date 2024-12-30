using LobotJR.Command.Controller.AccessControl;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.General
{
    /// <summary>
    /// View containing help and information commands.
    /// </summary>
    public class InfoView : ICommandView
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly BugReportController BugController;
        private readonly EquipmentController EquipmentController;
        private readonly PlayerController PlayerController;
        private readonly PetController PetController;
        private readonly FishingController FishingController;
        private readonly AccessControlController AccessControlController;
        private readonly TournamentController TournamentController;
        private readonly GroupFinderController GroupFinderController;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Info";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public InfoView(BugReportController bugController, EquipmentController equipmentController, PlayerController playerController, PetController petController, FishingController fishingController, AccessControlController accessControlController, TournamentController tournamentController, GroupFinderController groupFinderController, SettingsManager settingsManager)
        {
            SettingsManager = settingsManager;
            EquipmentController = equipmentController;
            PlayerController = playerController;
            PetController = petController;
            FishingController = fishingController;
            AccessControlController = accessControlController;
            TournamentController = tournamentController;
            GroupFinderController = groupFinderController;
            BugController = bugController;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("Help", this, CommandMethod.GetInfo(Help), "help", "faq"),
                new CommandHandler("WolfcoinInfo", this, CommandMethod.GetInfo(WolfcoinInfo), "1"),
                new CommandHandler("LevelInfo", this, CommandMethod.GetInfo(LevelInfo), "2"),
                new CommandHandler("PetInfo", this, CommandMethod.GetInfo(PetInfo), "3", "pethelp"),
                new CommandHandler("ShopInfo", this, CommandMethod.GetInfo(ShopInfo), "shop"),
                new CommandHandler("BugReport", new CommandExecutor(this,CommandMethod.GetInfo<string>(ReportBug), true), "bug"),
                new CommandHandler("ClientData", this, CommandMethod.GetInfo(FetchClientData), "fetch-client-data")
            };
        }

        public CommandResult Help()
        {
            return new CommandResult(
                "Hi I'm LobotJR! I'm a chat bot written by LobosJR to help out with things.  To ask me about a certain topic, whisper me the number next to what you want to know about! (Ex: Whisper me !1 for information on Wolfcoins)",
                "Here's a list of things you can ask me about: Wolfcoins (!1) - Leveling System (!2) - Pet System (!3)"
                );
        }

        public CommandResult WolfcoinInfo()
        {
            return new CommandResult("Wolfcoins are a currency you earn by watching the stream! You can check your coins by whispering me '!coins' or '!stats'. To find out what you can spend coins on, message me '!shop'.");
        }

        public CommandResult LevelInfo()
        {
            return new CommandResult("Did you know you gain experience by watching the stream? You can level up as you get more XP! Max level is 20. To check your level & xp, message me '!xp' '!level' or '!stats'. Only Level 2+ viewers can post links. This helps prevent bot spam!");
        }

        public CommandResult PetInfo()
        {
            return new CommandResult("View all your pets by whispering me '!pets'. View individual pet stats using '!pet <stable id>' where the id is the number next to your pet's name in brackets [].",
                "A summoned/active pet will join you on dungeon runs and possibly even bring benefits! But this will drain its energy, which you can restore by feeding it.",
                "You can !dismiss, !summon, !release, and !feed your pets using their stable id (ex: !summon 2)"
                );
        }

        public CommandResult ShopInfo()
        {
            var settings = SettingsManager.GetGameSettings();
            return new CommandResult($"Whisper me '!stats <username>' to check another users stats! (Cost: {settings.PryCost} coin)   Whisper me '!gloat' to spend {settings.LevelGloatCost} coins and show off your level! (Cost: {settings.LevelGloatCost} coins)");
        }

        public CommandResult ReportBug(User user, string message)
        {
            BugController.SubmitReport(user, message);
            Logger.Warn(">>{user}: A bug has been reported. {message}", user.Username, message);
            return new CommandResult("Bug report submitted");
        }

        private string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("|", "\\p").Replace(";", "\\s").Replace("&", "\\a");
        }

        public CommandResult FetchClientData(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            var qualities = string.Join(";", EquipmentController.GetItemQualities().Select(x => $"{x.Id}|{Escape(x.Name)}|{x.Color}"));
            var types = string.Join(";", EquipmentController.GetItemTypes().Select(x => $"{x.Id}|{Escape(x.Name)}"));
            var slots = string.Join(";", EquipmentController.GetItemSlots().Select(x => $"{x.Id}|{Escape(x.Name)}|{x.MaxEquipped}"));
            var items = string.Join(";", EquipmentController.GetAllItems().Select(x => $"{x.Id}|{Escape(x.Name)}|{Escape(x.Description)}|{x.Max}|{x.SuccessChance:N}|{x.XpBonus:N}|{x.CoinBonus:N}|{x.ItemFind:N}|{x.PreventDeathBonus:N}|{x.QualityId}|{x.TypeId}|{x.TypeId}"));
            var inventory = string.Join(";", EquipmentController.GetInventoryByUser(user).Select(x => $"{x.ItemId}|{x.Count}|{(x.IsEquipped ? "E" : "U")}"));
            var classes = string.Join(";", PlayerController.GetPlayableClasses().Select(x => $"{x.Id}|{x.Name}|{x.SuccessChance:N}|{x.XpBonus:N}|{x.CoinBonus:N}|{x.ItemFind}|{x.PreventDeathBonus}"));
            var equips = string.Join(";", string.Join("|", PlayerController.GetClassEquippables().Select(x => $"{x.Key}:{string.Join(",", x.Value)}")));
            var rarities = string.Join(";", PetController.GetRarities().Select(x => $"{x.Id}|{x.Name}|{x.Color}"));
            var pets = string.Join(";", PetController.GetPets().Select(x => $"{x.Id}|{x.Name}|{x.Description}|{x.RarityId}"));
            var stable = string.Join(";", PetController.GetStableForUser(user).Select((x, i) => $"{i}|{x.PetId}|{x.Name}|{(x.IsSparkly ? "S" : "")}|{x.Level}|{x.Experience}|{x.Affection}|{x.Hunger}|{(x.IsActive ? "A" : "")}"));
            var fishRarities = string.Join(";", FishingController.GetRarities().Select(x => $"{x.Id}|{x.Name}"));
            var fishSizes = string.Join(";", FishingController.GetSizes().Select(x => $"{x.Id}|{x.Name}|{x.Message}"));
            var fish = string.Join(";", FishingController.GetAllFish().Select(x => $"{x.Id}|{x.Name}|{x.FlavorText}|{x.RarityId}|{x.SizeCategoryId}"));
            var roles = string.Join(";", AccessControlController.GetEnrolledGroups(user).Select(x => $"{x.Name}"));
            var tournamentTime = TournamentController.NextTournament == null ? "-" : (TournamentController.NextTournament.Value - DateTime.Now).ToString("c");
            var settings = SettingsManager.GetGameSettings();
            var tournamentData = $"{tournamentTime};{settings.FishingTournamentInterval}";
            var timerData = GroupFinderController.GetLockoutTime(player).ToString("c");
            var toAdd = new List<string>() { qualities, types, slots, items, inventory, classes, equips, rarities, pets, stable, fishRarities, fishSizes, fish, roles, tournamentData, timerData };
            var output = new CommandResult(true);
            var message = "";
            foreach (var item in toAdd)
            {
                if (message.Length + item.Length > 10000)
                {
                    output.Responses.Add(message);
                    message = "";
                }
                if (message.Length != 0)
                {
                    message += "&";
                }
                message += item;
            }
            if (message.Length > 0)
            {
                output.Responses.Add(message);
            }
            if (player.Level < 2)   // This might be the first whisper we've sent this user, so send a short message first to increase the character limit.
            {
                output.Responses.Insert(0, "Welcome to the Wolfpack RPG!");
            }
            return output;
        }
    }
}
