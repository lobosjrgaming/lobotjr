using LobotJR.Shared.Channel;
using LobotJR.Shared.User;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Twitch
{
    /// <summary>
    /// Client that provide access to common Twitch API endpoints.
    /// </summary>
    public interface ITwitchClient
    {
        /// <summary>
        /// Initialization code for the IRC client. This requires an open
        /// database connection.
        /// </summary>
        void Initialize();
        /// <summary>
        /// Attempts to refresh the chat and broadcast tokens.
        /// </summary>
        /// <exception cref="Exception">Exception is thrown if the tokens fail to refresh.</exception>
        Task RefreshTokens();
        /// <summary>
        /// Queues a whisper to send.
        /// </summary>
        /// <param name="user">The user object of the user to send the message
        /// to.</param>
        /// <param name="message">The message to send.</param>
        void QueueWhisper(User user, string message);
        /// <summary>
        /// Queues a whisper to send to multiple users.
        /// </summary>
        /// <param name="users">An enumerable collection of user objects to
        /// send the whisper to.</param>
        /// <param name="message">The message to send.</param>
        void QueueWhisper(IEnumerable<User> users, string message);
        /// <summary>
        /// Processes the whisper queue, sending as many queued whispers as
        /// possible while remaining within the rate limits set by Twitch.
        /// </summary>
        Task ProcessQueue();
        /// <summary>
        /// Times out a user asynchronously.
        /// </summary>
        /// <param name="user">The user object of the user to timeout.</param>
        /// <param name="duration">The duration of the timeout. Null for a permanent ban.</param>
        /// <param name="message">The message to send along with the timeout.</param>
        /// <returns>True if the timeout was executed successfully.</returns>
        Task<bool> TimeoutAsync(User user, int? duration, string message);
        /// <summary>
        /// Gets the details of all subscribers to the broadcast user.
        /// </summary>
        /// <returns>A collection of subscription responses from Twitch.</returns>
        Task<IEnumerable<SubscriptionResponseData>> GetSubscriberListAsync();
        /// <summary>
        /// Gets a list of all chatters in the channel.
        /// </summary>
        /// <returns>A collection of chatter responses from Twitch.</returns>
        Task<IEnumerable<TwitchUserData>> GetChatterListAsync();
        /// <summary>
        /// Gets a list of all moderators for the channel.
        /// </summary>
        /// <returns>A collection of moderator users from Twitch.</returns>
        Task<IEnumerable<TwitchUserData>> GetModeratorListAsync();
        /// <summary>
        /// Gets a list of all VIPs for the channel.
        /// </summary>
        /// <returns>A collection of VIP users from Twitch.</returns>
        Task<IEnumerable<TwitchUserData>> GetVipListAsync();
        /// <summary>
        /// Gets the Twitch ids for a collection usernames.
        /// </summary>
        /// <param name="usernames">A collection of usernames.</param>
        /// <param name="logProgress">True if the method should create log
        /// entries showing lookup progress. Default is true. You should only
        /// set this to false if you are handling progress logging in the
        /// calling method.</param>
        /// <returns>A collection of Twitch user data responses.</returns>
        Task<IEnumerable<UserResponseData>> GetTwitchUsers(IEnumerable<string> usernames, bool logProgress = true);
    }
}
