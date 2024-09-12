using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Trigger
{
    /// <summary>
    /// Manages the responders that are automatically triggered on public messages.
    /// </summary>
    public class TriggerManager
    {
        private readonly IEnumerable<ITriggerResponder> Responders;

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
                    var response = responder.Process(match, user);
                    response.Sender = user;
                    return response;
                }
            }
            return new TriggerResult() { Sender = user, Processed = false };
        }

        /// <summary>
        /// Processes a command result object, adding all output to the logs
        /// and sending any whispers or chat messages triggered by the command.
        /// </summary>
        /// <param name="result">The command result object.</param>
        /// <param name="irc">The twitch irc client to send messages through.</param>
        /// <param name="twitchClient">The twitch API client to send whispers through.</param>
        public async Task HandleResult(TriggerResult result, ITwitchIrcClient irc, ITwitchClient twitchClient)
        {
            if (result.Messages != null)
            {
                foreach (var responseMessage in result.Messages)
                {
                    irc.QueueMessage(responseMessage);
                }
            }
            if (result.Whispers != null)
            {
                foreach (var triggerWhisper in result.Whispers)
                {
                    twitchClient.QueueWhisper(result.Sender, triggerWhisper);
                }
            }
            if (result.TimeoutSender)
            {
                await twitchClient.TimeoutAsync(result.Sender, 1, result.TimeoutMessage);
            }
        }
    }
}
