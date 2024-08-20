using LobotJR.Command.Controller.Twitch;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;

namespace LobotJR.Command.View.Twitch
{
    /// <summary>
    /// View containing commands for managing the user database.
    /// </summary>
    public class UserAdmin : ICommandView
    {
        private readonly UserController UserController;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "User.Admin";
        /// <summary>
        /// This view does not send any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public UserAdmin(UserController userController, SettingsManager settingsManager)
        {
            UserController = userController;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("UpdateViewers", this, CommandMethod.GetInfo(UpdateViewers), "updateviewers")
            };
        }

        public CommandResult UpdateViewers()
        {
            var settings = SettingsManager.GetAppSettings();
            UserController.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime);
            return new CommandResult($"Viewer update triggered.");
        }
    }
}
