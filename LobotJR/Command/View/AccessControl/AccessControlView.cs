using LobotJR.Command.Controller.AccessControl;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.AccessControl
{
    /// <summary>
    /// View containing commands for checking access to other commands.
    /// </summary>
    public class AccessControlView : ICommandView
    {
        private readonly AccessControlController Controller;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "AccessControl";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public AccessControlView(AccessControlController accessControlController)
        {
            Controller = accessControlController;
            Commands = new CommandHandler[]
            {
                new CommandHandler("CheckAccess", this, CommandMethod.GetInfo<string>(CheckAccess), "CheckAccess", "check-access"),
            };
        }

        private CommandResult CheckAccess(User user, string groupName = "")
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                var groups = Controller.GetEnrolledGroups(user);
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

            var group = Controller.GetGroupByName(groupName);
            if (group == null)
            {
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }

            var flagAccess = (group.IncludeSubs && user.IsSub) || (group.IncludeVips && user.IsVip) || (group.IncludeMods && user.IsMod) || (group.IncludeAdmins && user.IsAdmin);
            var enrollAccess = group.Enrollments.Any(x => x.UserId.Equals(user.TwitchId));
            var access = (enrollAccess || flagAccess) ? "are" : "are not";
            return new CommandResult($"You {access} a member of \"{group.Name}\"!");
        }
    }
}
