using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wolfcoins;

namespace LobotJR.Command.System.Gloat
{
    /// <summary>
    /// Runs the tournament logic for the fishing system.
    /// </summary>
    public class GloatSystem : ISystem
    {
        private readonly IRepository<Catch> PersonalLeaderboard;
        private readonly Dictionary<string, int> Wolfcoins;
        public int FishingGloatCost { get; private set; }

        public GloatSystem(
            IRepositoryManager repositoryManager,
            IContentManager contentManager,
            Currency wolfcoins)
        {
            PersonalLeaderboard = repositoryManager.Catches;
            Wolfcoins = wolfcoins.coinList;
            FishingGloatCost = contentManager.GameSettings.Read().First().FishingGloatCost;
        }

        /// <summary>
        /// Checks if the user has the coins to gloat about a fishing record.
        /// </summary>
        /// <param name="user">The Twitch object for the user attempting to
        /// gloat.</param>
        /// <returns>True if the user has the coins to gloat, false if not.</returns>
        public bool CanGloatFishing(User user)
        {
            var key = Wolfcoins.Keys.FirstOrDefault(x => x.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(key) && Wolfcoins.TryGetValue(key, out var coins))
            {
                return coins >= FishingGloatCost;
            }
            return false;
        }

        /// <summary>
        /// Attempts to gloat about a specific fishing record.
        /// </summary>
        /// <param name="user">The Twitch object for the user attempting to
        /// gloat.</param>
        /// <param name="index">The id of the fish to gloat about.</param>
        /// <returns>The details of the record to gloat about.</returns>
        public Catch FishingGloat(User user, int index)
        {
            if (Wolfcoins.TryGetValue(user.Username, out var coins))
            {
                var records = PersonalLeaderboard.Read(x => x.UserId.Equals(user.TwitchId)).OrderBy(x => x.FishId);
                var key = Wolfcoins.Keys.FirstOrDefault(x => x.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && index < records.Count() && !string.IsNullOrWhiteSpace(key))
                {
                    Wolfcoins[key] = coins - FishingGloatCost;
                    return records.ElementAt(index);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the total number of fish on a user's leaderboard.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <returns>The number of leaderboard records.</returns>
        public int GetFishCount(User user)
        {
            return PersonalLeaderboard.Read(x => x.UserId.Equals(user.TwitchId)).Count();
        }

        public Task Process(bool broadcasting)
        {
            return Task.CompletedTask;
        }
    }
}
