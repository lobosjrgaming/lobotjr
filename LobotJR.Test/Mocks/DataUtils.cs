using LobotJR.Data;
using LobotJR.Twitch.Model;

namespace LobotJR.Test.Mocks
{
    public static class DataUtils
    {

        /// <summary>
        /// Clears personal leaderboard records for a specific user.
        /// </summary>
        /// <param name="manager">The data manager to manipulate.</param>
        /// <param name="user">The Twitch object of the user to clear.</param>
        public static void ClearFisherRecords(SqliteRepositoryManager manager, User user)
        {
            var records = manager.Catches.Read(x => x.UserId.Equals(user.TwitchId));
            foreach (var record in records)
            {
                manager.Catches.Delete(record);
            }
            manager.Catches.Commit();
        }
    }
}
