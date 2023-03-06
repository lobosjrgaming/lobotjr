﻿using System;
using System.Collections.Generic;

namespace LobotJR.Command
{
    /// <summary>
    /// Class containing the results of an attempt to process a command.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Whether or not the message matched a loaded command, and that command was executed.
        /// </summary>
        public bool Processed { get; set; }
        /// <summary>
        /// Whether or not to timeout the sender of the command.
        /// </summary>
        public bool TimeoutSender { get; set; }
        /// <summary>
        /// The message to send to the user as part of the timeout.
        /// </summary>
        public string TimeoutMessage { get; set; }
        /// <summary>
        /// The responses to send back to the user who issued the command.
        /// </summary>
        public IList<string> Responses { get; set; }
        /// <summary>
        /// Messages to send to chat as a result of this command.
        /// </summary>
        public IList<string> Messages { get; set; }
        /// <summary>
        /// Errors generated by the command, regardless of the success of the command's execution.
        /// </summary>
        public IList<Exception> Errors { get; set; }
        /// <summary>
        /// Debug messages to output from the command.
        /// </summary>
        public IList<string> Debug { get; set; }

        public CommandResult()
        {
            Processed = false;
        }

        public CommandResult(params string[] responses)
        {
            Processed = true;
            Responses = new List<string>(responses);
        }

        public CommandResult(bool processed, IEnumerable<Exception> errors)
        {
            Processed = processed;
            if (errors != null)
            {
                Errors = new List<Exception>(errors);
            }
        }
    }
}
