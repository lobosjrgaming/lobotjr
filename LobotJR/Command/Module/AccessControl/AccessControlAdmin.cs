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
        private readonly IRepository<AccessGroup> UserRoles;
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
            UserRoles = repositoryManager.UserRoles;
            UserSystem = userSystem;
            Commands = new CommandHandler[]
            {
                new CommandHandler("ListRoles", ListRoles, "ListRoles", "list-roles"),
                new CommandHandler("CreateRole", CreateRole, "CreateRole", "create-role"),
                new CommandHandler("DescribeRole", DescribeRole, "DescribeRole", "describe-role"),
                new CommandHandler("DeleteRole", DeleteRole, "DeleteRole", "delete-role"),

                new CommandHandler("EnrollUser", AddUserToRole, "EnrollUser", "enroll-user"),
                new CommandHandler("UnenrollUser", RemoveUserFromRole, "UnenrollUser", "unenroll-user"),

                new CommandHandler("RestrictCommand", AddCommandToRole, "RestrictCommand", "restrict-command"),
                new CommandHandler("ListCommands", ListCommands, "ListCommands", "list-commands"),
                new CommandHandler("UnrestrictCommand", RemoveCommandFromRole, "UnrestrictCommand", "unrestrict-command")
            };
        }

        private CommandResult ListRoles(string data, User user)
        {
            return new CommandResult(user, $"There are {UserRoles.Read().Count()} roles: {string.Join(", ", UserRoles.Read().Select(x => x.Name))}");
        }

        private CommandResult CreateRole(string data, User user)
        {
            var existingRole = UserRoles.Read(x => x.Name.Equals(data)).FirstOrDefault();
            if (existingRole != null)
            {
                return new CommandResult(user, $"Error: Unable to create role, \"{data}\" already exists.");
            }

            UserRoles.Create(new AccessGroup(data));
            UserRoles.Commit();
            return new CommandResult(user, $"Role \"{data}\" created successfully!");
        }

        private CommandResult DescribeRole(string data, User user)
        {
            var existingRole = UserRoles.Read(x => x.Name.Equals(data)).FirstOrDefault();
            if (existingRole == null)
            {
                return new CommandResult(user, $"Error: Role \"{data}\" not found.");
            }

            return new CommandResult(user,
                $"Role \"{data}\" contains the following commands: {string.Join(", ", existingRole.Commands)}.",
                $"Role \"{data}\" contains the following users: {string.Join(", ", existingRole.UserIds)}."
            );
        }

        private CommandResult DeleteRole(string data, User user)
        {
            var existingRole = UserRoles.Read(x => x.Name.Equals(data)).FirstOrDefault();
            if (existingRole == null)
            {
                return new CommandResult(user, $"Error: Unable to delete role, \"{data}\" does not exist.");
            }

            if (existingRole.Commands.Count > 0)
            {
                return new CommandResult(user, $"Error: Unable to delete role, please remove all commands first.");
            }

            UserRoles.Delete(existingRole);
            UserRoles.Commit();
            return new CommandResult(user, $"Role \"{data}\" deleted successfully!");
        }

        private CommandResult AddUserToRole(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected parameters: {username} {role name}.");
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
            var roleName = data.Substring(space + 1);
            if (roleName.Length == 0)
            {
                return new CommandResult(user, "Error: Role name cannot be empty.");
            }

            var role = UserRoles.Read(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (role == null)
            {
                return new CommandResult(user, $"Error: No role with name \"{roleName}\" was found.");
            }
            if (role.UserIds.Contains(userToAdd.TwitchId))
            {
                return new CommandResult(user, $"Error: User \"{userNameToAdd}\" is already a member of \"{roleName}\".");
            }
            role.AddUser(userToAdd.TwitchId);
            UserRoles.Update(role);
            UserRoles.Commit();

            return new CommandResult(user, $"User \"{userNameToAdd}\" was added to role \"{role.Name}\" successfully!");
        }

        private CommandResult RemoveUserFromRole(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected parameters: {username} {role name}.");
            }

            var userNameToRemove = data.Substring(0, space);
            if (userNameToRemove.Length == 0)
            {
                return new CommandResult(user, "Error: Username cannot be empty.");
            }
            var roleName = data.Substring(space + 1);
            if (roleName.Length == 0)
            {
                return new CommandResult(user, "Error: Role name cannot be empty.");
            }

            var userToRemove = UserSystem.GetUserByName(userNameToRemove);
            var role = UserRoles.Read(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (role == null)
            {
                return new CommandResult(user, $"Error: No role with name \"{roleName}\" was found.");
            }

            if (!role.UserIds.Contains(userToRemove.TwitchId))
            {
                return new CommandResult(user, $"Error: User \"{userNameToRemove}\" is not a member of \"{roleName}\".");
            }
            role.RemoveUser(userToRemove.TwitchId);
            UserRoles.Update(role);
            UserRoles.Commit();

            return new CommandResult(user, $"User \"{userNameToRemove}\" was removed from role \"{role.Name}\" successfully!");
        }

        private CommandResult AddCommandToRole(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected parameters: {command name} {role name}.");
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

            var roleName = data.Substring(space + 1);
            if (roleName.Length == 0)
            {
                return new CommandResult(user, "Error: Role name cannot be empty.");
            }
            var role = UserRoles.Read(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (role == null)
            {
                return new CommandResult(user, $"Error: Role \"{roleName}\" does not exist.");
            }

            if (role.Commands.Contains(commandName))
            {
                return new CommandResult(user, $"Error: \"{roleName}\" already has access to \"{commandName}\".");
            }

            role.AddCommand(commandName);
            UserRoles.Update(role);
            UserRoles.Commit();

            return new CommandResult(user, $"Command \"{commandName}\" was added to the role \"{role.Name}\" successfully!");
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

        private CommandResult RemoveCommandFromRole(string data, User user)
        {
            var space = data.IndexOf(' ');
            if (space == -1)
            {
                return new CommandResult(user, "Error: Invalid number of parameters. Expected paremeters: {command name} {role name}.");
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

            var roleName = data.Substring(space + 1);
            if (roleName.Length == 0)
            {
                return new CommandResult(user, "Error: Role name cannot be empty.");
            }
            var role = UserRoles.Read(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (role == null)
            {
                return new CommandResult(user, $"Error: Role \"{roleName}\" does not exist.");
            }

            if (!role.Commands.Contains(commandName))
            {
                return new CommandResult(user, $"Error: \"{roleName}\" doesn't have access to \"{commandName}\".");
            }

            role.RemoveCommand(commandName);
            UserRoles.Update(role);
            UserRoles.Commit();

            return new CommandResult(user, $"Command \"{commandName}\" was removed from role \"{role.Name}\" successfully!");
        }
    }
}
