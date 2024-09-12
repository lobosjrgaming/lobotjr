using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Controller.Fishing
{
    /// <summary>
    /// Runs the leaderboard logic for the fishing controller.
    /// </summary>
    public class LeaderboardController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IConnectionManager ConnectionManager;

        /// <summary>
        /// Event handler for events related to the leaderboard.
        /// </summary>
        /// <param name="catchData">The catch data the leaderboard was updated with.</param>
        public delegate void LeaderboardEventHandler(LeaderboardEntry catchData);

        /// <summary>
        /// Event fired when a user's catch sets a new record.
        /// </summary>
        public event LeaderboardEventHandler NewGlobalRecord;

        public LeaderboardController(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        /// <summary>
        /// Gets the global leaderboard records.
        /// </summary>
        /// <returns>A collection of catch data containing the largest catch of
        /// each fish.</returns>
        public IEnumerable<LeaderboardEntry> GetLeaderboard()
        {
            return ConnectionManager.CurrentConnection.FishingLeaderboard.Read();
        }

        /// <summary>
        /// Gets the personal leaderboard for a user.
        /// </summary>
        /// <param name="user">The user object.</param>
        /// <returns>A collection of records for the user.</returns>
        public IEnumerable<Catch> GetPersonalLeaderboard(User user)
        {
            return ConnectionManager.CurrentConnection.Catches.Read(x => x.UserId.Equals(user.TwitchId)).OrderBy(x => x.FishId);
        }

        /// <summary>
        /// Gets the personal leaderboard for a user.
        /// </summary>
        /// <param name="user">The user object.</param>
        /// <returns>A collection of records for the user.</returns>
        public Catch GetUserRecordForFish(User user, Fish fish)
        {
            return ConnectionManager.CurrentConnection.Catches.Read(x => x.UserId.Equals(user.TwitchId) && x.Fish.Equals(fish)).FirstOrDefault();
        }

        /// <summary>
        /// Updates the personal leaderboard with new data if the catch object
        /// would set a new record.
        /// </summary>
        /// <param name="user">The user object of the user catching the
        /// fish.</param>
        /// <param name="catchData">An object with catch data to use for the
        /// update.</param>
        /// <returns>Whether or not the leaderboard was updated.</returns>
        public bool UpdatePersonalLeaderboard(User user, Catch catchData)
        {
            if (user == null || catchData == null)
            {
                return false;
            }

            var record = ConnectionManager.CurrentConnection.Catches.Read(x => x.UserId.Equals(user.TwitchId) && x.Fish.Equals(catchData.Fish)).FirstOrDefault();
            if (record == null || record.Weight < catchData.Weight)
            {
                Logger.Debug("Catch set a new personal record for user {user}, fish {fish} at {weight} pounds.", user.Username, catchData.Fish?.Name, catchData.Weight);
                if (record == null)
                {
                    ConnectionManager.CurrentConnection.Catches.Create(catchData);
                }
                else
                {
                    record.CopyFrom(catchData);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes a fish from a user's records.
        /// </summary>
        /// <param name="user">The user object of the user to modify.</param>
        /// <param name="index">The index of the fish to remove.</param>
        public void DeleteFish(User user, int index)
        {
            if (user != null)
            {
                var records = ConnectionManager.CurrentConnection.Catches.Read(x => x.UserId.Equals(user.TwitchId)).OrderBy(x => x.FishId);
                if (index >= 0 && records.Count() > index)
                {
                    var record = records.ElementAt(index);
                    Logger.Debug("Removed fish {fish} at index {index} for user {user}", record?.Fish?.Name, index, user.Username);
                    ConnectionManager.CurrentConnection.Catches.Delete(record);
                }
            }
        }

        /// <summary>
        /// Updates the global leaderboard with new data if the catch object
        /// would set a new record.
        /// </summary>
        /// <param name="catchData">An object with catch data to use for the
        /// update.</param>
        /// <returns>Whether or not the leaderboard was updated.</returns>
        public bool UpdateGlobalLeaderboard(Catch catchData)
        {
            if (catchData == null)
            {
                return false;
            }

            var entry = new LeaderboardEntry()
            {
                Fish = catchData.Fish,
                Length = catchData.Length,
                Weight = catchData.Weight,
                UserId = catchData.UserId
            };
            var record = ConnectionManager.CurrentConnection.FishingLeaderboard.Read(x => x.Fish.Equals(catchData.Fish)).FirstOrDefault();
            if (record == null || record.Weight < catchData.Weight)
            {
                Logger.Debug("Catch set a new global record for fish {fish} at {weight} pounds.", catchData.Fish?.Name, catchData.Weight);
                if (record == null)
                {
                    ConnectionManager.CurrentConnection.FishingLeaderboard.Create(entry);
                }
                else
                {
                    record.CopyFrom(entry);
                }
                NewGlobalRecord?.Invoke(entry);
                return true;
            }
            return false;
        }
    }
}
