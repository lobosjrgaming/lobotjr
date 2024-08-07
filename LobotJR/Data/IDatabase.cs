using LobotJR.Command;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System;

namespace LobotJR.Data
{
    /// <summary>
    /// Interface for the available tables in the database.
    /// </summary>
    public interface IDatabase : IDisposable
    {
        // Application Data
        IRepository<Metadata> Metadata { get; }
        IRepository<AppSettings> AppSettings { get; }
        IRepository<DataTimer> DataTimers { get; }
        IRepository<User> Users { get; }
        IRepository<AccessGroup> AccessGroups { get; }
        IRepository<Enrollment> Enrollments { get; }
        IRepository<Restriction> Restrictions { get; }
        IRepository<Catch> Catches { get; }
        IRepository<LeaderboardEntry> FishingLeaderboard { get; }
        IRepository<TournamentResult> TournamentResults { get; }
        IRepository<TournamentEntry> TournamentEntries { get; }
        IRepository<PlayerCharacter> PlayerCharacters { get; }
        IRepository<Inventory> Inventories { get; }
        IRepository<Stable> Stables { get; }
        IRepository<DungeonLockout> DungeonLockouts { get; }

        // Game Content Data
        IRepository<GameSettings> GameSettings { get; }
        IRepository<Fish> FishData { get; }
        IRepository<Item> ItemData { get; }
        IRepository<ItemType> ItemTypeData { get; }
        IRepository<ItemSlot> ItemSlotData { get; }
        IRepository<ItemQuality> ItemQualityData { get; }
        IRepository<Pet> PetData { get; }
        IRepository<PetRarity> PetRarityData { get; }
        IRepository<DungeonMode> DungeonModeData { get; }
        IRepository<Dungeon> DungeonData { get; }
        IRepository<LevelRange> LevelRangeData { get; }
        IRepository<Loot> LootData { get; }
        IRepository<Encounter> EncounterData { get; }
        IRepository<EncounterLevel> EncounterLevelData { get; }
        IRepository<DungeonTimer> DungeonTimerData { get; }
        IRepository<CharacterClass> CharacterClassData { get; }
    }
}
