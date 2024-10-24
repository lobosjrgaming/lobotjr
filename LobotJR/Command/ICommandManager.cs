using LobotJR.Command.View;
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
        /// Event raised when a view sends a push notification.
        /// </summary>
        event PushNotificationHandler PushNotifications;
        /// <summary>
        /// List of ids for registered commands.
        /// </summary>
        IEnumerable<string> Commands { get; }
        /// <summary>
        /// List of command strings and aliases for registered commands.
        /// </summary>
        IEnumerable<string> CommandStrings { get; }

        /// <summary>
        /// Initializes all registered command views.
        /// </summary>
        void InitializeViews();
        /// <summary>
        /// Checks if a command id exists or is a valid wildcard pattern.
        /// </summary>
        /// <param name="commandId">The command id to validate.</param>
        /// <returns>Whether or not the command id is valid.</returns>
        bool IsValidCommand(string commandId);
        /// <summary>
        /// Describes the parameters for a command.
        /// </summary>
        /// <param name="commandName">The name of the command to check.</param>
        /// <returns>A string containing the parameter names and types for the
        /// command.</returns>
        string DescribeCommand(string commandName);
        /// <summary>
        /// Gets all aliases for a given command id.
        /// </summary>
        /// <param name="commandId">The id of a command.</param>
        /// <returns>A list of aliases that can be used to execute the command.</returns>
        IEnumerable<string> GetAliases(string commandId);
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
        /// <param name="isInternal">Whether this was an internal command
        /// invoked through the UI, or a normal message sent through Twitch.</param>
        Task HandleResult(string whisperMessage, CommandResult result, ITwitchIrcClient irc, ITwitchClient twitchClient, bool isInternal = false);
    }
}
