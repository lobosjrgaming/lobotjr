using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Twitch
{
    /// <summary>
    /// IRC client built for Twitch.
    /// </summary>
    public interface ITwitchIrcClient : IDisposable
    {
        /// <summary>
        /// Gets the time elapsed since the last message was received.
        /// </summary>
        TimeSpan IdleTime { get; }
        /// <summary>
        /// Disconnects the inner TCP client and reconnects to the server.
        /// </summary>
        void ForceReconnect();
        /// <summary>
        /// Connects the client to the twitch server, authenticates the chat
        /// user, and joins the channel of the broadcast user.
        /// </summary>
        /// <param name="secure">Whether or not to connect using SSL.</param>
        /// <returns>Whether or not the connection succeeded.</returns>
        Task<bool> Connect(bool secure = true);
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
        /// <summary>
        /// Injects a message into the incoming message stream. Used to allow
        /// the bot UI to send commands without going through the IRC
        /// connection.
        /// </summary>
        /// <param name="message">The text message to inject.</param>
        /// <param name="username">The Username to send from.</param>
        /// <param name="userid">The Twitch ID of the user to send from.</param>
        void InjectMessage(string message, string username, string userid);
    }
}
