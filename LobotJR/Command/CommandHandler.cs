using LobotJR.Command.View;
using System.Collections.Generic;
using System.Reflection;

namespace LobotJR.Command
{
    /// <summary>
    /// Represents a command the bot can execute in response to a message from
    /// a user.
    /// </summary>
    public class CommandHandler
    {
        /// <summary>
        /// The name of the command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Determines whether or not the command can be sent through public
        /// chat, or only via whispers directly to the bot.
        /// </summary>
        public bool WhisperOnly { get; set; } = true;

        /// <summary>
        /// Determines whether users executing this command in chat will be
        /// timed out if they try to execute this command in chat instead of a
        /// whisper. This will only trigger if WhisperOnly is set to true.
        /// </summary>
        public bool TimeoutInChat { get; set; } = true;

        /// <summary>
        /// The strings that can be used to issue the command.
        /// </summary>
        public IEnumerable<string> CommandStrings { get; }

        /// <summary>
        /// Delegate that executes the command, and provides the strings to
        /// return to the executing user.
        /// </summary>
        public CommandExecutor Executor { get; set; }

        /// <summary>
        /// Delegate that execute the command in compact mode, which provides a
        /// response in the form of a collection of key/value pairs.
        /// </summary>
        public CompactExecutor CompactExecutor { get; set; }

        /// <summary>
        /// Creates a new command handler.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="methodInfo">Reflection data for the method executed by the command.</param>
        /// <param name="commandStrings">The strings that be used to trigger the command.</param>
        public CommandHandler(string name, ICommandView target, MethodInfo methodInfo, params string[] commandStrings)
        {
            Name = name;
            Executor = new CommandExecutor(target, methodInfo);
            CommandStrings = commandStrings;
        }

        /// <summary>
        /// Creates a new command handler with a specified executor. Mainly used for testing.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="executor">The command executor object used to execute the command.</param>
        /// <param name="commandStrings">The strings that be used to trigger the command.</param>
        public CommandHandler(string name, CommandExecutor executor, params string[] commandStrings)
        {
            Name = name;
            Executor = executor;
            CommandStrings = commandStrings;
        }

        /// <summary>
        /// Creates a new command handler.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="methodInfo">Reflection data for the method executed by the command.</param>
        /// <param name="compactMethodInfo">Reflection data for the method executed by the command in compact mode.</param>
        /// <param name="commandStrings">The strings that be used to trigger the command.</param>
        public CommandHandler(string name, ICommandView target, MethodInfo methodInfo, MethodInfo compactMethodInfo, params string[] commandStrings) : this(name, target, methodInfo, commandStrings)
        {
            CompactExecutor = new CompactExecutor(target, compactMethodInfo);
        }
    }
}
