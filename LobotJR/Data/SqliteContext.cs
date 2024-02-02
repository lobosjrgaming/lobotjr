using LobotJR.Command;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Fishing;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System.Data.Common;
using System.Data.Entity;

namespace LobotJR.Data
{
    /// <summary>
    /// SQLite implementation of the EF6 DbContext
    /// </summary>
    public class SqliteContext : DbContext
    {
        public DbSet<Metadata> Metadata { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<DataTimer> DataTimers { get; set; }

        /** Admin data */
        public DbSet<AccessGroup> AccessGroups { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Restriction> Restrictions { get; set; }

        /** User data */
        public DbSet<User> Users { get; set; }

        /** Fishing user data */
        public DbSet<Catch> Catches { get; set; }
        public DbSet<LeaderboardEntry> FishingLeaderboard { get; set; }
        public DbSet<TournamentResult> FishingTournaments { get; set; }

        /** Dungeon user data */
        public DbSet<LobotJR.Command.Model.Equipment.Equipment> Equipment { get; set; } // What items the player has equipped
        public DbSet<object> Inventory { get; set; }    // What items the player owns

        /** Content data */
        public DbSet<Fish> FishData { get; set; }
        public DbSet<Item> ItemData { get; set; }
        public DbSet<object> ClassData { get; set; }
        public DbSet<object> PetData { get; set; }
        public DbSet<object> DungeonData { get; set; }

        public SqliteContext() { }

        public SqliteContext(DbConnection connection) : base(connection, true) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteInitializer(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
        }

        public void Initialize()
        {
            this.Database.Initialize(false);
        }
    }
}
