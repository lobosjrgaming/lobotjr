using LobotJR.Command.Controller.AccessControl;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.AccessControl
{
    /// <summary>
    /// View containing commands for managing access groups.
    /// </summary>
    public class AccessControlAdmin : ICommandView
    {
        private readonly AccessControlController AccessControlController;
        private readonly UserController UserController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "AccessControl.Admin";
        /// <summary>
        /// This view does not issue any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public AccessControlAdmin(AccessControlController accessControlController, UserController userController)
        {
            AccessControlController = accessControlController;
            UserController = userController;
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
            var groups = AccessControlController.GetAllGroups();
            return new CommandResult($"There are {groups.Count()} groups: {string.Join(", ", groups.Select(x => x.Name))}");
        }

        private CommandResult CreateGroup(string groupName)
        {
            if (!AccessControlController.DoesGroupExist(groupName))
            {
                AccessControlController.CreateGroup(groupName);
                return new CommandResult($"Access group \"{groupName}\" created successfully!");
            }
            return new CommandResult($"Error: Unable to create group, \"{groupName}\" already exists.");
        }

        private CommandResult DescribeGroup(string groupName)
        {
            var existingGroup = AccessControlController.GetGroupByName(groupName);
            if (existingGroup != null)
            {
                var enrollments = AccessControlController.GetGroupEnrollments(existingGroup);
                var restrictions = AccessControlController.GetGroupRestrictions(existingGroup);
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
                names.AddRange(enrollments.Select(x => UserController.GetUserById(x.UserId)?.Username).Where(x => x != null));
                return new CommandResult(
                    $"Access group \"{groupName}\" contains the following commands: {string.Join(", ", restrictions.Select(x => x.Command))}.",
                    $"Access group \"{groupName}\" contains the following users: {string.Join(", ", names)}."
                );
            }
            return new CommandResult($"Error: Group \"{groupName}\" not found.");
        }

        private CommandResult DeleteGroup(string groupName)
        {
            var existingGroup = AccessControlController.GetGroupByName(groupName);
            if (existingGroup != null)
            {
                if (!AccessControlController.GetGroupEnrollments(existingGroup).Any())
                {
                    if (!AccessControlController.GetGroupRestrictions(existingGroup).Any())
                    {
                        AccessControlController.DeleteGroup(existingGroup);
                        return new CommandResult($"Group \"{groupName}\" deleted successfully!");
                    }
                    return new CommandResult($"Error: Unable to delete group, please unrestrict all commands first.");
                }
                return new CommandResult($"Error: Unable to delete group, please unenroll all users first.");
            }
            return new CommandResult($"Error: Unable to delete group, \"{groupName}\" does not exist.");
        }

        private CommandResult SetGroupFlag(string groupName, string flag, bool value)
        {
            if (Enum.TryParse<GroupFlags>(flag, true, out var enumFlag))
            {
                var existingGroup = AccessControlController.GetGroupByName(groupName);
                if (existingGroup != null)
                {
                    switch (enumFlag)
                    {
                        case GroupFlags.Admin:
                            existingGroup.IncludeAdmins = value;
                            break;
                        case GroupFlags.Mod:
                            existingGroup.IncludeMods = value;
                            break;
                        case GroupFlags.Vip:
                            existingGroup.IncludeVips = value;
                            break;
                        case GroupFlags.Sub:
                            existingGroup.IncludeSubs = value;
                            break;
                    }
                    var includeClause = value ? "includes" : "does not include";
                    return new CommandResult($"Access group \"{existingGroup.Name}\" now {includeClause} {flag}s.");
                }
                return new CommandResult($"Error: Group \"{groupName}\" not found.");
            }
            var names = Enum.GetNames(typeof(GroupFlags));
            return new CommandResult($"Error: Invalid flag, must be one of {string.Join(", ", names.Select(x => $"\"{x}\""))}.");
        }

        private CommandResult AddUserToGroup(string username, string groupName)
        {
            var user = UserController.GetUserByName(username);
            if (user != null)
            {
                var group = AccessControlController.GetGroupByName(groupName);
                if (group != null)
                {
                    var success = AccessControlController.EnrollUserInGroup(group, user);
                    if (success)
                    {
                        return new CommandResult($"User \"{user.Username}\" was added to group \"{group.Name}\" successfully!");
                    }
                    return new CommandResult($"Error: User \"{user.Username}\" is already a member of \"{group.Name}\".");
                }
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }
            return new CommandResult($"Error: User {username} not present in database, please try again in a few minutes.");
        }

        private CommandResult RemoveUserFromGroup(string username, string groupName)
        {
            var user = UserController.GetUserByName(username);
            if (user != null)
            {
                var group = AccessControlController.GetGroupByName(groupName);
                if (group != null)
                {
                    var success = AccessControlController.UnenrollUserFromGroup(group, user);
                    if (success)
                    {
                        return new CommandResult($"User \"{user.Username}\" was removed from group \"{group.Name}\" successfully!");
                    }
                    return new CommandResult($"Error: User \"{user.Username}\" is not a member of \"{group.Name}\".");
                }
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }
            return new CommandResult($"Error: User {username} not present in database, please try again in a few minutes.");
        }

        private CommandResult AddCommandToGroup(string commandName, string groupName)
        {
            if (AccessControlController.IsValidCommand(commandName))
            {
                var group = AccessControlController.GetGroupByName(groupName);
                if (group != null)
                {
                    var success = AccessControlController.RestrictCommandToGroup(group, commandName);
                    if (success)
                    {
                        return new CommandResult($"Command \"{commandName}\" was added to group \"{group.Name}\" successfully!");
                    }
                    return new CommandResult($"Error: Command \"{commandName}\" is already restricted to \"{group.Name}\".");
                }
                return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
            }
            return new CommandResult($"Error: Command {commandName} does not match any commands.");
        }

        private CommandResult ListCommands()
        {
            var commands = AccessControlController.GetAllCommands();
            var views = commands.Where(x => x.LastIndexOf('.') != -1).Select(x => x.Substring(0, x.LastIndexOf('.'))).Distinct().ToList();
            var response = new string[views.Count + 1];
            response[0] = $"There are {commands.Count()} commands across {views.Count} views.";
            for (var i = 0; i < views.Count; i++)
            {
                response[i + 1] = $"{views[i]}: {string.Join(", ", commands.Where(x => x.StartsWith(views[i])))}";
            }
            return new CommandResult(response);
        }

        private CommandResult RemoveCommandFromGroup(string commandName, string groupName)
        {
            var group = AccessControlController.GetGroupByName(groupName);
            if (group != null)
            {
                var success = AccessControlController.UnrestrictCommandFromGroup(group, commandName);
                if (success)
                {
                    return new CommandResult($"Command \"{commandName}\" was removed from group \"{group.Name}\" successfully!");
                }
                return new CommandResult($"Error: Command \"{commandName}\" is not restricted to \"{group.Name}\".");
            }
            return new CommandResult($"Error: No group with name \"{groupName}\" was found.");
        }
    }
}
