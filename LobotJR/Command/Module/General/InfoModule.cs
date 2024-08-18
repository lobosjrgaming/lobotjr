using LobotJR.Command.System.General;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System.Collections.Generic;

namespace LobotJR.Command.Module.General
{
    /// <summary>
    /// Module containing help and information commands.
    /// </summary>
    public class InfoModule : ICommandModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly BugReportSystem BugSystem;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Info";
        /// <summary>
        /// This module does not issue any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public InfoModule(BugReportSystem bugSystem, SettingsManager settingsManager)
        {
            SettingsManager = settingsManager;
            BugSystem = bugSystem;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("Help", this, CommandMethod.GetInfo(Help), "help", "faq"),
                new CommandHandler("WolfcoinInfo", this, CommandMethod.GetInfo(WolfcoinInfo), "1"),
                new CommandHandler("LevelInfo", this, CommandMethod.GetInfo(LevelInfo), "2"),
                new CommandHandler("PetInfo", this, CommandMethod.GetInfo(LevelInfo), "3", "pethelp"),
                new CommandHandler("ShopInfo", this, CommandMethod.GetInfo(ShopInfo), "shop"),
                new CommandHandler("BugReport", new CommandExecutor(this,CommandMethod.GetInfo<string>(ReportBug), true), "bug")
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
            BugSystem.SubmitReport(user, message);
            Logger.Warn(">>{user}: A bug has been reported. {message}", user.Username, message);
            return new CommandResult("Bug report submitted");
        }
    }
}
