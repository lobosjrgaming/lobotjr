using LobotJR.Command;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System.Data.Entity;

namespace LobotJR.Data
{
    /// <summary>
    /// Implementation of a repository manager using sqlite storage.
    /// </summary>
    public class SqliteRepositoryManager : IDatabase
    {
        private DbContext context;

        public IRepository<Metadata> Metadata { get; private set; }
        public IRepository<AppSettings> AppSettings { get; private set; }
        public IRepository<GameSettings> GameSettings { get; private set; }
        public IRepository<DataTimer> DataTimers { get; private set; }
        public IRepository<User> Users { get; private set; }
        public IRepository<AccessGroup> AccessGroups { get; private set; }
        public IRepository<Enrollment> Enrollments { get; private set; }
        public IRepository<Restriction> Restrictions { get; private set; }
        public IRepository<Catch> Catches { get; private set; }
        public IRepository<LeaderboardEntry> FishingLeaderboard { get; private set; }
        public IRepository<TournamentResult> TournamentResults { get; private set; }
        public IRepository<TournamentEntry> TournamentEntries { get; private set; }
        public IRepository<PlayerCharacter> PlayerCharacters { get; private set; }
        public IRepository<Inventory> Inventories { get; private set; }
        public IRepository<Stable> Stables { get; private set; }
        public IRepository<DungeonLockout> DungeonLockouts { get; private set; }

        public IRepository<Fish> FishData { get; private set; }
        public IRepository<Item> ItemData { get; private set; }
        public IRepository<ItemType> ItemTypeData { get; private set; }
        public IRepository<ItemSlot> ItemSlotData { get; private set; }
        public IRepository<ItemQuality> ItemQualityData { get; private set; }
        public IRepository<Pet> PetData { get; private set; }
        public IRepository<PetRarity> PetRarityData { get; private set; }
        public IRepository<Dungeon> DungeonData { get; private set; }
        public IRepository<DungeonMode> DungeonModeData { get; private set; }
        public IRepository<LevelRange> LevelRangeData { get; private set; }
        public IRepository<Loot> LootData { get; private set; }
        public IRepository<Encounter> EncounterData { get; private set; }
        public IRepository<EncounterLevel> EncounterLevelData { get; private set; }
        public IRepository<DungeonTimer> DungeonTimerData { get; private set; }
        public IRepository<CharacterClass> CharacterClassData { get; private set; }

        public SqliteRepositoryManager(DbContext context)
        {
            SetContext(context);
        }

        public SqliteRepositoryManager()
        {
            SetContext(new SqliteContext());
        }

        private void SetContext(DbContext context)
        {
            this.context = context;
            Metadata = new SqliteRepository<Metadata>(context);
            AppSettings = new SqliteRepository<AppSettings>(context);
            GameSettings = new SqliteRepository<GameSettings>(context);
            DataTimers = new SqliteRepository<DataTimer>(context);
            Users = new SqliteRepository<User>(context);
            AccessGroups = new SqliteRepository<AccessGroup>(context);
            Enrollments = new SqliteRepository<Enrollment>(context);
            Restrictions = new SqliteRepository<Restriction>(context);
            Catches = new SqliteRepository<Catch>(context);
            FishingLeaderboard = new SqliteRepository<LeaderboardEntry>(context);
            TournamentResults = new SqliteRepository<TournamentResult>(context);
            TournamentEntries = new SqliteRepository<TournamentEntry>(context);
            PlayerCharacters = new SqliteRepository<PlayerCharacter>(context);
            Inventories = new SqliteRepository<Inventory>(context);
            Stables = new SqliteRepository<Stable>(context);
            DungeonLockouts = new SqliteRepository<DungeonLockout>(context);

            FishData = new SqliteRepository<Fish>(context);
            ItemData = new SqliteRepository<Item>(context);
            ItemTypeData = new SqliteRepository<ItemType>(context);
            ItemSlotData = new SqliteRepository<ItemSlot>(context);
            ItemQualityData = new SqliteRepository<ItemQuality>(context);
            PetData = new SqliteRepository<Pet>(context);
            PetRarityData = new SqliteRepository<PetRarity>(context);
            DungeonData = new SqliteRepository<Dungeon>(context);
            DungeonModeData = new SqliteRepository<DungeonMode>(context);
            LevelRangeData = new SqliteRepository<LevelRange>(context);
            LootData = new SqliteRepository<Loot>(context);
            EncounterData = new SqliteRepository<Encounter>(context);
            EncounterLevelData = new SqliteRepository<EncounterLevel>(context);
            DungeonTimerData = new SqliteRepository<DungeonTimer>(context);
            CharacterClassData = new SqliteRepository<CharacterClass>(context);
        }

        public void Dispose()
        {
            context.SaveChanges();
            context.Database.Connection.Close();
            context.Dispose();
        }
    }
}
