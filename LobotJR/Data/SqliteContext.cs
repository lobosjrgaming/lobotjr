using LobotJR.Command;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.General;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
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
        public DbSet<GameSettings> GameSettings { get; set; }
        public DbSet<BugReport> BugReports { get; set; }
        public DbSet<DataTimer> DataTimers { get; set; }

        /** Admin data */
        public DbSet<AccessGroup> AccessGroups { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Restriction> Restrictions { get; set; }

        /** User data */
        public DbSet<User> Users { get; set; }
        public DbSet<PlayerCharacter> PlayerCharacters { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Stable> Stables { get; set; }
        public DbSet<DungeonLockout> DungeonLockouts { get; set; }


        /** Fishing user data */
        public DbSet<Catch> Catches { get; set; }
        public DbSet<LeaderboardEntry> FishingLeaderboard { get; set; }
        public DbSet<TournamentResult> FishingTournaments { get; set; }

        /** Content data */
        public DbSet<Fish> FishData { get; set; }
        public DbSet<Item> ItemData { get; set; }
        public DbSet<ItemType> ItemTypeData { get; set; }
        public DbSet<ItemSlot> ItemSlotData { get; set; }
        public DbSet<ItemQuality> ItemQualityData { get; set; }
        public DbSet<Pet> PetData { get; set; }
        public DbSet<PetRarity> PetRarityData { get; set; }
        public DbSet<Dungeon> DungeonData { get; set; }
        public DbSet<Loot> LootData { get; set; }
        public DbSet<Encounter> EncounterData { get; set; }
        public DbSet<DungeonTimer> DungeonTimerData { get; set; }
        public DbSet<CharacterClass> ClassData { get; set; }
        public DbSet<Equippables> EquippableData { get; set; }

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
