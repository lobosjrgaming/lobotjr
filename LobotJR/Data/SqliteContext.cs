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
using Microsoft.EntityFrameworkCore;

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
        public DbSet<ClientSettings> ClientSettings { get; set; }
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
        public DbSet<DungeonHistory> DungeonHistories { get; set; }
        public DbSet<DungeonParticipant> DungeonParticipants { get; set; }


        /** Fishing user data */
        public DbSet<Catch> Catches { get; set; }
        public DbSet<LeaderboardEntry> FishingLeaderboard { get; set; }
        public DbSet<TournamentResult> FishingTournaments { get; set; }

        /** Content data */
        public DbSet<Fish> Fish { get; set; }
        public DbSet<FishRarity> FishRarities { get; set; }
        public DbSet<FishSize> FishSizes { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemType> ItemTypes { get; set; }
        public DbSet<ItemSlot> ItemSlots { get; set; }
        public DbSet<ItemQuality> ItemQualities { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<PetRarity> PetRarities { get; set; }
        public DbSet<Dungeon> Dungeons { get; set; }
        public DbSet<DungeonMode> DungeonModes { get; set; }
        public DbSet<Loot> Loot { get; set; }
        public DbSet<Encounter> Encounters { get; set; }
        public DbSet<EncounterLevel> EncounterLevels { get; set; }
        public DbSet<LevelRange> LevelRanges { get; set; }
        public DbSet<DungeonTimer> DungeonTimers { get; set; }
        public DbSet<CharacterClass> CharacterClasses { get; set; }
        public DbSet<Equippables> Equippables { get; set; }

        public SqliteContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //context.AccessGroups.Add(new AccessGroup() { Id = 1, Name = "Admin", IncludeAdmins = true });
            //context.AccessGroups.Add(new AccessGroup() { Id = 2, Name = "UIDev" });
            //context.Enrollments.Add(new Enrollment() { GroupId = 2, UserId = "26374083" });
            //context.Metadata?.Add(new Metadata());
            //context.AppSettings.Add(new AppSettings());
        }
        //protected override void OnModelCreating()
        //{
        //    //var sqliteConnectionInitializer = new SqliteInitializer(modelBuilder);
        //    //Database.SetInitializer(sqliteConnectionInitializer);
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlite("Data source=.\\data.sqlite");
            base.OnConfiguring(optionsBuilder);
        }

        public void Initialize()
        {
            //this.Database.Initialize(false);
        }
    }
}
