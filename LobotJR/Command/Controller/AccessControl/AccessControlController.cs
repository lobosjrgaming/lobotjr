using LobotJR.Command.Model.AccessControl;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Controller.AccessControl
{
    /// <summary>
    /// Controller for access management functions.
    /// </summary>
    public class AccessControlController : IMetaController
    {
        private readonly IConnectionManager ConnectionManager;

        /// <summary>
        /// Entry point to inject fully resolved command manager into the
        /// controller.
        public ICommandManager CommandManager { get; set; }

        public AccessControlController(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        /// <summary>
        /// Creates a new empty access group.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>The newly-created group.</returns>
        public AccessGroup CreateGroup(string groupName)
        {
            if (!ConnectionManager.CurrentConnection.AccessGroups.Read(x => x.Name.Equals(groupName)).Any())
            {
                var newGroup = new AccessGroup() { Name = groupName };
                ConnectionManager.CurrentConnection.AccessGroups.Create(newGroup);
                return newGroup;
            }
            return null;
        }

        /// <summary>
        /// Gets all access groups that currently exist.
        /// </summary>
        /// <returns>A collection of access groups.</returns>
        public IEnumerable<AccessGroup> GetAllGroups()
        {
            return ConnectionManager.CurrentConnection.AccessGroups.Read();
        }

        /// <summary>
        /// Checks if an access group with a specified name exists.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>True if a group with that name exists.</returns>
        public bool DoesGroupExist(string groupName)
        {
            return ConnectionManager.CurrentConnection.AccessGroups.Read(x => x.Name.Equals(groupName)).Any();
        }

        /// <summary>
        /// Gets an access group with a given name.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>The access group with that name, or null if none exists.</returns>
        public AccessGroup GetGroupByName(string groupName)
        {
            return ConnectionManager.CurrentConnection.AccessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
        }

        /// <summary>
        /// Gets all user enrollemnts for an access group.
        /// </summary>
        /// <param name="accessGroup"></param>
        /// <returns>A collection of all enrollments tied to this group.</returns>
        public IEnumerable<Enrollment> GetGroupEnrollments(AccessGroup accessGroup)
        {
            return ConnectionManager.CurrentConnection.Enrollments.Read(x => x.GroupId.Equals(accessGroup.Id));
        }

        /// <summary>
        /// Gets all command restrictions for an access group.
        /// </summary>
        /// <param name="accessGroup">An access group.</param>
        /// <returns>A collection of all restrictions tied to this group.</returns>
        public IEnumerable<Restriction> GetGroupRestrictions(AccessGroup accessGroup)
        {
            return ConnectionManager.CurrentConnection.Restrictions.Read(x => x.GroupId.Equals(accessGroup.Id));
        }

        /// <summary>
        /// Deletes an access group. Any restrictions and enrollments for that
        /// group are also deleted.
        /// </summary>
        /// <param name="accessGroup">The group to delete.</param>
        public void DeleteGroup(AccessGroup accessGroup)
        {
            var restrictions = GetGroupRestrictions(accessGroup);
            foreach (var restriction in restrictions)
            {
                ConnectionManager.CurrentConnection.Restrictions.Delete(restriction);
            }
            var enrollments = GetGroupEnrollments(accessGroup);
            foreach (var enrollment in enrollments)
            {
                ConnectionManager.CurrentConnection.Enrollments.Delete(enrollment);
            }
            ConnectionManager.CurrentConnection.AccessGroups.Delete(accessGroup);
        }

        /// <summary>
        /// Enrolls a user in a group.
        /// </summary>
        /// <param name="accessGroup">The access group to add to.</param>
        /// <param name="user">The user to add.</param>
        /// <returns>True if the user was enrolled in this group. False if the
        /// user was already enrolled in the group.</returns>
        public bool EnrollUserInGroup(AccessGroup accessGroup, User user)
        {
            if (!ConnectionManager.CurrentConnection.Enrollments.Read(x => x.Group.Equals(accessGroup) && x.UserId.Equals(user.TwitchId)).Any())
            {
                var enrollment = new Enrollment(accessGroup, user.TwitchId);
                ConnectionManager.CurrentConnection.Enrollments.Create(enrollment);
            }
            return false;
        }

        /// <summary>
        /// Unenrolls a user from a group.
        /// </summary>
        /// <param name="accessGroup">The access group to remove from.</param>
        /// <param name="user">The user to remove.</param>
        /// <returns>True if the user was unenrolled from this group. False if
        /// the user was not enrolled in the group.</returns>
        public bool UnenrollUserFromGroup(AccessGroup accessGroup, User user)
        {
            var enrollment = ConnectionManager.CurrentConnection.Enrollments.Read(x => x.Group.Equals(accessGroup) && x.UserId.Equals(user.TwitchId)).FirstOrDefault();
            if (enrollment != null)
            {
                ConnectionManager.CurrentConnection.Enrollments.Delete(enrollment);
            }
            return false;
        }

        /// <summary>
        /// Restricts a command to a group.
        /// </summary>
        /// <param name="accessGroup">The access group to add to.</param>
        /// <param name="command">The command to add.</param>
        /// <returns>True if the command was restricted to this group. False if
        /// the command was already restricted to the group, or was not a valid
        /// command string.</returns>
        public bool RestrictCommandToGroup(AccessGroup accessGroup, string command)
        {
            if (!ConnectionManager.CurrentConnection.Restrictions.Read(x => x.Group.Equals(accessGroup) && x.Command.Equals(command)).Any())
            {
                if (CommandManager.IsValidCommand(command))
                {
                    var enrollment = new Restriction(accessGroup, command);
                    ConnectionManager.CurrentConnection.Restrictions.Create(enrollment);
                }
            }
            return false;
        }

        /// <summary>
        /// Unrestricts a command from a group.
        /// </summary>
        /// <param name="accessGroup">The access group to remove from.</param>
        /// <param name="command">The command to remove.</param>
        /// <returns>True if the command was unrestricted from this group.
        /// False if the command was not restricted to the group.</returns>
        public bool UnrestrictCommandFromGroup(AccessGroup accessGroup, string command)
        {
            var restriction = ConnectionManager.CurrentConnection.Restrictions.Read(x => x.Group.Equals(accessGroup) && x.Command.Equals(command)).FirstOrDefault();
            if (restriction != null)
            {
                ConnectionManager.CurrentConnection.Restrictions.Delete(restriction);
            }
            return false;
        }

        /// <summary>
        /// Checks a command string to see if it is valid. A valid command
        /// string either exactly matches the id of a command or is a wildcard
        /// pattern that matches one or more commands.
        /// </summary>
        /// <param name="command">The command string to check.</param>
        /// <returns>True if the command string is valid.</returns>
        public bool IsValidCommand(string command)
        {
            return CommandManager.IsValidCommand(command);
        }

        /// <summary>
        /// Gets a collection of all of the commands loaded into the command
        /// manager.
        /// </summary>
        /// <returns>A collection of command ids.</returns>
        public IEnumerable<string> GetAllCommands()
        {
            return CommandManager.Commands;
        }
    }
}
