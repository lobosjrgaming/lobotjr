using LobotJR.Twitch.Model;
using System.Collections.Generic;

namespace LobotJR.Command.View
{
    /// <summary>
    /// Handler for push notification events.
    /// </summary>
    /// <param name="user">The user to push to, or null for public pushes.</param>
    /// <param name="commandResult">The CommandResult object to process.</param>
    public delegate void PushNotificationHandler(User user, CommandResult commandResult);

    /// <summary>
    /// Holds the view logic for a grouping of commands.
    /// </summary>
    public interface ICommandView
    {
        /// <summary>
        /// The name of the view used to group commands.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A collection containing all commands within this view.
        /// </summary>
        IEnumerable<CommandHandler> Commands { get; }
    }

    /// <summary>
    /// Interface for views that send push notifications. Used to send messages
    /// to players not in direct response to an incoming command.
    /// </summary>
    public interface IPushNotifier
    {
        /// <summary>
        /// Event that this view will raise when a push notification needs to be sent.
        /// </summary>
        event PushNotificationHandler PushNotification;
    }
}
