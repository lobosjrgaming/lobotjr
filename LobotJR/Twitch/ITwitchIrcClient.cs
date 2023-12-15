using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LobotJR.Twitch
{
    /// <summary>
    /// IRC client built for Twitch.
    /// </summary>
    public interface ITwitchIrcClient : IDisposable
    {
        /// <summary>
        /// Disposes and recreates the inner tcp client to allow for proper reconnects.
        /// </summary>
        void Restart();

        /// <summary>
        /// Connects the client to the twitch server, authenticates the chat
        /// user, and joins the channel of the broadcast user.
        /// </summary>
        /// <param name="secure">Whether or not to connect using SSL.</param>
        /// <returns>Whether or not the connection succeeded.</returns>
        Task<bool> Connect(bool secure = true);

        /// <summary>
        /// Starts a thread that listens for messages and sends message events.
        /// </summary>
        /// <returns>The cancellation token source used to cancel the thread.</returns>
        CancellationTokenSource Start();

        /// <summary>
        /// Sends any available queued messages and processes any incoming messages.
        /// </summary>
        /// <returns>A collection of messages that have been received since the
        /// last time this method was called.</returns>
        Task<IEnumerable<IrcMessage>> Process();

        /// <summary>
        /// Queues a message to send out on the IRC client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void QueueMessage(string message);
    }
}
