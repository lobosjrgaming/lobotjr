using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
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
                new CommandHandler("ListGroups", ListGroups, "ListGroups", "list-groups", "ListRoles", "list-roles"),
                new CommandHandler("CreateGroup", CreateGroup, "CreateGroup", "create-group", "CreateRole", "create-role"),
                new CommandHandler("DescribeGroup", DescribeGroup, "DescribeGroup", "describe-group", "DescribeRole", "describe-role"),
                new CommandHandler("DeleteGroup", DeleteGroup, "DeleteGroup", "delete-group", "DeleteRole", "delete-role"),

                new CommandHandler("SetGroupFlag", SetGroupFlag, "SetGroupFlag", "set-group-flag"),

                new CommandHandler("EnrollUser", AddUserToGroup, "EnrollUser", "enroll-user"),
                new CommandHandler("UnenrollUser", RemoveUserFromGroup, "UnenrollUser", "unenroll-user"),

                new CommandHandler("RestrictCommand", AddCommandToGroup, "RestrictCommand", "restrict-command"),
                new CommandHandler("ListCommands", ListCommands, "ListCommands", "list-commands"),
                new CommandHandler("UnrestrictCommand", RemoveCommandFromGroup, "UnrestrictCommand", "unrestrict-command")
            };
        }

        private CommandResult ListGroups(string data, User user)
        {
            return new CommandResult(user, $"There are {AccessGroups.Read().Count()} groups: {string.Join(", ", AccessGroups.Read().Select(x => x.Name))}");
        }

        private CommandResult CreateGroup(string data, User user)
        {
            var existingGroup = AccessGroups.Read(x => x.Name.Equals(data)).FirstOrDefault();
            if (existingGroup != null)
            {
                return new CommandResult(user, $"Error: Unable to create group, \"{data}\" already exists.");
            }

            AccessGroups.Create(new AccessGroup() { Name = data });
            AccessGroups.Commit();
            return new CommandResult(user, $"Access group \"{data}\" created successfully!");
        }

        private CommandResult DescribeGroup(string data, User user)
        {
            var existingGroup = AccessGroups.Read(x => x.Name.Equals(data)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult(user, $"Error: Group \"{data}\" not found.");
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
            return new CommandResult(user,
                $"Access group \"{data}\" contains the following commands: {string.Join(", ", restrictions.Select(x => x.Command))}.",
                $"Access group \"{data}\" contains the following users: {string.Join(", ", names)}."
            );
        }

        private CommandResult DeleteGroup(string data, User user)
        {
            var existingGroup = AccessGroups.Read(x => x.Name.Equals(data)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult(user, $"Error: Unable to delete group, \"{data}\" does not exist.");
            }

            var enrollments = Enrollments.Read(x => x.GroupId.Equals(existingGroup.Id));
            if (enrollments.Any())
            {
                return new CommandResult(user, $"Error: Unable to delete group, please unenroll all users first.");
            }

            var restrictions = Restrictions.Read(x => x.GroupId.Equals(existingGroup.Id));
            if (restrictions.Any())
            {
                return new CommandResult(user, $"Error: Unable to delete group, please unrestrict all commands first.");
            }

            AccessGroups.Delete(existingGroup);
            AccessGroups.Commit();
            return new CommandResult(user, $"Group \"{data}\" deleted successfully!");
        }

        private CommandResult SetGroupFlag(string data, User user)
        {
            List<int> spaces = new List<int>();
            for (var i = data.IndexOf(' '); i != -1; i = data.IndexOf(' ', i + 1))
            {
                spaces.Add(i);
            }
            if (spaces.Count < 2)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected parameters: {flag name} {value} {group name}.");
            }
            var flag = data.Substring(0, spaces[0]);
            var valueString = data.Substring(spaces[0] + 1, spaces[1] - spaces[0]);
            var groupName = data.Substring(spaces[1] + 1);

            var existingGroup = AccessGroups.Read(x => x.Name.Equals(groupName)).FirstOrDefault();
            if (existingGroup == null)
            {
                return new CommandResult(user, $"Error: Group \"{data}\" not found.");
            }

            bool value = false;
            var parseResult = bool.TryParse(valueString, out value);
            if (!parseResult)
            {
                return new CommandResult(user, $"Error: Invalid value, must be \"true\" or \"false\".");
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
                return new CommandResult(user, $"Error: Invalid flag, must be one of \"mod\", \"vip\", \"sub\", or \"admin\".");
            }
            AccessGroups.Update(existingGroup);
            AccessGroups.Commit();
            var includeClause = value ? "includes" : "does not include";
            return new CommandResult(user, $"Access group \"{existingGroup.Name}\" now {includeClause} {flag}s.");
        }

        private CommandResult AddUserToGroup(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected parameters: {username} {group name}.");
            }

            var userNameToAdd = data.Substring(0, space);
            if (userNameToAdd.Length == 0)
            {
                return new CommandResult(user, "Error: Username cannot be empty.");
            }
            var userToAdd = UserSystem.GetUserByName(userNameToAdd);
            if (userToAdd == null)
            {
                return new CommandResult(user, "Error: User id not present in id cache, please try again in a few minutes.");
            }
            var groupName = data.Substring(space + 1);
            if (groupName.Length == 0)
            {
                return new CommandResult(user, "Error: Group name cannot be empty.");
            }

            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult(user, $"Error: No group with name \"{groupName}\" was found.");
            }

            var enrollments = Enrollments.Read(x => x.GroupId.Equals(group.Id));
            if (enrollments.Any(x => userToAdd.TwitchId.Equals(x.UserId)))
            {
                return new CommandResult(user, $"Error: User \"{userNameToAdd}\" is already a member of \"{groupName}\".");
            }
            Enrollments.Create(new Enrollment(group.Id, userToAdd.TwitchId));
            Enrollments.Commit();

            return new CommandResult(user, $"User \"{userNameToAdd}\" was added to group \"{group.Name}\" successfully!");
        }

        private CommandResult RemoveUserFromGroup(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected parameters: {username} {group name}.");
            }

            var userNameToRemove = data.Substring(0, space);
            if (userNameToRemove.Length == 0)
            {
                return new CommandResult(user, "Error: Username cannot be empty.");
            }
            var groupName = data.Substring(space + 1);
            if (groupName.Length == 0)
            {
                return new CommandResult(user, "Error: Group name cannot be empty.");
            }

            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult(user, $"Error: No group with name \"{groupName}\" was found.");
            }

            var userToRemove = UserSystem.GetUserByName(userNameToRemove);
            if (userToRemove == null)
            {
                return new CommandResult(user, $"Error: User \"{userNameToRemove}\" not found in user database. Please ensure the name is correct and the user has been in chat before.");
            }

            var enrollment = Enrollments.Read(x => x.GroupId.Equals(group.Id) && x.UserId.Equals(userToRemove.TwitchId)).FirstOrDefault();
            if (enrollment == null)
            {
                return new CommandResult(user, $"Error: User \"{userNameToRemove}\" is not a member of \"{groupName}\".");
            }
            Enrollments.Delete(enrollment);
            Enrollments.Commit();

            return new CommandResult(user, $"User \"{userNameToRemove}\" was removed from group \"{group.Name}\" successfully!");
        }

        private CommandResult AddCommandToGroup(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected parameters: {command name} {group name}.");
            }

            var commandName = data.Substring(0, space);
            if (commandName.Length == 0)
            {
                return new CommandResult(user, "Error: Command name cannot be empty.");
            }
            if (!CommandManager.IsValidCommand(commandName))
            {
                return new CommandResult(user, $"Error: Command {commandName} does not match any commands.");
            }

            var groupName = data.Substring(space + 1);
            if (groupName.Length == 0)
            {
                return new CommandResult(user, "Error: Group name cannot be empty.");
            }
            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult(user, $"Error: Group \"{groupName}\" does not exist.");
            }

            var restrictions = Restrictions.Read(x => x.GroupId == group.Id && x.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (restrictions.Any())
            {
                return new CommandResult(user, $"Error: \"{groupName}\" already has access to \"{commandName}\".");
            }

            Restrictions.Create(new Restriction() { GroupId = group.Id, Command = commandName });
            Restrictions.Commit();
            return new CommandResult(user, $"Command \"{commandName}\" was added to the group \"{group.Name}\" successfully!");
        }

        private CommandResult ListCommands(string data, User user)
        {
            var commands = CommandManager.Commands;
            var modules = commands.Where(x => x.LastIndexOf('.') != -1).Select(x => x.Substring(0, x.LastIndexOf('.'))).Distinct().ToList();
            var response = new string[modules.Count + 1];
            response[0] = $"There are {commands.Count()} commands across {modules.Count} modules.";
            for (var i = 0; i < modules.Count; i++)
            {
                response[i + 1] = $"{modules[i]}: {string.Join(", ", commands.Where(x => x.StartsWith(modules[i])))}";
            }
            return new CommandResult(user, response);
        }

        private CommandResult RemoveCommandFromGroup(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected paremeters: {command name} {group name}.");
            }

            var commandName = data.Substring(0, space);
            if (commandName.Length == 0)
            {
                return new CommandResult(user, "Error: Command name cannot be empty.");
            }
            if (!CommandManager.IsValidCommand(commandName))
            {
                return new CommandResult(user, $"Error: Command {commandName} does not match any commands.");
            }

            var groupName = data.Substring(space + 1);
            if (groupName.Length == 0)
            {
                return new CommandResult(user, "Error: Group name cannot be empty.");
            }
            var group = AccessGroups.Read(x => x.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (group == null)
            {
                return new CommandResult(user, $"Error: Group \"{groupName}\" does not exist.");
            }

            var restrictions = Restrictions.Read(x => x.GroupId == group.Id && x.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (!restrictions.Any())
            {
                return new CommandResult(user, $"Error: \"{groupName}\" doesn't have access to \"{commandName}\".");
            }

            var toRemove = restrictions.First();
            Restrictions.Delete(toRemove);
            Restrictions.Commit();

            return new CommandResult(user, $"Command \"{commandName}\" was removed from group \"{group.Name}\" successfully!");
        }
    }
}
