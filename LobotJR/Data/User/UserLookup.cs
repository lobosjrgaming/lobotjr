using LobotJR.Shared.Authentication;
using LobotJR.Shared.Client;
using LobotJR.Shared.User;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Data.User
{
    public class CacheUpdateResult
    {
        public List<string> UpdatedUsers { get; set; } = new List<string>();
        public List<string> FailedUsers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Provides a lookup for users by id or username. If a username is not
    /// found, their id will be looked up from the twitch API, and either their
    /// username in the cache will be updated or an entry will be added for
    /// them.
    /// </summary>
    public class UserLookup
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private DateTime lastUpdate = DateTime.Now;
        private readonly List<string> cacheMisses = new List<string>();

        public int UpdateTime { get; set; }
        public IRepository<Twitch.Model.User> UserMap { get; private set; }

        public UserLookup(IRepositoryManager repositoryManager)
        {
            UserMap = repositoryManager.Users;
        }

        /// <summary>
        /// Gets the username associated with a twitch id.
        /// </summary>
        /// <param name="id">The twitch id.</param>
        /// <returns>The username.</returns>
        public string GetUsername(string id)
        {
            var entry = UserMap.Read(x => x.TwitchId.Equals(id)).FirstOrDefault();
            return entry?.Username;
        }

        /// <summary>
        /// Gets a twitch id from it's associated username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cache">Whether or not to cache the user if the id isn't found. Defaults to true.</param>
        /// <returns>The twitch id.</returns>
        public string GetId(string username, bool cache = true)
        {
            var entry = UserMap.Read(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (entry == null)
            {
                if (!cacheMisses.Contains(username) && cache)
                {
                    cacheMisses.Add(username);
                }
            }
            return entry?.TwitchId;
        }

        /// <summary>
        /// Updates the twitch id cache with every username requested that was
        /// not found in the cache.
        /// </summary>
        /// <param name="token">A valid twitch OAuth token.</param>
        /// <param name="clientData">The client id the app is running under.</param>
        public async Task<CacheUpdateResult> UpdateCache(TokenResponse token, ClientData clientData)
        {
            var results = new CacheUpdateResult();
            while (cacheMisses.Count > 0)
            {
                var limit = Math.Min(cacheMisses.Count, 100);
                var removed = cacheMisses.GetRange(0, limit);
                cacheMisses.RemoveRange(0, limit);
                var response = await Users.Get(token, clientData, removed);
                if (response == null || response.Data == null)
                {
                    Logger.Warn("Null response attempting to fetch user ids while updating user cache.");
                    return results;
                }
                results.UpdatedUsers.AddRange(response.Data.Data.Where(x => x != null).Select(x => x.Login));
                results.FailedUsers.AddRange(removed.Except(results.UpdatedUsers));
                foreach (var entry in response.Data.Data)
                {
                    var existing = UserMap.Read(x => x.TwitchId.Equals(entry.Id)).FirstOrDefault();
                    if (existing != null)
                    {
                        existing.Username = entry.Login;
                        UserMap.Update(existing);
                    }
                    else
                    {
                        UserMap.Create(new Twitch.Model.User()
                        {
                            Username = entry.Login,
                            TwitchId = entry.Id
                        });
                    }
                }
            }
            UserMap.Commit();
            lastUpdate = DateTime.Now;
            return results;
        }

        /// <summary>
        /// Determines whether or not enough time has passed to update the
        /// user id cache.
        /// </summary>
        /// <param name="current">The current time, usually DateTime.Now.</param>
        /// <returns>Whether or not the user id cache should be updated.</returns>
        public bool IsUpdateTime(DateTime current)
        {
            return (current - lastUpdate).TotalSeconds >= this.UpdateTime;
        }
    }
}
