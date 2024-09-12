using LobotJR.Command.Model.Twitch;
using LobotJR.Data;
using LobotJR.Shared.Channel;
using LobotJR.Shared.User;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.Controller.Twitch
{
    /// <summary>
    /// Controller for managing Twitch user data.
    /// </summary>
    public class UserController : IProcessor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly ITwitchClient TwitchClient;
        private readonly List<LookupRequest> LookupRequests = new List<LookupRequest>();
        private DateTime? LookupTimer = null;

        /// <summary>
        /// The time the user controller last fetched user ids from Twitch's
        /// API.
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        /// <summary>
        /// The user object for the broadcasting user.
        /// </summary>
        public User BroadcastUser { get; private set; }
        /// <summary>
        /// The user object for the chat user.
        /// </summary>
        public User ChatUser { get; private set; }
        /// <summary>
        /// Whether or not the stream is currently broadcasting live.
        /// </summary>
        public bool IsBroadcasting { get; private set; }

        public UserController(IConnectionManager connectionManager, SettingsManager settingsManager, ITwitchClient twitchClient)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            TwitchClient = twitchClient;
        }

        /// <summary>
        /// Gets the user object for a given username.
        /// </summary>
        /// <param name="username">The name of the user to retrieve.</param>
        /// <returns>The user object with the specified username, or null if
        /// none exists.</returns>
        public User GetUserByName(string username)
        {
            return ConnectionManager.CurrentConnection.Users.Read(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        /// <summary>
        /// Gets a collection of user objects from a list of usernames.
        /// </summary>
        /// <param name="names">The names of all users to lookup.</param>
        /// <returns>The user object for each user provided.</returns>
        public async Task<IEnumerable<User>> GetUsersByNames(params string[] names)
        {
            var allUsers = ConnectionManager.CurrentConnection.Users.Read().ToList();
            var allNames = allUsers.Select(x => x.Username).ToList();
            var knownNames = allNames.Intersect(names, StringComparer.OrdinalIgnoreCase).ToList();
            var missingNames = names.Except(allNames, StringComparer.OrdinalIgnoreCase).ToList();
            var known = new List<User>();
            if (knownNames.Any())
            {
                var knownUsers = allUsers.Join(knownNames, user => user.Username, name => name, (user, name) => user, StringComparer.OrdinalIgnoreCase);
                known.AddRange(knownUsers);
            }
            if (missingNames.Any())
            {
                var lookup = await TwitchClient.GetTwitchUsers(missingNames);
                var users = CreateUsers(lookup);
                known.AddRange(users);
            }
            return known;
        }

        private void RequestLookup(LookupRequest request)
        {
            LookupRequests.Add(request);
            if (LookupTimer == null)
            {
                LookupTimer = DateTime.Now;
            }
        }

        /// <summary>
        /// Gets the user object for a given username and executes a callback
        /// function with that user object. If the user already exists in the
        /// database, the callback will be executed on the next frame. If it
        /// doesn't exist in the database, it will be executed on the batched
        /// lookup.
        /// </summary>
        /// <param name="username">The name of the user to retrieve.</param>
        /// <param name="callback">The callback method to execute after the
        /// user lookup call.</param>
        public void GetUserByNameAsync(string username, Action<User> callback)
        {
            var user = ConnectionManager.CurrentConnection.Users.Read(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (user != null)
            {
                callback(user);
            }
            else
            {
                RequestLookup(new LookupRequest(username, callback));
            }
        }

        /// <summary>
        /// Gets the user object for a given Twitch id.
        /// </summary>
        /// <param name="twitchId">The twitch id of the user to retrieve.</param>
        /// <returns>The user object with the specified id, or null if
        /// none exists.</returns>
        public User GetUserById(string twitchId)
        {
            return ConnectionManager.CurrentConnection.Users.Read(x => x.TwitchId.Equals(twitchId)).FirstOrDefault();
        }

        /// <summary>
        /// Gets an existing user by their Twitch id. If no user with that id
        /// exists in the database, an entry will be created. If the username
        /// in the database doesn't match the provided username, it will be
        /// updated.
        /// </summary>
        /// <param name="twitchId">The Twitch id of the user.</param>
        /// <param name="username">The user's Twitch display name.</param>
        /// <param name="isSub">True if the user is a channel subscriber.</param>
        /// <param name="isVip">True if the user is a channel VIP.</param>
        /// <param name="isMod">True if the user is a channel moderator.</param>
        /// <returns>The user object from the database.</returns>
        public User GetOrCreateUser(string twitchId, string username)
        {
            var existing = ConnectionManager.CurrentConnection.Users.Read(x => twitchId.Equals(x.TwitchId)).FirstOrDefault();
            if (existing == null)
            {
                existing = new User() { Username = username, TwitchId = twitchId };
                ConnectionManager.CurrentConnection.Users.Create(existing);
            }
            else if (!existing.Username.Equals(username))
            {
                existing.Username = username;
            }
            return existing;
        }

        /// <summary>
        /// Updates the user's mod, sub, and vip status based on the flags on
        /// the incoming message. Note that this should only be used for public
        /// messages, as whisper messages will have different flags set.
        /// </summary>
        /// <param name="user">The object for the user who sent the message.</param>
        /// <param name="message">The object for the message that was sent.</param>
        public void UpdateUser(User user, IrcMessage message)
        {
            user.IsMod = message.IsMod;
            user.IsVip = message.IsVip;
            user.IsSub = message.IsSub;
        }

        /// <summary>
        /// Sets the broadcast and chat user for the bot. Sets IsAdmin flag for
        /// both users if unset.
        /// </summary>
        /// <param name="broadcastUser">The user object for the broadcasting user.</param>
        /// <param name="chatUser">The user object for the chat user.</param>
        public void SetBotUsers(User broadcastUser, User chatUser)
        {
            BroadcastUser = broadcastUser;
            ChatUser = chatUser;
            if (!broadcastUser.IsAdmin)
            {
                broadcastUser.IsAdmin = true;
            }
            if (!chatUser.IsAdmin)
            {
                chatUser.IsAdmin = true;
            }
        }

        /// <summary>
        /// Sets a user's subscriber flag.
        /// </summary>
        /// <param name="user">The user to update.</param>
        public void SetSub(User user)
        {
            user.IsSub = true;
        }

        private void SyncLists(IEnumerable<User> allUsers, IEnumerable<string> target, Func<User, bool> checkLambda, Action<User, bool> updateLambda)
        {
            var targetUsers = target.Join(allUsers, t => t, u => u.TwitchId, (t, u) => u).ToList();
            var existingFlagged = allUsers.Where(x => checkLambda(x)).ToList();
            var notFlagged = allUsers.Except(existingFlagged).ToList();
            var toRemove = existingFlagged.Except(targetUsers).ToList();
            var toAdd = targetUsers.Except(existingFlagged).ToList();
            foreach (var user in toRemove)
            {
                updateLambda(user, false);
            }
            foreach (var user in toAdd)
            {
                updateLambda(user, true);
            }
        }

        private IEnumerable<string> UpdateDisplayNames(Dictionary<string, string> userPairs, Dictionary<string, User> databaseUserDict, IEnumerable<string> existingIds)
        {
            var userDict = userPairs.ToDictionary(x => x.Key, x => x.Value);
            var updateIds = existingIds.Where(x => !databaseUserDict[x].Username.Equals(userDict[x])).ToList();
            if (updateIds.Any())
            {
                Logger.Info("Updating display names for {count} users.", updateIds.Count());
                var cursor = 0;
                var chunkSize = 1000;
                do
                {
                    ConnectionManager.CurrentConnection.Users.BeginTransaction();
                    foreach (var toUpdate in updateIds.Skip(cursor).Take(chunkSize))
                    {
                        var user = databaseUserDict[toUpdate];
                        user.Username = userDict[toUpdate];
                        ConnectionManager.CurrentConnection.Users.Update(user);
                    }
                    ConnectionManager.CurrentConnection.Users.Commit();
                    cursor += chunkSize;
                }
                while (cursor < updateIds.Count);
            }
            return updateIds;
        }

        private void ProcessUpdate(IEnumerable<TwitchUserData> mods, IEnumerable<TwitchUserData> vips, IEnumerable<SubscriptionResponseData> subs)
        {
            IEnumerable<KeyValuePair<string, string>> userPairs = new List<KeyValuePair<string, string>>();
            if (mods.Any())
            {
                userPairs = userPairs.Union(mods.Distinct().ToDictionary(x => x.UserId, x => x.UserName));
            }
            if (vips.Any())
            {
                userPairs = userPairs.Union(vips.Distinct().ToDictionary(x => x.UserId, x => x.UserName));
            }
            if (subs.Any())
            {
                userPairs = userPairs.Union(subs.Distinct().ToDictionary(x => x.UserId, x => x.UserName));
            }

            var userDict = userPairs.ToDictionary(x => x.Key, x => x.Value);
            var databaseUsers = ConnectionManager.CurrentConnection.Users.Read().ToList();
            var databaseUserDict = databaseUsers.ToDictionary(x => x.TwitchId, x => x);
            var existingIds = databaseUserDict.Keys.Intersect(userPairs.Select(x => x.Key)).ToList();
            var newIds = userDict.Keys.Except(existingIds).ToList();
            UpdateDisplayNames(userDict, databaseUserDict, existingIds);
            if (newIds.Any())
            {
                ConnectionManager.CurrentConnection.Users.BatchCreate(newIds.Select(x => new User() { TwitchId = x, Username = userDict[x] }), 1000, Logger, "user");
            }

            if (mods.Any())
            {
                SyncLists(databaseUsers, mods.Select(x => x.UserId), x => x.IsMod, (u, v) => u.IsMod = v);
            }
            else
            {
                Logger.Warn("Null response attempting to retrieve moderator list.");
            }

            if (vips.Any())
            {
                SyncLists(databaseUsers, vips.Select(x => x.UserId), x => x.IsVip, (u, v) => u.IsVip = v);
            }
            else
            {
                Logger.Warn("Null response attempting to retrieve vip list.");
            }

            if (subs.Any())
            {
                SyncLists(databaseUsers, subs.Select(x => x.UserId), x => x.IsSub, (u, v) => u.IsSub = v);
            }
            else
            {
                Logger.Warn("Null response attempting to retrieve subscriber list.");
            }
            ConnectionManager.CurrentConnection.Users.Commit();

            var updateTime = (DateTime.Now - LastUpdate).TotalMilliseconds;
            Logger.Info("User database updated in {time} milliseconds.", updateTime);
            if (updateTime > 5000)
            {
                Logger.Warn("Database update took a long time, this might indicate a problem...");
            }
        }

        private IEnumerable<User> CreateUsers(IEnumerable<UserResponseData> users)
        {
            var db = ConnectionManager.CurrentConnection;
            db.Commit();
            db.Users.BeginTransaction();
            Logger.Info("Writing {count} user records to database.", users.Count());
            var total = users.Count();
            var output = new List<User>();
            var startTime = DateTime.Now;
            var logTime = DateTime.Now;
            var processed = 0;

            var tempUsers = users.ToDictionary(x => x.Id, x => x.DisplayName);
            var all = db.Users.Read().ToDictionary(x => x.TwitchId, x => x);
            var idsToAdd = tempUsers.Keys.Except(all.Keys);
            var idsToUpdate = tempUsers.Keys.Intersect(all.Keys);
            foreach (var id in idsToUpdate)
            {
                if (DateTime.Now - logTime > TimeSpan.FromSeconds(5))
                {
                    var elapsed = DateTime.Now - startTime;
                    var estimate = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / processed * total) - elapsed;
                    Logger.Info("{count} total user records written. {elapsed} time elapsed, {estimate} estimated remaining.", processed, elapsed.ToString("hh\\:mm\\:ss"), estimate.ToString("hh\\:mm\\:ss"));
                    logTime = DateTime.Now;
                }
                var existing = all[id];
                var update = tempUsers[id];
                if (!existing.Username.Equals(update))
                {
                    existing.Username = update;
                    db.Users.Update(existing);
                    output.Add(existing);
                }
                processed++;
            }
            db.Users.Commit();
            var usersToAdd = idsToAdd.Select(x => new User(tempUsers[x], x)).ToList();
            var batch = db.Users.BatchCreate(usersToAdd, 1000, Logger, "user");
            output.AddRange(batch);
            Logger.Info("Data for {count} users inserted into the database!", output.Count);
            return output;
        }

        private void ProcessLookups(IEnumerable<UserResponseData> users)
        {
            LookupTimer = null;

            var newUsers = CreateUsers(users);
            var newUserNames = newUsers.Select(x => x.Username);

            var matchedRequests = LookupRequests.Where(x => newUserNames.Contains(x.Username, StringComparer.OrdinalIgnoreCase));

            foreach (var request in matchedRequests)
            {
                var user = newUsers.FirstOrDefault(x => x.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));
                request.Callback(user);
            }

            var failedRequests = LookupRequests.Except(matchedRequests);
            if (failedRequests.Any())
            {
                Logger.Warn("Lookup requests for the following users failed: {userList}", failedRequests.Select(x => x.Username));
                Logger.Warn("This probably shouldn't happen, please contact the developer");
            }
            LookupRequests.Clear();
        }

        /// <summary>
        /// Forces an update of just the viewer list. This does not sync mod,
        /// sub, or vip status.
        /// </summary>
        public async Task<IEnumerable<User>> GetViewerList()
        {
            var chatters = await TwitchClient.GetChatterListAsync();
            if (chatters.Any())
            {
                var databaseUsers = ConnectionManager.CurrentConnection.Users.Read();
                var databaseUserDict = databaseUsers.ToDictionary(x => x.TwitchId, x => x);
                var userDict = chatters.ToDictionary(x => x.UserId, x => x.UserName);
                var foundIds = userDict.Keys.Intersect(databaseUserDict.Keys).ToList();
                return foundIds.Select(x => databaseUserDict[x]);
            }
            return Array.Empty<User>();
        }

        public async Task Process()
        {
            var elapsed = DateTime.Now - LastUpdate;
            var settings = SettingsManager.GetAppSettings();
            if (elapsed > TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime))
            {
                LastUpdate = DateTime.Now;
                var mods = await TwitchClient.GetModeratorListAsync();
                var vips = await TwitchClient.GetVipListAsync();
                var subs = await TwitchClient.GetSubscriberListAsync();
                ProcessUpdate(mods, vips, subs);
            }

            if (LookupTimer != null && DateTime.Now - LookupTimer > TimeSpan.FromSeconds(settings.UserLookupBatchTime))
            {
                var users = await TwitchClient.GetTwitchUsers(LookupRequests.Select(x => x.Username));
                ProcessLookups(users);
            }
        }
    }
}
