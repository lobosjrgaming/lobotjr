using LobotJR.Twitch.Model;
using System.Threading.Tasks;

namespace LobotJR.Command.System.General
{
    /// <summary>
    /// System for handling events or commands that require confirmation.
    /// </summary>
    public class ConfirmationSystem : ISystemProcess
    {
        /// <summary>
        /// Event handler for confirmation events.
        /// </summary>
        /// <param name="user">The user that triggered the event.</param>
        public delegate void ConfirmationHandler(User user);
        /// <summary>
        /// Event fired when the player sends a confirm command.
        /// </summary>
        public event ConfirmationHandler Confirmed;
        /// <summary>
        /// Event fired when the player sends a cancel command.
        /// </summary>
        public event ConfirmationHandler Canceled;

        /// <summary>
        /// Trigger a confirm event.
        /// </summary>
        /// <param name="user">The user that triggered the event.</param>
        public void Confirm(User user)
        {
            Confirmed?.Invoke(user);
        }

        /// <summary>
        /// Trigger a cancel event.
        /// </summary>
        /// <param name="user">The user that triggered the event.</param>
        public void Cancel(User user)
        {
            Canceled?.Invoke(user);
        }

        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
