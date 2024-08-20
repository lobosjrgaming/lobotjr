using LobotJR.Command.Controller.Twitch;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;

namespace LobotJR.Command.Module.Twitch
{
    /// <summary>
    /// Module containing commands for managing the user database.
    /// </summary>
    public class UserModule : ICommandModule
    {
        private readonly UserController UserSystem;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "User.Admin";
        /// <summary>
        /// This module does not send any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public UserModule(UserController userSystem, SettingsManager settingsManager)
        {
            UserSystem = userSystem;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("UpdateViewers", this, CommandMethod.GetInfo(UpdateViewers), "updateviewers")
            };
        }

        public CommandResult UpdateViewers()
        {
            var settings = SettingsManager.GetAppSettings();
            UserSystem.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime);
            return new CommandResult($"Viewer update triggered.");
        }
    }
}
