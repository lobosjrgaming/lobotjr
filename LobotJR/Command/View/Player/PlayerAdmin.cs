using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Player
{
    /// <summary>
    /// View containing commands for managing player experience.
    /// </summary>
    public class PlayerAdmin : ICommandView
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly PlayerController PlayerController;
        private readonly EquipmentController EquipmentController;
        private readonly UserController UserController;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Player.Admin";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public PlayerAdmin(PlayerController playerController, EquipmentController equipmentController, UserController userController, SettingsManager settingsManager)
        {
            PlayerController = playerController;
            EquipmentController = equipmentController;
            UserController = userController;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("GiveExperienceToUser", this, CommandMethod.GetInfo<string, int>(GiveExperienceToUser), "givexp", "giveexperience") { WhisperOnly = false },
                new CommandHandler("GiveExperienceToAll", this, CommandMethod.GetInfo<int>(GiveExperienceToAll), "grantxp") { WhisperOnly = false },
                new CommandHandler("SetExperience", this, CommandMethod.GetInfo<string, int>(SetExperience), "setxp", "setexperience") { WhisperOnly = false },
                new CommandHandler("SetPrestige", this, CommandMethod.GetInfo<string, int>(SetPrestige), "setprestige"),
                new CommandHandler("GiveCoins", this, CommandMethod.GetInfo<string, int>(GiveCoins), "givecoins", "addcoins") { WhisperOnly = false },
                new CommandHandler("SetCoins", this, CommandMethod.GetInfo<string, int>(SetCoins), "setcoins") { WhisperOnly = false },
                new CommandHandler("RemoveCoins", this, CommandMethod.GetInfo<string, int>(RemoveCoins), "removecoins") { WhisperOnly = false },
                new CommandHandler("ResetPlayer", this, CommandMethod.GetInfo<string>(ResetPlayer), "debuglevel5", "resetplayer"),
                new CommandHandler("ClearClass", this, CommandMethod.GetInfo<string>(ClearClass), "clearclass"),

                new CommandHandler("GodMode", this, CommandMethod.GetInfo<string>(EnableGodMode), "godmode"),
                new CommandHandler("SetInterval", this, CommandMethod.GetInfo<int>(SetInterval), "setinterval") { WhisperOnly = false },
                new CommandHandler("SetMultiplier", this, CommandMethod.GetInfo<int>(SetMultiplier), "setmultiplier") { WhisperOnly = false } ,

                new CommandHandler("ExperienceOn", new CommandExecutor(this, CommandMethod.GetInfo<string>(EnableExperience), true), "xpon") { WhisperOnly = false },
                new CommandHandler("ExperienceOff", new CommandExecutor(this, CommandMethod.GetInfo<string>(DisableExperience), true), "xpoff") { WhisperOnly = false },

                new CommandHandler("NextAward", this, CommandMethod.GetInfo(PrintNextAward), "nextaward"),
                new CommandHandler("PrintInfo", this, CommandMethod.GetInfo<string>(PrintUserInfo), "printinfo"),

                new CommandHandler("ImportFix", this, CommandMethod.GetInfo(ImportFix), "importfix", "import-fix"),
            };
        }

        private CommandResult CreateDefaultResult(string user)
        {
            return new CommandResult($"Unable to find player record for user {user}.");
        }

        public CommandResult GiveExperienceToUser(string username, int amount)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                PlayerController.GainExperience(targetUser, targetPlayer, amount);
                return new CommandResult($"Gave {amount} XP to {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult GiveExperienceToAll(int amount)
        {
            var viewers = UserController.GetViewerList().GetAwaiter().GetResult();
            foreach (var user in viewers)
            {
                var player = PlayerController.GetPlayerByUser(user);
                PlayerController.GainExperience(user, player, amount);
            }
            return new CommandResult($"Gave {amount} experience to {viewers.Count()} viewers.");
        }

        public CommandResult SetExperience(string username, int value)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                PlayerController.GainExperience(targetUser, targetPlayer, value - targetPlayer.Experience);
                return new CommandResult($"Set experience to {targetPlayer.Experience} for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult SetPrestige(string username, int value)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                targetPlayer.Prestige = value;
                return new CommandResult($"Set prestige to {targetPlayer.Prestige} for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult GiveCoins(string username, int amount)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                targetPlayer.Currency += amount;
                return new CommandResult($"Gave {amount} Wolfcoins to {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult SetCoins(string username, int value)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                targetPlayer.Currency = value;
                return new CommandResult($"Set Wolfcoins to {value} for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult RemoveCoins(string username, int amount)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                targetPlayer.Currency = Math.Max(0, targetPlayer.Currency - amount);
                return new CommandResult($"Removed {amount} Wolfcoins from {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult ResetPlayer(string username)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                PlayerController.ClearClass(targetPlayer);
                targetPlayer.Experience = 1;
                targetPlayer.Level = 1;
                PlayerController.GainExperience(targetUser, targetPlayer, 599);
                return new CommandResult($"Cleared class and experience then set level to 5 for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult ClearClass(string username)
        {
            var targetUser = UserController.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                PlayerController.ClearClass(targetPlayer);
                return new CommandResult($"Cleared class for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult EnableGodMode(string username)
        {
            //TODO: God mode just set the player's success chance to 1000,
            // but with how the classes are structured now that's not really possible
            // We either need to add an item to the player, or create a special flag for it
            // Check with Lobos to see if he still has need of this command
            return new CommandResult($"God mode not yet implemented.");
        }

        public CommandResult SetInterval(int interval)
        {
            SettingsManager.GetGameSettings().ExperienceFrequency = interval;
            return new CommandResult(true, $"XP & Coins will now be awarded every {interval} minutes.");
        }

        public CommandResult SetMultiplier(int multiplier)
        {
            PlayerController.CurrentMultiplier = multiplier;
            return new CommandResult(true, $"{multiplier}x XP & Coins will now be awarded.");
        }

        //throwaway param is there because sometimes mods like to put memes next to the !xpon and !xpoff commands, this way that won't break them.
        public CommandResult EnableExperience(User user, string throwaway)
        {
            if (PlayerController.AwardsEnabled)
            {
                return new CommandResult(true, $"XP has already been enabled by {PlayerController.AwardSetter.Username}.");
            }
            PlayerController.EnableAwards(user);
            return new CommandResult(true, "Wolfcoins & XP will be awarded.");
        }

        //throwaway param is there because sometimes mods like to put memes next to the !xpon and !xpoff commands, this way that won't break them.
        public CommandResult DisableExperience(string throwaway)
        {
            if (!PlayerController.AwardsEnabled)
            {
                return new CommandResult(true, "XP isn't on.");
            }
            PlayerController.DisableAwards();
            return new CommandResult(true, "Wolfcoins & XP will no longer be awarded.");
        }

        public CommandResult PrintNextAward()
        {
            if (PlayerController.AwardsEnabled)
            {
                var settings = SettingsManager.GetGameSettings();
                var toNext = (PlayerController.LastAward + TimeSpan.FromMinutes(settings.ExperienceFrequency) - DateTime.Now);
                if (toNext > TimeSpan.Zero)
                {
                    return new CommandResult(true, $"{toNext.Minutes} minutes and {toNext.Seconds} seconds until next coins/xp are awarded.");
                }
                return new CommandResult(true, $"Coin/xp awards are overdue, something might be wrong.");
            }
            return new CommandResult(true, "Awards are not currently enabled.");
        }

        public CommandResult PrintUserInfo(string target)
        {
            var targetUser = UserController.GetUserByName(target);
            if (targetUser != null)
            {
                var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                if (targetPlayer != null)
                {
                    Logger.Info($"Name: {targetUser.Username}");
                    Logger.Info($"Level: {targetPlayer.Level}");
                    Logger.Info($"Prestige: {targetPlayer.Prestige}");
                    Logger.Info($"Class: {targetPlayer.CharacterClass.Name}");
                    var successChance = targetPlayer.CharacterClass.SuccessChance + EquipmentController.GetEquippedGear(targetPlayer).Sum(x => x.SuccessChance);
                    Logger.Info($"Dungeon Success Chance: {Math.Round(successChance * 100)}%");
                    Logger.Info($"Number of Items: {EquipmentController.GetInventoryByPlayer(targetPlayer).Count()}");
                }
            }
            return new CommandResult(true);
        }

        public CommandResult ImportFix()
        {
            var recordCount = PlayerController.ImportFix().GetAwaiter().GetResult();
            return new CommandResult($"{recordCount} user records updated");
        }
    }
}
