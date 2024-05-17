using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.AccessControl
{
    /// <summary>
    /// Module of access control admin commands.
    /// </summary>
    public class AccessControlAdmin : ICommandModule
    {
        //private readonly ICommandManager CommandManager;
        private readonly IRepository<AccessGroup> AccessGroups;
        private readonly IRepository<Enrollment> Enrollments;
        private readonly IRepository<Restriction> Restrictions;
        private readonly UserSystem UserSystem;

        public ICommandManager CommandManager;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "AccessControl.Admin";

        /// <summary>
        /// This module does not issue any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;

        /// <summary>
        /// A collection of commands for managing access to commands.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public AccessControlAdmin(IRepositoryManager repositoryManager, UserSystem userSystem)
        {
            AccessGroups = repositoryManager.AccessGroups;
            Enrollments = repositoryManager.Enrollments;
            Restrictions = repositoryManager.Restrictions;
            UserSystem = userSystem;
            Commands = new CommandHandler[]
            {
                new CommandHandler("ListGroups", this, CommandMethod.GetInfo(ListGroups), "ListGroups", "list-groups", "ListRoles", "list-roles"),
                new CommandHandler("CreateGroup", this, CommandMethod.GetInfo<string>(CreateGroup), "CreateGroup", "create-group", "CreateRole", "create-role"),
                new CommandHandler("DescribeGroup", this, CommandMethod.GetInfo<string>(DescribeGroup), "DescribeGroup", "describe-group", "DescribeRole", "describe-role"),
                new CommandHandler("DeleteGroup", this, CommandMethod.GetInfo<string>(DeleteGroup), "DeleteGroup", "delete-group", "DeleteRole", "delete-role"),

                new CommandHandler("SetGroupFlag", this, CommandMethod.GetInfo<string, string, bool>(SetGroupFlag), "SetGroupFlag", "set-group-flag"),

                new CommandHandler("EnrollUser", this, CommandMethod.GetInfo<string, string>(AddUserToGroup), "EnrollUser", "enroll-user"),
                new CommandHandler("UnenrollUser", this, CommandMethod.GetInfo<string, string>(RemoveUserFromGroup), "UnenrollUser", "unenroll-user"),

                new CommandHandler("RestrictCommand", this, CommandMethod.GetInfo<string, string>(AddCommandToGroup), "RestrictCommand", "restrict-command"),
                new CommandHandler("ListCommands", this, CommandMethod.GetInfo(ListCommands), "ListCommands", "list-commands"),
                new CommandHandler("UnrestrictCommand", this, CommandMethod.GetInfo<string, string>(RemoveCommandFromGroup), "UnrestrictCommand", "unrestrict-command")
            };
        }

        private CommandResult ListGroups()
        {
            return new CommandResult($"There are {AccessGroups.Read().Count()} groups: {string.Join(", ", AccessGroups.Read().Select(x => x.Name))}");
        }

        private CommandResult CreateGroup(string groupName)
        {
            var existingGroup = AccessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup != null)
            {
                return new CommandResult($"Error: Unable to create group, \"{groupName}\" already exists.");
            }

            AccessGroups.Create(new AccessGroup() { Name = groupName });
            AccessGroups.Commit();
            return new CommandResult($"Access group \"{groupName}\" created successfully!");
        }

        private CommandResult DescribeGroup(string groupName)
        {
            var existingGroup = AccessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult($"Error: Group \"{groupName}\" not found.");
            }
            var enrollments = Enrollments.Read(x => x.GroupId.Equals(existingGroup.Id));
            var restrictions = Restrictions.Read(x => x.GroupId.Equals(existingGroup.Id));
            var names = new List<string>();
            if (existingGroup.IncludeAdmins)
            {
                names.Add("Admins");
            }
            if (existingGroup.IncludeMods)
            {
                names.Add("Mods");
            }
            if (existingGroup.IncludeVips)
            {
                names.Add("VIPs");
            }
            if (existingGroup.IncludeSubs)
            {
                names.Add("Subs");
            }
            foreach (var enrollment in enrollments)
            {
                var enrolledUser = UserSystem.GetUserById(enrollment.UserId);
                if (enrolledUser != null)
                {
                    names.Add(enrolledUser.Username);
                }
            }
            return new CommandResult(
                $"Access group \"{groupName}\" contains the following commands: {string.Join(", ", restrictions.Select(x => x.Command))}.",
                $"Access group \"{groupName}\" contains the following users: {string.Join(", ", names)}."
            );
        }

        private CommandResult DeleteGroup(string groupName)
        {
            var existingGroup = AccessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult($"Error: Unable to delete group, \"{groupName}\" does not exist.");
            }

            var enrollments = Enrollments.Read(x => x.GroupId.Equals(existingGroup.Id));
            if (enrollments.Any())
            {
                return new CommandResult($"Error: Unable to delete group, please unenroll all users first.");
            }

            var restrictions = Restrictions.Read(x => x.GroupId.Equals(existingGroup.Id));
            if (restrictions.Any())
            {
                return new CommandResult($"Error: Unable to delete group, please unrestrict all commands first.");
            }

            AccessGroups.Delete(existingGroup);
            AccessGroups.Commit();
            return new CommandResult($"Group \"{groupName}\" deleted successfully!");
        }

        private CommandResult SetGroupFlag(string groupName, string flag, bool value)
        {
            var existingGroup = AccessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult($"Error: Group \"{groupName}\" not found.");
            }

            if (flag.Equals("mod", StringComparison.OrdinalIgnoreCase))
            {
                existingGroup.IncludeMods = value;
            }
            else if (flag.Equals("vip", StringComparison.OrdinalIgnoreCase))
            {
                existingGroup.IncludeVips = value;
            }
            else if (flag.Equals("sub", StringComparison.OrdinalIgnoreCase))
            {
                existingGroup.IncludeSubs = value;
            }
            else if (flag.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                existingGroup.IncludeAdmins = value;
            }
            else
            {
                return new CommandResult($"Error: Invalid flag, must be one of \"mod\", \"vip\", \"sub\", or \"admin\".");
            }
            AccessGroups.Update(existingGroup);
            AccessGroups.Commit();
            var includeClause = value ? "includes" : "does not include";
            return new CommandResult($"Access group \"{existingGroup.Name}\" now {includeClause} {flag}s.");
        }

        private CommandResult AddUserToGroup(string username, string groupName)
        {
            var userToAdd = UserSystem.GetUserByName(username);
            if (userToAdd == null)
            {
                return new CommandResult("Error: User id not present in id cache, please try again in a few minutes.");
            }

            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }

            var enrollments = Enrollments.Read(x => x.GroupId.Equals(group.Id));
            if (enrollments.Any(x => userToAdd.TwitchId.Equals(x.UserId)))
            {
                return new CommandResult($"Error: User \"{username}\" is already a member of \"{groupName}\".");
            }
            Enrollments.Create(new Enrollment(group, userToAdd.TwitchId));
            Enrollments.Commit();

            return new CommandResult($"User \"{username}\" was added to group \"{group.Name}\" successfully!");
        }

        private CommandResult RemoveUserFromGroup(string username, string groupName)
        {
            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }

            var userToRemove = UserSystem.GetUserByName(username);
            if (userToRemove == null)
            {
                return new CommandResult($"Error: User \"{username}\" not found in user database. Please ensure the name is correct and the user has been in chat before.");
            }

            var enrollment = Enrollments.Read(x => x.GroupId.Equals(group.Id) && x.UserId.Equals(userToRemove.TwitchId)).FirstOrDefault();
            if (enrollment == null)
            {
                return new CommandResult($"Error: User \"{username}\" is not a member of \"{groupName}\".");
            }
            Enrollments.Delete(enrollment);
            Enrollments.Commit();

            return new CommandResult($"User \"{username}\" was removed from group \"{group.Name}\" successfully!");
        }

        private CommandResult AddCommandToGroup(string commandName, string groupName)
        {
            if (!CommandManager.IsValidCommand(commandName))
            {
                return new CommandResult($"Error: Command {commandName} does not match any commands.");
            }

            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: Group \"{groupName}\" does not exist.");
            }

            var restrictions = Restrictions.Read(x => x.GroupId == group.Id && x.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (restrictions.Any())
            {
                return new CommandResult($"Error: \"{groupName}\" already has access to \"{commandName}\".");
            }

            Restrictions.Create(new Restriction() { GroupId = group.Id, Command = commandName });
            Restrictions.Commit();
            return new CommandResult($"Command \"{commandName}\" was added to the group \"{group.Name}\" successfully!");
        }

        private CommandResult ListCommands()
        {
            var commands = CommandManager.Commands;
            var modules = commands.Where(x => x.LastIndexOf('.') != -1).Select(x => x.Substring(0, x.LastIndexOf('.'))).Distinct().ToList();
            var response = new string[modules.Count + 1];
            response[0] = $"There are {commands.Count()} commands across {modules.Count} modules.";
            for (var i = 0; i < modules.Count; i++)
            {
                response[i + 1] = $"{modules[i]}: {string.Join(", ", commands.Where(x => x.StartsWith(modules[i])))}";
            }
            return new CommandResult(response);
        }

        private CommandResult RemoveCommandFromGroup(string commandName, string groupName)
        {
            if (!CommandManager.IsValidCommand(commandName))
            {
                return new CommandResult($"Error: Command {commandName} does not match any commands.");
            }

            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: Group \"{groupName}\" does not exist.");
            }

            var restrictions = Restrictions.Read(x => x.GroupId == group.Id && x.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (!restrictions.Any())
            {
                return new CommandResult($"Error: \"{groupName}\" doesn't have access to \"{commandName}\".");
            }

            var toRemove = restrictions.First();
            Restrictions.Delete(toRemove);
            Restrictions.Commit();

            return new CommandResult($"Command \"{commandName}\" was removed from group \"{group.Name}\" successfully!");
        }
    }
}
