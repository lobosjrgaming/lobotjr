using LobotJR.Data;
using LobotJR.Shared.Authentication;
using LobotJR.Shared.Channel;
using LobotJR.Shared.Chat;
using LobotJR.Shared.Client;
using LobotJR.Shared.User;
using LobotJR.Shared.Utility;
using LobotJR.Twitch.Model;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LobotJR.Twitch
{
    /// <summary>
    /// Client that provide access to common Twitch API endpoints.
    /// </summary>
    public class TwitchClient : ITwitchClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<string> Blacklist = new List<string>();

        private readonly WhisperQueue Queue;
        private readonly ClientData ClientData;
        private readonly TokenData TokenData;

        public TwitchClient(IConnectionManager connectionManager, SettingsManager settingsManager, ClientData clientData, TokenData tokenData)
        {
            Queue = new WhisperQueue(connectionManager, settingsManager, 3, 100);
            ClientData = clientData;
            TokenData = tokenData;
        }

        /// <summary>
        /// Initializes properties that require database access.
        /// </summary>
        public void Initialize()
        {
            Queue.UpdateMaxRecipients();
        }

        private async Task<RestResponse<TokenResponse>> RefreshToken(TokenResponse token)
        {
            if (token.ExpirationDate < DateTime.Now)
            {
                return await AuthToken.Refresh(ClientData.ClientId, ClientData.ClientSecret, token.RefreshToken);
            }
            return null;
        }

        /// <summary>
        /// Attempts to refresh the chat and broadcast tokens.
        /// </summary>
        /// <exception cref="Exception">Exception is thrown if the tokens fail to refresh.</exception>
        public async Task RefreshTokens()
        {
            var updated = false;
            var response = await RefreshToken(TokenData.ChatToken);
            if (response != null)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.ErrorException != null || response.ErrorMessage != null)
                    {
                        throw new Exception($"Encountered an exception trying to refresh the token for {TokenData.ChatUser}. {response.ErrorMessage}. {response.ErrorException}");
                    }
                    throw new Exception($"Encountered an unexpected response trying to refresh the token for {TokenData.ChatUser}. {response.StatusCode}: {response.Content}");
                }
                TokenData.ChatToken.CopyFrom(response.Data);
                updated = true;
            }
            response = await RefreshToken(TokenData.BroadcastToken);
            if (response != null)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.ErrorException != null || response.ErrorMessage != null)
                    {
                        throw new Exception($"Encountered an exception trying to refresh the token for {TokenData.ChatUser}. {response.ErrorMessage}. {response.ErrorException}");
                    }
                    throw new Exception($"Encountered an unexpected response trying to refresh the token for {TokenData.BroadcastUser}. {response.StatusCode}: {response.Content}");
                }
                TokenData.BroadcastToken.CopyFrom(response.Data);
                updated = true;
            }
            if (updated)
            {
                FileUtils.WriteTokenData(TokenData);
            }
        }

        /// <summary>
        /// Queues a whisper to send.
        /// </summary>
        /// <param name="user">The user object of the user to send the message
        /// to.</param>
        /// <param name="message">The message to send.</param>
        public void QueueWhisper(User user, string message)
        {
            if (user != null && !Blacklist.Contains(user.Username))
            {
                Queue.Enqueue(user, message, DateTime.Now);
            }
        }

        /// <summary>
        /// Queues a whisper to send to multiple users.
        /// </summary>
        /// <param name="users">An enumerable collection of user objects to
        /// send the whisper to.</param>
        /// <param name="message">The message to send.</param>
        public void QueueWhisper(IEnumerable<User> users, string message)
        {
            foreach (var user in users)
            {
                QueueWhisper(user, message);
            }
        }

        private async Task<HttpStatusCode> WhisperAsync(User user, string message)
        {
            var result = await Whisper.Post(TokenData.ChatToken, ClientData, TokenData.ChatId, user.TwitchId, message);
            return result?.StatusCode ?? (HttpStatusCode)0;
        }

        /// <summary>
        /// Processes the whisper queue, sending as many queued whispers as
        /// possible while remaining within the rate limits set by Twitch.
        /// </summary>
        public async Task ProcessQueue()
        {
            var canSend = Queue.TryGetMessage(out var message);
            while (canSend)
            {
                var result = await WhisperAsync(message.User, message.Message);
                if (result == HttpStatusCode.NoContent)
                {
                    Queue.ReportSuccess(message);
                }
                else if (result == HttpStatusCode.NotFound)
                {
                    Logger.Warn("User name {user} returned id {id} from Twitch. Twitch says this user id doesn't exist. User {user} has been blacklisted from whispers.", message.User.Username, message.User.TwitchId);
                    Blacklist.Add(message.User.TwitchId);
                }
                else if (result == (HttpStatusCode)429)
                {
                    Logger.Error("We sent too many whispers. Whispers have been turned off for one minute, and no more unique recipients will be allowed.");
                    Logger.Debug("Max whisper recipient limit has been updated to {count}.", Queue.WhisperRecipients.Count);
                    Logger.Debug("See below for details on the current state of the whisper queue.");
                    Logger.Debug(Queue.Debug());
                    Queue.FreezeQueue();
                    break;
                }
                else
                {
                    Logger.Error("Something went wrong trying to send a whisper. Twitch response: {response}", result);
                }
                canSend = Queue.TryGetMessage(out message);
            }
        }

        /// <summary>
        /// Times out a user asynchronously.
        /// </summary>
        /// <param name="user">The user object of the user to timeout.</param>
        /// <param name="duration">The duration of the timeout. Null for a permanent ban.</param>
        /// <param name="message">The message to send along with the timeout.</param>
        /// <returns>True if the timeout was executed successfully.</returns>
        public async Task<bool> TimeoutAsync(User user, int? duration, string message)
        {
            var result = await BanUser.Post(TokenData.ChatToken, ClientData, TokenData.BroadcastId, TokenData.ChatId, user.TwitchId, duration, message);
            return result == HttpStatusCode.OK;
        }

        /// <summary>
        /// Gets the details of all subscribers to the broadcast user.
        /// </summary>
        /// <returns>A collection of subscription responses from Twitch.</returns>
        public async Task<IEnumerable<SubscriptionResponseData>> GetSubscriberListAsync()
        {
            var results = await Subscriptions.GetAll(TokenData.BroadcastToken, ClientData, TokenData.BroadcastId);
            if (results.Any(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized))
            {
                var failure = results.FirstOrDefault(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized);
                Logger.Warn("Encountered an unexpected response retrieving subscribers: {statusCode}: {content}", failure.StatusCode, failure.Content);
                return new List<SubscriptionResponseData>();
            }
            return results.Where(x => x.Data != null && x.Data.Data != null).SelectMany(x => x.Data.Data);
        }

        /// <summary>
        /// Gets a list of all chatters in the channel.
        /// </summary>
        /// <returns>A collection of chatter responses from Twitch.</returns>
        public async Task<IEnumerable<TwitchUserData>> GetChatterListAsync()
        {
            var results = await Chatters.GetAll(TokenData.ChatToken, ClientData, TokenData.BroadcastId, TokenData.ChatId);
            if (results.Any(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized))
            {
                var failure = results.FirstOrDefault(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized);
                Logger.Warn("Encountered an unexpected response retrieving chatters: {statusCode}: {content}", failure.StatusCode, failure.Content);
                return new List<TwitchUserData>();
            }
            return results.Where(x => x.Data != null && x.Data.Data != null).SelectMany(x => x.Data.Data);
        }

        /// <summary>
        /// Gets a list of all moderators for the channel.
        /// </summary>
        /// <returns>A collection of moderator users from Twitch.</returns>
        public async Task<IEnumerable<TwitchUserData>> GetModeratorListAsync()
        {
            var results = await Moderators.GetAll(TokenData.BroadcastToken, ClientData, TokenData.BroadcastId);
            if (results.Any(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized))
            {
                var failure = results.FirstOrDefault(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized);
                Logger.Warn("Encountered an unexpected response retrieving moderators: {statusCode}: {content}", failure.StatusCode, failure.Content);
                return new List<TwitchUserData>();
            }
            return results.Where(x => x.Data != null && x.Data.Data != null).SelectMany(x => x.Data.Data);
        }

        /// <summary>
        /// Gets a list of all VIPs for the channel.
        /// </summary>
        /// <returns>A collection of VIP users from Twitch.</returns>
        public async Task<IEnumerable<TwitchUserData>> GetVipListAsync()
        {
            var results = await VIPs.GetAll(TokenData.BroadcastToken, ClientData, TokenData.BroadcastId);
            if (results.Any(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized))
            {
                var failure = results.FirstOrDefault(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized);
                Logger.Warn("Encountered an unexpected response retrieving VIPs: {statusCode}: {content}", failure.StatusCode, failure.Content);
                return new List<TwitchUserData>();
            }
            return results.Where(x => x.Data != null && x.Data.Data != null).SelectMany(x => x.Data.Data);
        }

        /// <summary>
        /// Gets the Twitch ids for a collection usernames.
        /// </summary>
        /// <param name="usernames">A collection of usernames.</param>
        /// <param name="logProgress">True if the method should create log
        /// entries showing lookup progress. Default is true. You should only
        /// set this to false if you are handling progress logging in the
        /// calling method.</param>
        /// <returns>A collection of Twitch user data responses.</returns>
        public async Task<IEnumerable<UserResponseData>> GetTwitchUsers(IEnumerable<string> usernames, bool logProgress = true)
        {
            var results = await Users.Get(TokenData.BroadcastToken, ClientData, usernames, logProgress);
            if (results.Any(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized))
            {
                var failures = results.Where(x => x.StatusCode != HttpStatusCode.OK && x.StatusCode != HttpStatusCode.Unauthorized);
                foreach (var failure in failures)
                {
                    Logger.Warn("Encountered an unexpected response looking up userids: {statusCode}: {content}. From: {request}", failure.StatusCode, failure.Content, failure.ResponseUri);
                }
                return new List<UserResponseData>();
            }
            return results.Where(x => x.Data != null && x.Data.Data != null).SelectMany(x => x.Data.Data);
        }
    }
}
