using LobotJR.Command;
using LobotJR.Command.Model.Fishing;
using LobotJR.Twitch;
using System;
using System.Data.Entity;

namespace LobotJR.Data
{
    /// <summary>
    /// Implementation of a repository manager using sqlite storage.
    /// </summary>
    public class SqliteRepositoryManager : IRepositoryManager, IContentManager, IDisposable
    {
        private DbContext context;

        public IRepository<Metadata> Metadata { get; private set; }
        public IRepository<AppSettings> AppSettings { get; private set; }
        public IRepository<DataTimer> DataTimers { get; private set; }
        public IRepository<Twitch.Model.User> Users { get; private set; }
        public IRepository<AccessGroup> AccessGroups { get; private set; }
        public IRepository<Enrollment> Enrollments { get; private set; }
        public IRepository<Restriction> Restrictions { get; private set; }
        public IRepository<Catch> Catches { get; private set; }
        public IRepository<LeaderboardEntry> FishingLeaderboard { get; private set; }
        public IRepository<TournamentResult> TournamentResults { get; private set; }
        public IRepository<TournamentEntry> TournamentEntries { get; private set; }
        public IRepository<Fish> FishData { get; private set; }

        public SqliteRepositoryManager(DbContext context)
        {
            this.context = context;
            Metadata = new SqliteRepository<Metadata>(context);
            AppSettings = new SqliteRepository<AppSettings>(context);
            DataTimers = new SqliteRepository<DataTimer>(context);
            Users = new SqliteRepository<Twitch.Model.User>(context);
            AccessGroups = new SqliteRepository<AccessGroup>(context);
            Enrollments = new SqliteRepository<Enrollment>(context);
            Restrictions = new SqliteRepository<Restriction>(context);
            Catches = new SqliteRepository<Catch>(context);
            FishingLeaderboard = new SqliteRepository<LeaderboardEntry>(context);
            TournamentResults = new SqliteRepository<TournamentResult>(context);
            TournamentEntries = new SqliteRepository<TournamentEntry>(context);

            FishData = new SqliteRepository<Fish>(context);
        }

        public void Dispose()
        {
            context.Database.Connection.Close();
            context.Dispose();
        }
    }
}
