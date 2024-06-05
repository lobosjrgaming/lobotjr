using LobotJR.Command.System.Player;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.Player
{
    /// <summary>
    /// Module containing commands for managing player experience.
    /// </summary>
    public class PlayerAdmin : ICommandModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly PlayerSystem PlayerSystem;
        private readonly UserSystem UserSystem;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Player.Admin";
        /// <summary>
        /// This module does not send any push notifications. If this module
        /// triggers a level up, that event will be handled by the
        /// PlayerModule.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public PlayerAdmin(PlayerSystem playerSystem, UserSystem userSystem, SettingsManager settingsManager)
        {
            PlayerSystem = playerSystem;
            UserSystem = userSystem;
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

                new CommandHandler("ExperienceOn", this, CommandMethod.GetInfo(EnableExperience), "xpon"),
                new CommandHandler("ExperienceOff", this, CommandMethod.GetInfo(DisableExperience), "xpoff"),

                new CommandHandler("NextAward", this, CommandMethod.GetInfo(PrintNextAward), "nextaward"),
                new CommandHandler("PrintInfo", this, CommandMethod.GetInfo<string>(PrintUserInfo), "printinfo"),
            };
        }

        private CommandResult CreateDefaultResult(string user)
        {
            return new CommandResult($"Unable to find player record for user {user}.");
        }

        public CommandResult GiveExperienceToUser(string username, int amount)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                PlayerSystem.GainExperience(targetUser, targetPlayer, amount);
                return new CommandResult($"Gave {amount} XP to {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult GiveExperienceToAll(int amount)
        {
            foreach (var user in UserSystem.Viewers)
            {
                var player = PlayerSystem.GetPlayerByUser(user);
                PlayerSystem.GainExperience(user, player, amount);
            }
            return new CommandResult($"Gave {amount} experience to {UserSystem.Viewers.Count()} viewers.");
        }

        public CommandResult SetExperience(string username, int value)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                PlayerSystem.GainExperience(targetUser, targetPlayer, value - targetPlayer.Experience);
                return new CommandResult($"Set experience to {targetPlayer.Experience} for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult SetPrestige(string username, int value)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                targetPlayer.Prestige = value;
                return new CommandResult($"Set prestige to {targetPlayer.Prestige} for {targetPlayer.Prestige}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult GiveCoins(string username, int amount)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                targetPlayer.Currency += amount;
                return new CommandResult($"Gave {amount} Wolfcoins to {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult SetCoins(string username, int value)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                targetPlayer.Currency = value;
                return new CommandResult($"Set Wolfcoins to {value} for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult RemoveCoins(string username, int amount)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                targetPlayer.Currency = Math.Max(0, amount);
                return new CommandResult($"Removed {amount} Wolfcoins from {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult ResetPlayer(string username)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                PlayerSystem.ClearClass(targetPlayer);
                targetPlayer.Experience = 1;
                targetPlayer.Level = 1;
                PlayerSystem.GainExperience(targetUser, targetPlayer, 599);
                return new CommandResult($"Cleared class and experience then set level to 5 for {targetUser.Username}.");
            }
            return CreateDefaultResult(username);
        }

        public CommandResult ClearClass(string username)
        {
            var targetUser = UserSystem.GetUserByName(username);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                PlayerSystem.ClearClass(targetPlayer);
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
            PlayerSystem.CurrentMultiplier = multiplier;
            return new CommandResult(true, $"{multiplier}x XP & Coins will now be awarded.");
        }

        public CommandResult EnableExperience(User user)
        {
            if (PlayerSystem.AwardsEnabled)
            {
                return new CommandResult(true, $"XP has already been enabled by {PlayerSystem.AwardSetter.Username}.");
            }
            PlayerSystem.EnableAwards(user);
            return new CommandResult(true, "Wolfcoins & XP will be awarded.");
        }

        public CommandResult DisableExperience()
        {
            if (!PlayerSystem.AwardsEnabled)
            {
                return new CommandResult(true, "Wolfcoins & XP will no longer be awarded.");
            }
            PlayerSystem.DisableAwards();
            return new CommandResult(true, "XP isn't on.");
        }

        public CommandResult PrintNextAward()
        {
            var settings = SettingsManager.GetGameSettings();
            var toNext = (PlayerSystem.LastAward + TimeSpan.FromMinutes(settings.ExperienceFrequency) - DateTime.Now).Value;
            return new CommandResult(true, $"{toNext.Minutes} minutes and {toNext.Seconds} seconds until next coins/xp are awarded.");
        }

        public CommandResult PrintUserInfo(string target)
        {
            var targetUser = UserSystem.GetUserByName(target);
            if (targetUser != null)
            {
                var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                if (targetPlayer != null)
                {
                    Logger.Info($"Name: {targetUser.Username}");
                    Logger.Info($"Level: {targetPlayer.Level}");
                    Logger.Info($"Prestige: {targetPlayer.Prestige}");
                    Logger.Info($"Class: {targetPlayer.CharacterClass.Name}");
                    Logger.Info($"Dungeon Success Chance: {targetPlayer.CharacterClass.SuccessChance}");
                    //TODO: Fix the dungeon success chance to include equipment, add item output
                    // Logger.Info($"Number of Items: {targetPlayer.Level}");
                }
            }
            return new CommandResult(true);
        }
    }
}
