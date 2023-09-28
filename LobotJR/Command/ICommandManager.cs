﻿using LobotJR.Command.Module;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System.Collections.Generic;

namespace LobotJR.Command
{
    /// <summary>
    /// Manages the commands the bot can execute.
    /// </summary>
    public interface ICommandManager
    {
        /// <summary>
        /// Event raised when a module sends a push notification.
        /// </summary>
        event PushNotificationHandler PushNotifications;

        /// <summary>
        /// Repository manager for access to data.
        /// </summary>
        IRepositoryManager RepositoryManager { get; }
        /// <summary>
        /// User lookup service used to translate between usernames and user ids.
        /// </summary>
        UserSystem UserSystem { get; }
        /// <summary>
        /// List of ids for registered commands.
        /// </summary>
        IEnumerable<string> Commands { get; }

        /// <summary>
        /// Initializes all registered command modules.
        /// </summary>
        void InitializeModules();
        /// <summary>
        /// Checks if a command id exists or is a valid wildcard pattern.
        /// </summary>
        /// <param name="commandId">The command id to validate.</param>
        /// <returns>Whether or not the command id is valid.</returns>
        bool IsValidCommand(string commandId);
        /// <summary>
        /// Processes a message from a user to check for and execute a command.
        /// </summary>
        /// <param name="message">The message the user sent.</param>
        /// <param name="user">The Twitch user object.</param>
        /// <param name="isWhisper">Whether or not the message was sent as a whisper.</param>
        /// <returns>An object containing the results of the attempt to process the message.</returns>
        CommandResult ProcessMessage(string message, User user, bool isWhisper);
    }
}
