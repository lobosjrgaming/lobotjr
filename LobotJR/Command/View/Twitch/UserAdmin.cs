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
    public class UserAdmin : ICommandView, IPushNotifier
    {
        private readonly UserController UserController;
        private readonly SettingsManager SettingsManager;


        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "User.Admin";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }
        /// <summary>
        /// Pushes to users that are banned to let them know they were banned.
        /// </summary>
        public event PushNotificationHandler PushNotification;

        public UserAdmin(UserController userController, SettingsManager settingsManager)
        {
            UserController = userController;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("UpdateViewers", this, CommandMethod.GetInfo(UpdateViewers), "updateviewers"),
                new CommandHandler("RpgBan", this, CommandMethod.GetInfo<string, string>(RpgBan), "rpgban", "botban"),
                new CommandHandler("RpgUnban", this, CommandMethod.GetInfo<string>(RpgUnban), "rpgunban", "botunban")
            };
        }

        public CommandResult UpdateViewers()
        {
            var settings = SettingsManager.GetAppSettings();
            UserController.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime);
            return new CommandResult($"Viewer update triggered.");
        }

        public CommandResult RpgBan(string username, string message = "")
        {
            var user = UserController.GetUserByName(username);
            if (user != null)
            {
                if (user.BanTime == null)
                {
                    user.BanTime = DateTime.Now;
                    user.BanMessage = message;
                    string reason = message != null ? $" Reason: {message}." : "";
                    PushNotification?.Invoke(user, new CommandResult($"You have been banned from the Wolfpack RPG.{reason}"));
                    return new CommandResult($"{user.Username} has been banned from the Wolfpack RPG.");
                }
                return new CommandResult($"User {user.Username} is already banned.");
            }
            return new CommandResult($"Unable to find user \"{username}\"");
        }

        public CommandResult RpgUnban(string username)
        {
            var user = UserController.GetUserByName(username);
            if (user != null)
            {
                if (user.BanTime != null)
                {
                    user.BanTime = null;
                    PushNotification?.Invoke(user, new CommandResult($"You have been unbanned from the Wolfpack RPG."));
                    return new CommandResult($"{user.Username}'s ban for the Wolfpack RPG has been lifted.");
                }
                return new CommandResult($"{user.Username} is not banned from the Wolfpack RPG.");
            }
            return new CommandResult($"Unable to find user \"{username}\"");
        }
    }
}
