using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.AccessControl
{
    /// <summary>
    /// Module containing commands for managing access groups.
    /// </summary>
    public class AccessControlAdmin : ICommandModule, IMetaModule
    {
        private readonly UserSystem UserSystem;

        /// <summary>
        /// Entry point to inject fully resolved command manager into the
        /// module.
        public ICommandManager CommandManager { get; set; }

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "AccessControl.Admin";
        /// <summary>
        /// This module does not issue any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public AccessControlAdmin(UserSystem userSystem)
        {
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

        private CommandResult ListGroups(IDatabase database)
        {
            var groups = database.AccessGroups.Read().ToList();
            return new CommandResult($"There are {groups.Count()} groups: {string.Join(", ", groups.Select(x => x.Name))}");
        }

        private CommandResult CreateGroup(IDatabase database, string groupName)
        {
            var accessGroups = database.AccessGroups;
            var existingGroup = accessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup != null)
            {
                return new CommandResult($"Error: Unable to create group, \"{groupName}\" already exists.");
            }

            accessGroups.Create(new AccessGroup() { Name = groupName });
            accessGroups.Commit();
            return new CommandResult($"Access group \"{groupName}\" created successfully!");
        }

        private CommandResult DescribeGroup(IDatabase database, string groupName)
        {
            var existingGroup = database.AccessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult($"Error: Group \"{groupName}\" not found.");
            }
            var enrollments = database.Enrollments.Read(x => x.GroupId.Equals(existingGroup.Id));
            var restrictions = database.Restrictions.Read(x => x.GroupId.Equals(existingGroup.Id));
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

        private CommandResult DeleteGroup(IDatabase database, string groupName)
        {
            var accessGroups = database.AccessGroups;
            var existingGroup = accessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult($"Error: Unable to delete group, \"{groupName}\" does not exist.");
            }

            var enrollments = database.Enrollments.Read(x => x.GroupId.Equals(existingGroup.Id));
            if (enrollments.Any())
            {
                return new CommandResult($"Error: Unable to delete group, please unenroll all users first.");
            }

            var restrictions = database.Restrictions.Read(x => x.GroupId.Equals(existingGroup.Id));
            if (restrictions.Any())
            {
                return new CommandResult($"Error: Unable to delete group, please unrestrict all commands first.");
            }

            accessGroups.Delete(existingGroup);
            accessGroups.Commit();
            return new CommandResult($"Group \"{groupName}\" deleted successfully!");
        }

        private CommandResult SetGroupFlag(IDatabase database, string groupName, string flag, bool value)
        {
            var accessGroups = database.AccessGroups;
            var existingGroup = accessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
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
            accessGroups.Update(existingGroup);
            accessGroups.Commit();
            var includeClause = value ? "includes" : "does not include";
            return new CommandResult($"Access group \"{existingGroup.Name}\" now {includeClause} {flag}s.");
        }

        private CommandResult AddUserToGroup(IDatabase database, string username, string groupName)
        {
            var enrollments = database.Enrollments;
            var userToAdd = UserSystem.GetUserByName(username);
            if (userToAdd == null)
            {
                return new CommandResult("Error: User id not present in id cache, please try again in a few minutes.");
            }

            var group = database.AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }

            var groupEnrollments = enrollments.Read(x => x.GroupId.Equals(group.Id));
            if (groupEnrollments.Any(x => userToAdd.TwitchId.Equals(x.UserId)))
            {
                return new CommandResult($"Error: User \"{username}\" is already a member of \"{groupName}\".");
            }
            enrollments.Create(new Enrollment(group, userToAdd.TwitchId));
            enrollments.Commit();

            return new CommandResult($"User \"{username}\" was added to group \"{group.Name}\" successfully!");
        }

        private CommandResult RemoveUserFromGroup(IDatabase database, string username, string groupName)
        {
            var enrollments = database.Enrollments;
            var group = database.AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }

            var userToRemove = UserSystem.GetUserByName(username);
            if (userToRemove == null)
            {
                return new CommandResult($"Error: User \"{username}\" not found in user database. Please ensure the name is correct and the user has been in chat before.");
            }

            var enrollment = enrollments.Read(x => x.GroupId.Equals(group.Id) && x.UserId.Equals(userToRemove.TwitchId)).FirstOrDefault();
            if (enrollment == null)
            {
                return new CommandResult($"Error: User \"{username}\" is not a member of \"{groupName}\".");
            }
            enrollments.Delete(enrollment);
            enrollments.Commit();

            return new CommandResult($"User \"{username}\" was removed from group \"{group.Name}\" successfully!");
        }

        private CommandResult AddCommandToGroup(IDatabase database, string commandName, string groupName)
        {
            var restrictions = database.Restrictions;
            if (!CommandManager.IsValidCommand(commandName))
            {
                return new CommandResult($"Error: Command {commandName} does not match any commands.");
            }

            var group = database.AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: Group \"{groupName}\" does not exist.");
            }

            var groupRestrictions = restrictions.Read(x => x.GroupId == group.Id && x.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (groupRestrictions.Any())
            {
                return new CommandResult($"Error: \"{groupName}\" already has access to \"{commandName}\".");
            }

            restrictions.Create(new Restriction() { GroupId = group.Id, Command = commandName });
            restrictions.Commit();
            return new CommandResult($"Command \"{commandName}\" was added to the group \"{group.Name}\" successfully!");
        }

        private CommandResult ListCommands(IDatabase database)
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

        private CommandResult RemoveCommandFromGroup(IDatabase database, string commandName, string groupName)
        {
            var restrictions = database.Restrictions;
            if (!CommandManager.IsValidCommand(commandName))
            {
                return new CommandResult($"Error: Command {commandName} does not match any commands.");
            }

            var group = database.AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult($"Error: Group \"{groupName}\" does not exist.");
            }

            var groupRestrictions = restrictions.Read(x => x.GroupId == group.Id && x.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (!groupRestrictions.Any())
            {
                return new CommandResult($"Error: \"{groupName}\" doesn't have access to \"{commandName}\".");
            }

            var toRemove = groupRestrictions.First();
            restrictions.Delete(toRemove);
            restrictions.Commit();

            return new CommandResult($"Command \"{commandName}\" was removed from group \"{group.Name}\" successfully!");
        }
    }
}
