using LobotJR.Command.Controller.Equipment;
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
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Info";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public InfoView(BugReportController bugController, EquipmentController equipmentController, PlayerController playerController, PetController petController, SettingsManager settingsManager)
        {
            SettingsManager = settingsManager;
            EquipmentController = equipmentController;
            PlayerController = playerController;
            PetController = petController;
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
            return s.Replace("\\", "\\\\").Replace("|", "\\p").Replace(";", "\\s").Replace("&", "\\a").Trim();
        }

        private string Format(double number, double multiply = 100)
        {
            number *= multiply;
            if (number == 0)
            {
                return "";
            }
            if (number == Math.Floor(number))
            {
                return ((int)number).ToString("D");
            }
            var rounded = number.ToString("N2");
            if (rounded.EndsWith(".00"))
            {
                return ((int)number).ToString("D");
            }
            return number.ToString("N2");
        }

        public CommandResult FetchClientData()
        {
            var limit = 500;
            var settings = SettingsManager.GetGameSettings();
            var qualities = EquipmentController.GetItemQualities().Select(x => $"{x.Id}|{Escape(x.Name)}|{x.Color}");
            var types = EquipmentController.GetItemTypes().Select(x => $"{x.Id}|{Escape(x.Name)}");
            var slots = EquipmentController.GetItemSlots().Select(x => $"{x.Id}|{Escape(x.Name)}|{x.MaxEquipped}");
            var classes = PlayerController.GetPlayableClasses().Select(x => $"{x.Id - 1}|{x.Name}|{Format(x.SuccessChance)}|{Format(x.XpBonus)}|{Format(x.CoinBonus)}|{Format(x.ItemFind)}|{Format(x.PreventDeathBonus)}");
            var equips = new string[] { string.Join("|", PlayerController.GetClassEquippables().Select(x => $"{x.Key}:{string.Join(",", x.Value)}")) };
            var rarities = PetController.GetRarities().Select(x => $"{x.Id}|{Escape(x.Name)}|{x.Color}");
            var toAdd = new List<IEnumerable<string>>() { qualities, types, slots, classes, equips, rarities };
            var output = new CommandResult(true);
            var message = "cd: ";
            foreach (var group in toAdd)
            {
                if (message.Length != 4)
                {
                    message += "&";
                }
                var startLength = message.Length;
                foreach (var item in group)
                {
                    if (message.Length + item.Length > limit)
                    {
                        output.Responses.Add(message);
                        message = "cd: ";
                        startLength = message.Length;
                    }
                    if (message.Length != startLength)
                    {
                        message += ";";
                    }
                    message += item;
                }
            }
            if (message.Length > 4)
            {
                output.Responses.Add(message);
            }
            return output;
        }
    }
}
