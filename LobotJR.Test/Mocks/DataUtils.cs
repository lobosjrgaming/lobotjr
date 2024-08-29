using LobotJR.Data;
using LobotJR.Twitch.Model;

namespace LobotJR.Test.Mocks
{
    public static class DataUtils
    {

        /// <summary>
        /// Clears personal leaderboard records for a specific user.
        /// </summary>
        /// <param name="database">The database to manipulate.</param>
        /// <param name="user">The Twitch object of the user to clear.</param>
        public static void ClearFisherRecords(IDatabase database, User user)
        {
            var records = database.Catches.Read(x => x.UserId.Equals(user.TwitchId));
            foreach (var record in records)
            {
                database.Catches.Delete(record);
            }
            database.Catches.Commit();
        }
    }
}
