using LobotJR.Data;
using LobotJR.Shared.Channel;
using LobotJR.Shared.User;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Twitch
{
    public class LookupRequest
    {
        public string Username { get; set; }
        public Func<User, CommandResult> Callback { get; set; }

        public LookupRequest(string username, Func<User, CommandResult> callback)
        {
            Username = username;
            Callback = callback;
        }
    }

    /// <summary>
    /// System for managing Twitch user data.
    /// </summary>
    public class UserSystem : ISystem
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IRepository<User> Users;
        private readonly AppSettings Settings;
        private readonly TwitchClient TwitchClient;
        private DateTime? LookupTimer = null;
        private List<LookupRequest> LookupRequests = new List<LookupRequest>();

        /// <summary>
        /// The time the user system last fetched user ids from Twitch's API.
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public IEnumerable<string> Viewers { get; private set; } = Enumerable.Empty<string>();

        public UserSystem(IRepositoryManager repositoryManager, TwitchClient twitchClient)
        {
            Users = repositoryManager.Users;
            Settings = repositoryManager.AppSettings.Read().First();
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
            return Users.Read(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        /// <summary>
        /// Gets a collection of user objects from a list of usernames.
        /// </summary>
        /// <param name="names">The names of all users to lookup.</param>
        /// <returns>The user object for each user provided.</returns>
        public async Task<IEnumerable<User>> GetUsersByNames(params string[] names)
        {
            var known = Users.Read(x => names.Contains(x.Username, StringComparer.OrdinalIgnoreCase));
            var missing = names.Except(known.Select(x => x.Username), StringComparer.OrdinalIgnoreCase);
            if (missing.Any())
            {
                var lookup = await TwitchClient.GetTwitchUsers(missing);
                var users = CreateUsers(lookup);
                known = known.Concat(users);
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
        /// Gets the user object for a given username. If it doesn't exist in
        /// the database, add it to the next lookup batch.
        /// </summary>
        /// <param name="username">The name of the user to retrieve.</param>
        /// <param name="callback">The callback method to execute after the
        /// user lookup call.</param>
        /// <returns>The user object with the specified username, or null if
        /// none exists.</returns>
        public CommandResult GetUserByName(string username, Func<User, CommandResult> callback)
        {
            var user = Users.Read(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (user != null)
            {
                return callback(user);
            }
            RequestLookup(new LookupRequest(username, callback));
            return null;
        }

        /// <summary>
        /// Gets the user object for a given Twitch id.
        /// </summary>
        /// <param name="twitchId">The twitch id of the user to retrieve.</param>
        /// <returns>The user object with the specified id, or null if
        /// none exists.</returns>
        public User GetUserById(string twitchId)
        {
            return Users.Read(x => x.TwitchId.Equals(twitchId)).FirstOrDefault();
        }

        /// <summary>
        /// Gets an existing user by their Twitch id. If no user with that id
        /// exists in the database, an entry will be created. If the username
        /// in the database doesn't match the provided username, it will be
        /// updated.
        /// </summary>
        /// <param name="twitchId">The Twitch id of the user.</param>
        /// <param name="username">The user's Twitch display name.</param>
        /// <returns>The user object from the database.</returns>
        public User GetOrCreateUser(string twitchId, string username)
        {
            var existing = Users.Read(x => twitchId.Equals(x.TwitchId)).FirstOrDefault();
            if (existing == null)
            {
                existing = new User() { Username = username, TwitchId = twitchId };
                Users.Create(existing);
                Users.Commit();
            }
            else if (!existing.Username.Equals(username))
            {
                existing.Username = username;
                Users.Update(existing);
                Users.Commit();
            }
            return existing;
        }

        private void SyncLists(IEnumerable<string> target, Func<User, bool> checkLambda, Action<User, bool> updateLambda)
        {
            var toRemove = Users.Read(x => checkLambda(x) && !target.Any(y => y.Equals(x.TwitchId)));
            var toAdd = Users.Read(x => !checkLambda(x) && target.Any(y => y.Equals(x.TwitchId)));
            foreach (var user in toRemove)
            {
                updateLambda(user, false);
                Users.Update(user);
            }
            foreach (var user in toAdd)
            {
                updateLambda(user, true);
                Users.Update(user);
            }
        }

        private void ProcessUpdate(IEnumerable<TwitchUserData> mods, IEnumerable<TwitchUserData> vips, IEnumerable<SubscriptionResponseData> subs, IEnumerable<TwitchUserData> chatters)
        {
            LastUpdate = DateTime.Now;
            var allUsers = mods.ToDictionary(x => x.UserId, x => x.UserName)
                .Concat(vips.ToDictionary(x => x.UserId, x => x.UserName))
                .Concat(subs.ToDictionary(x => x.UserId, x => x.UserName))
                .Concat(chatters.ToDictionary(x => x.UserId, x => x.UserName)).ToDictionary(x => x.Key, x => x.Value);

            var existingUsers = Users.Read(x => allUsers.Keys.Contains(x.TwitchId));
            var newUsers = allUsers.Except(existingUsers.ToDictionary(x => x.TwitchId, x => x.Username));

            foreach (var user in newUsers)
            {
                Users.Create(new User() { TwitchId = user.Key, Username = user.Value });
            }
            Users.Commit();

            if (mods != null)
            {
                SyncLists(mods.Select(x => x.UserId), x => x.IsMod, (u, v) => u.IsMod = v);
            }
            else
            {
                Logger.Warn("Null response attempting to retrieve moderator list.");
            }

            if (vips != null)
            {
                SyncLists(vips.Select(x => x.UserId), x => x.IsVip, (u, v) => u.IsVip = v);
            }
            else
            {
                Logger.Warn("Null response attempting to retrieve vip list.");
            }

            if (subs != null)
            {
                SyncLists(subs.Select(x => x.UserId), x => x.IsSub, (u, v) => u.IsSub = v);
            }
            else
            {
                Logger.Warn("Null response attempting to retrieve subscriber list.");
            }
            Users.Commit();

            if (chatters != null)
            {
                Viewers = chatters.Where(x => x != null).Select(x => x.UserId).ToList();
            }
            else
            {
                Logger.Warn("Null response attempting to retrieve viewer list.");
            }

            Logger.Info("User database updated in {time} milliseconds.", (DateTime.Now - LastUpdate).TotalMilliseconds);
        }

        private IEnumerable<User> CreateUsers(IEnumerable<UserResponseData> users)
        {
            var output = new List<User>();
            foreach (var user in users)
            {
                var request = LookupRequests.FirstOrDefault(x => x.Username.Equals(user.DisplayName));
                var existing = Users.Read(x => x.TwitchId.Equals(user.Id)).FirstOrDefault();
                if (existing != null)
                {
                    existing.Username = user.DisplayName;
                    Users.Update(existing);
                    output.Add(existing);
                }
                else
                {
                    var newUser = new User()
                    {
                        TwitchId = user.Id,
                        Username = user.DisplayName,
                    };
                    Users.Create(newUser);
                    output.Add(newUser);
                }
            }
            Users.Commit();
            return output;
        }

        private void ProcessLookups(IEnumerable<UserResponseData> users)
        {
            LookupTimer = null;

            var newUsers = CreateUsers(users);

            foreach (var request in LookupRequests)
            {
                var match = newUsers.FirstOrDefault(x => x.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    var user = Users.Read(x => match.Id.Equals(x.TwitchId)).FirstOrDefault();
                    request.Callback(user);
                }
            }
        }

        public async Task Process(bool broadcasting)
        {
            var elapsed = DateTime.Now - LastUpdate;
            if (broadcasting && elapsed > TimeSpan.FromMinutes(Settings.UserDatabaseUpdateTime))
            {
                var mods = await TwitchClient.GetModeratorListAsync();
                var vips = await TwitchClient.GetVipListAsync();
                var subs = await TwitchClient.GetSubscriberListAsync();
                var chatters = await TwitchClient.GetChatterListAsync();
                ProcessUpdate(mods, vips, subs, chatters);
            }

            if (LookupTimer != null && DateTime.Now - LookupTimer > TimeSpan.FromSeconds(Settings.UserLookupBatchTime))
            {
                var users = await TwitchClient.GetTwitchUsers(LookupRequests.Select(x => x.Username));
                ProcessLookups(users);
            }
        }
    }
}
