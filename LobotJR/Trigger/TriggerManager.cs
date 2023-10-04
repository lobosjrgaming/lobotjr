﻿using LobotJR.Twitch.Model;
using System.Collections.Generic;

namespace LobotJR.Trigger
{
    /// <summary>
    /// Manages the responders that are automatically triggered on public messages.
    /// </summary>
    public class TriggerManager
    {
        private IEnumerable<ITriggerResponder> Responders;

        public TriggerManager(IEnumerable<ITriggerResponder> responders)
        {
            Responders = responders;
        }

        /// <summary>
        /// Processes all trigger responders for a given message. If the
        /// message matches the pattern for a responder, that responder returns
        /// the messages that the bot should respond with. These messages are
        /// sent to the public channel. Each message can only trigger a single
        /// responder.
        /// </summary>
        /// <param name="message">A message sent by a user.</param>
        /// <param name="user">The Twitch object of the user who sent the message.</param>
        /// <returns>An object containing all actions resulting from the trigger.</returns>
        public TriggerResult ProcessTrigger(string message, User user)
        {
            foreach (var responder in Responders)
            {
                var match = responder.Pattern.Match(message);
                if (match.Success)
                {
                    return responder.Process(match, user);
                }
            }
            return new TriggerResult() { Processed = false };
        }
    }
}
