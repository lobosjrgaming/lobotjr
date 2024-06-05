using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.AccessControl
{
    /// <summary>
    /// Module containing commands for checking access to other commands.
    /// </summary>
    public class AccessControlModule : ICommandModule
    {
        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "AccessControl";
        /// <summary>
        /// This module does not issue any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public AccessControlModule()
        {
            Commands = new CommandHandler[]
            {
                new CommandHandler("CheckAccess", this, CommandMethod.GetInfo<string>(CheckAccess), "CheckAccess", "check-access"),
            };
        }

        private CommandResult CheckAccess(IDatabase database, User user, string groupName = "")
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                var groupIds = database.Enrollments.Read(x => x.UserId.Equals(user.TwitchId, StringComparison.OrdinalIgnoreCase)).Select(x => x.GroupId).ToList();
                var groups = database.AccessGroups.Read(x => groupIds.Contains(x.Id));
                if (groups.Any())
                {
                    var count = groups.Count();
                    return new CommandResult($"You are a member of the following group{(count == 1 ? "" : "s")}: {string.Join(", ", groups.Select(x => x.Name))}.");
                }
                else
                {
                    return new CommandResult("You are not a member of any groups.");
                }
            }

            var group = database.AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }

            var access = database.Enrollments.Read(x => x.GroupId == group.Id && x.UserId.Equals(user.TwitchId, StringComparison.OrdinalIgnoreCase)).Any() ? "are" : "are not";
            return new CommandResult($"You {access} a member of \"{group.Name}\"!");
        }
    }
}
