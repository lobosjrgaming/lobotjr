﻿using LobotJR.Data;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.AccessControl
{
    /// <summary>
    /// Module of access control commands.
    /// </summary>
    public class AccessControlModule : ICommandModule
    {
        private readonly IRepository<AccessGroup> repository;
        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "AccessControl";

        /// <summary>
        /// This module does not issue any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;

        /// <summary>
        /// A collection of commands for managing access to commands.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public AccessControlModule(IRepositoryManager repositoryManager)
        {
            repository = repositoryManager.UserRoles;
            Commands = new CommandHandler[]
            {
                new CommandHandler("CheckAccess", CheckAccess, "CheckAccess", "check-access"),
            };
        }

        private CommandResult CheckAccess(string data, User user)
        {
            var roleName = data;
            if (roleName == null || roleName.Length == 0)
            {
                var roles = repository.Read(x => x.UserIds.Any(y => y.Equals(user.TwitchId, StringComparison.OrdinalIgnoreCase)));
                if (roles.Any())
                {
                    var count = roles.Count();
                    return new CommandResult(user, $"You are a member of the following role{(count == 1 ? "" : "s")}: {string.Join(", ", roles.Select(x => x.Name))}.");
                }
                else
                {
                    return new CommandResult(user, "You are not a member of any roles.");
                }
            }

            var role = repository.Read(x => x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (role == null)
            {
                return new CommandResult(user, $"Error: No role with name \"{roleName}\" was found.");
            }

            var access = role.UserIds.Contains(user.TwitchId) ? "are" : "are not";
            return new CommandResult(user, $"You {access} a member of \"{role.Name}\"!");
        }
    }
}
