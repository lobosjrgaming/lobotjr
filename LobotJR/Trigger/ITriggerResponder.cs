using LobotJR.Twitch.Model;
using System.Text.RegularExpressions;

namespace LobotJR.Trigger
{
    /// <summary>
    /// Interface for a chat trigger. These differ from chat commands in a few
    /// key ways:
    ///     1. Triggers only work in general chat, not whispers.
    ///     2. Triggers do not have to start with the command prefix character.
    ///     3. Triggers are matched with regular expressions.
    /// The result of these differences is that triggers are generally more
    /// expensive to process, and have less access to information about the
    /// user that tripped the trigger.
    /// Triggers are intended for moderation or other simple responses from the
    /// bot that minimally rely on user context.
    /// </summary>
    public interface ITriggerResponder
    {
        /// <summary>
        /// The regular expression that must be matched to trip the trigger.
        /// </summary>
        Regex Pattern { get; }
        /// <summary>
        /// The method to execute when a user trips the trigger.
        /// </summary>
        /// <param name="match">The regular expression's match for this
        /// message.</param>
        /// <param name="user">The user that tripped the trigger.</param>
        /// <returns>A trigger result object which contains instructions for
        /// how to respond to the trigger.</returns>
        TriggerResult Process(Match match, User user);
    }
}
