using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.AccessControl
{
    /// <summary>
    /// View containing commands for checking access to other commands.
    /// </summary>
    public class AccessControlView : ICommandView
    {
        private readonly IConnectionManager ConnectionManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "AccessControl";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public AccessControlView(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
            Commands = new CommandHandler[]
            {
                new CommandHandler("CheckAccess", this, CommandMethod.GetInfo<string>(CheckAccess), "CheckAccess", "check-access"),
            };
        }

        private CommandResult CheckAccess(User user, string groupName = "")
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                var groupIds = ConnectionManager.CurrentConnection.Enrollments.Read(x => x.UserId.Equals(user.TwitchId, StringComparison.OrdinalIgnoreCase)).Select(x => x.GroupId).ToList();
                var groups = ConnectionManager.CurrentConnection.AccessGroups.Read(x => groupIds.Contains(x.Id));
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

            var group = ConnectionManager.CurrentConnection.AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }

            var access = ConnectionManager.CurrentConnection.Enrollments.Read(x => x.GroupId == group.Id && x.UserId.Equals(user.TwitchId, StringComparison.OrdinalIgnoreCase)).Any() ? "are" : "are not";
            return new CommandResult($"You {access} a member of \"{group.Name}\"!");
        }
    }
}
