using LobotJR.Command.Module;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// <summary>
        /// Processes a command result object, adding all output to the logs
        /// and sending any whispers or chat messages triggered by the command.
        /// </summary>
        /// <param name="whisperMessage">The initial message that triggered the commmand.</param>
        /// <param name="result">The command result object.</param>
        /// <param name="irc">The twitch irc client to send messages through.</param>
        /// <param name="twitchClient">The twitch API client to send whispers through.</param>
        Task HandleResult(string whisperMessage, CommandResult result, ITwitchIrcClient irc, ITwitchClient twitchClient);
    }
}
