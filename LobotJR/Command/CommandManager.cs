﻿using Autofac;
using LobotJR.Command.View;
using LobotJR.Data;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LobotJR.Command
{
    /// <summary>
    /// Manages the commands the bot can execute.
    /// </summary>
    public class CommandManager : ICommandManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly char Prefix = '!';

        private const int MessageLimit = 450;

        private readonly Dictionary<string, string> commandStringToIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CommandExecutor> commandIdToExecutorMap = new Dictionary<string, CommandExecutor>();
        private readonly Dictionary<string, CompactExecutor> compactIdToExecutorMap = new Dictionary<string, CompactExecutor>();
        private readonly Dictionary<string, IEnumerable<string>> commandIdAliases = new Dictionary<string, IEnumerable<string>>();
        private readonly List<string> whisperOnlyCommands = new List<string>();
        private readonly List<string> timeoutCommands = new List<string>();
        private readonly Dictionary<string, Regex> commandStringRegexMap = new Dictionary<string, Regex>();
        private readonly IConnectionManager ConnectionManager;

        /// <summary>
        /// Event raised when a view sends a push notification.
        /// </summary>
        public event PushNotificationHandler PushNotifications;

        /// <summary>
        /// Command views to be loaded.
        /// </summary>
        public IEnumerable<ICommandView> CommandViews { get; private set; }
        /// <summary>
        /// List of ids for registered commands.
        /// </summary>
        public IEnumerable<string> Commands
        {
            get
            {
                return commandIdToExecutorMap.Keys
                    .Union(compactIdToExecutorMap.Keys)
                    .ToArray();
            }
        }

        /// <summary>
        /// List of command strings and aliases for registered commands.
        /// </summary>
        public IEnumerable<string> CommandStrings
        {
            get
            {
                return commandStringToIdMap.Keys.ToList();
            }
        }

        public CommandManager(IEnumerable<ICommandView> views, IEnumerable<IMetaController> metaControllers, IConnectionManager connectionManager)
        {
            CommandViews = views;
            ConnectionManager = connectionManager;
            foreach (var meta in metaControllers)
            {
                meta.CommandManager = this;
            }
        }

        private void AddCommand(CommandHandler command, string prefix)
        {
            var commandId = $"{prefix}.{command.Name}";
            if (command.Executor != null)
            {
                commandIdToExecutorMap.Add(commandId, command.Executor);
            }
            if (command.CompactExecutor != null)
            {
                compactIdToExecutorMap.Add(commandId, command.CompactExecutor);
            }
            if (command.WhisperOnly)
            {
                whisperOnlyCommands.Add(commandId);
                if (command.TimeoutInChat)
                {
                    timeoutCommands.Add(commandId);
                }
            }
            commandIdAliases.Add(commandId, command.CommandStrings);
            var exceptions = new List<Exception>();
            foreach (var commandString in command.CommandStrings)
            {
                if (commandStringToIdMap.ContainsKey(commandString))
                {
                    exceptions.Add(new Exception($"{commandId}: The command string \"{commandString}\" has already been registered by {commandStringToIdMap[commandString]}."));
                }
                else
                {
                    commandStringToIdMap.Add(commandString, commandId);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        private void AddView(ICommandView view)
        {
            if (view is IPushNotifier)
            {
                (view as IPushNotifier).PushNotification += View_PushNotification;
            }

            var exceptions = new List<Exception>();
            foreach (var command in view.Commands)
            {
                try
                {
                    AddCommand(command, view.Name);
                }
                catch (AggregateException e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"Failed to load view {view.Name}", exceptions);
            }
        }

        private async Task View_PushNotification(User user, CommandResult commandResult)
        {
            await PushNotifications?.Invoke(user, commandResult);
        }

        private bool CanUserExecute(string commandId, User user)
        {
            var restrictions = ConnectionManager.CurrentConnection.Restrictions.Read().Where(x => Restriction.CoversCommand(x.Command, commandId));
            if (restrictions.Any())
            {
                var groupIds = restrictions.Select(x => x.GroupId).ToList();
                var groups = ConnectionManager.CurrentConnection.AccessGroups.Read().Where(x => groupIds.Contains(x.Id));

                if ((user.IsMod && groups.Any(x => x.IncludeMods))
                    || (user.IsVip && groups.Any(x => x.IncludeVips))
                    || (user.IsSub && groups.Any(x => x.IncludeSubs))
                    || (user.IsAdmin && groups.Any(x => x.IncludeAdmins)))
                {
                    return true;
                }

                var enrollments = ConnectionManager.CurrentConnection.Enrollments.Read().Where(x => x.UserId.Equals(user.TwitchId, StringComparison.OrdinalIgnoreCase));
                return enrollments.Any(x => groupIds.Contains(x.GroupId));
            }
            return true;
        }

        private bool CanExecuteInChat(string commandId)
        {
            return !whisperOnlyCommands.Contains(commandId);
        }

        private bool TriggersTimeout(string commandId)
        {
            return timeoutCommands.Contains(commandId);
        }

        /// <summary>
        /// Initializes all registered command views.
        /// </summary>
        public void InitializeViews()
        {
            var exceptions = new List<Exception>();
            foreach (var view in CommandViews)
            {
                try
                {
                    AddView(view);
                }
                catch (AggregateException e)
                {
                    exceptions.Add(e);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }


        /// <summary>
        /// Checks if a command id exists or is a valid wildcard pattern.
        /// </summary>
        /// <param name="commandId">The command id to validate.</param>
        /// <returns>Whether or not the command id is valid.</returns>
        public bool IsValidCommand(string commandId)
        {
            var index = commandId.IndexOf('*');
            if (index >= 0)
            {
                if (!commandStringRegexMap.ContainsKey(commandId))
                {
                    var commandString = commandId.Replace(".", "\\.").Replace("*", ".*");
                    commandStringRegexMap.Add(commandId, new Regex($"^{commandString}$"));
                }
                var commandRegex = commandStringRegexMap[commandId];
                return Commands.Any(x => commandRegex.IsMatch(x));
            }
            return Commands.Any(x => x.Equals(commandId));
        }

        /// <summary>
        /// Finds all commands with ids that match the given pattern.
        /// </summary>
        /// <param name="pattern">A command pattern containing one or more
        /// wildcard (*) characters.</param>
        /// <returns>A collection of matching command ids.</returns>
        public IEnumerable<string> GetMatchingCommands(string pattern)
        {
            pattern = pattern.Replace(".", "\\.").Replace("*", ".*");
            var regex = new Regex($"^{pattern}$");
            return Commands.Where(x => regex.IsMatch(x));
        }

        /// <summary>
        /// Describes the parameters for a command.
        /// </summary>
        /// <param name="commandName">The name of the command to check.</param>
        /// <returns>A string containing the parameter names and types for the
        /// command.</returns>
        public string DescribeCommand(string commandName)
        {
            if (commandStringToIdMap.TryGetValue(commandName, out var id))
            {
                if (commandIdToExecutorMap.TryGetValue(id, out var executor))
                {
                    return executor.DescribeParameters();
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets all aliases for a given command id.
        /// </summary>
        /// <param name="commandId">The id of a command.</param>
        /// <returns>A list of aliases that can be used to execute the command.</returns>
        public IEnumerable<string> GetAliases(string commandId)
        {
            if (commandIdAliases.TryGetValue(commandId, out var aliases))
            {
                return aliases;
            }
            return Enumerable.Empty<string>();
        }

        private CommandResult PrepareCompactResponse(CommandRequest request, User user, ICompactResponse response)
        {
            var entries = response.ToCompact();
            var prefix = $"{request.CommandString}: ";
            var toSend = prefix;
            var responses = new List<string>();
            foreach (var entry in entries)
            {
                if (toSend.Length + entry.Length > MessageLimit)
                {
                    responses.Add(toSend);
                    toSend = prefix;
                }
                toSend += entry;
            }
            responses.Add(toSend);
            return new CommandResult(user, responses.ToArray());
        }

        private CommandResult TryExecuteCommand(CommandRequest request, User user)
        {
            try
            {
                if (request.IsCompact)
                {
                    if (compactIdToExecutorMap.TryGetValue(request.CommandId, out var compactExecutor))
                    {
                        var compactResponse = compactExecutor.Execute(user, request.Data);
                        if (compactResponse != null)
                        {
                            return PrepareCompactResponse(request, user, compactResponse);
                        }
                        return new CommandResult($"Command requested produced no results.");
                    }
                    else
                    {
                        return new CommandResult($"Command {request.CommandId} does not support compact mode.");
                    }
                }
                else
                {
                    if (commandIdToExecutorMap.TryGetValue(request.CommandId, out var executor))
                    {
                        CommandResult response;
                        try
                        {
                            response = executor.Execute(user, request.Data);
                        }
                        catch (ArgumentException e)
                        {
                            response = new CommandResult($"Error: {e.Message}");
                        }
                        catch (InvalidCastException e)
                        {
                            response = new CommandResult($"Error {e.Message}");
                        }
                        if (response != null)
                        {
                            response.Sender = user;
                            return response;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return new CommandResult(new Exception[] { e });
            }
            return new CommandResult();
        }

        /// <summary>
        /// Processes a message from a user to check for and execute a command.
        /// </summary>
        /// <param name="message">The message the user sent.</param>
        /// <param name="user">The Twitch user object.</param>
        /// <param name="isWhisper">Whether or not the message was sent as a whisper.</param>
        /// <returns>Whether a command was found and executed.</returns>
        public CommandResult ProcessMessage(string message, User user, bool isWhisper)
        {
            Logger.Debug("Attempting to process {user}'s command: {message}", user.Username, message);
            var request = CommandRequest.Parse(message);
            if (commandStringToIdMap.TryGetValue(request.CommandString, out var commandId))
            {
                if (!isWhisper && !CanExecuteInChat(commandId))
                {
                    if (TriggersTimeout(commandId))
                    {
                        return new CommandResult()
                        {
                            Processed = true,
                            Sender = user,
                            TimeoutSender = true,
                            Responses = new string[]
                            {
                                "You just tried to use a command in chat that is only available by whispering me. Reply in this window on twitch or type '/w lobotjr' in chat to use that command.",
                                "Sorry for purging you. Just trying to do my job to keep chat clear! <3"
                            }
                        };
                    }
                    return new CommandResult(true);
                }
                if (user.BanTime != null)
                {
                    Logger.Debug("Ignoring banned user's command.");
                    return new CommandResult()
                    {
                        Processed = true,
                        Sender = user,
                    };
                }
                request.CommandId = commandId;
                request.User = user;

                if (CanUserExecute(request.CommandId, request.User))
                {
                    return TryExecuteCommand(request, user);
                }
                return new CommandResult(new Exception[] { new UnauthorizedAccessException($"User \"{user.Username}\" attempted to execute unauthorized command \"{message}\"") });
            }
            return new CommandResult();
        }

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
        public async Task HandleResult(string whisperMessage, CommandResult result, ITwitchIrcClient irc, ITwitchClient twitchClient, bool isInternal = false)
        {
            if (result.TimeoutSender)
            {
                if (isInternal)
                {
                    Logger.Warn("This command would have attempted to time you out if sent through Twitch.");
                }
                else
                {
                    await twitchClient.TimeoutAsync(result.Sender, 1, result.TimeoutMessage);
                }
            }
            if (result.Responses?.Count > 0)
            {
                if (isInternal)
                {
                    foreach (var response in result.Responses)
                    {
                        Logger.Info(response);
                    }
                }
                else
                {
                    foreach (var response in result.Responses)
                    {
                        twitchClient.QueueWhisper(result.Sender, response);
                    }
                }
            }
            if (result.Messages?.Count > 0)
            {
                foreach (var broadcastMessage in result.Messages)
                {
                    irc.QueueMessage(broadcastMessage);
                }
                if (isInternal)
                {
                    foreach (var message in result.Messages)
                    {
                        Logger.Info(message);
                    }
                }
            }
            if (result.Errors?.Count > 0)
            {
                Logger.Error("Errors encountered while processing command {message} from user {user}", whisperMessage, result.Sender);
                foreach (var error in result.Errors)
                {
                    Logger.Error(error);
                }
            }
            if (result.Debug?.Count > 0)
            {
                Logger.Debug("Debug output generated by command {message} from user {user}", whisperMessage, result.Sender);
                foreach (var debug in result.Debug)
                {
                    Logger.Debug(debug);
                }
            }
        }
    }
}
