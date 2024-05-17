using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Experience;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.Pets;

namespace LobotJR.Data
{
    /// <summary>
    /// Collection of repositories of game content data for data access.
    /// </summary>
    public interface IContentManager
    {
        IRepository<Fish> FishData { get; }
        IRepository<Item> ItemData { get; }
        IRepository<ItemType> ItemTypeData { get; }
        IRepository<ItemSlot> ItemSlotData { get; }
        IRepository<ItemQuality> ItemQualityData { get; }
        IRepository<Pet> PetData { get; }
        IRepository<PetRarity> PetRarityData { get; }
        IRepository<Dungeon> DungeonData { get; }
        IRepository<Loot> LootData { get; }
        IRepository<Encounter> EncounterData { get; }
        IRepository<DungeonTimer> DungeonTimerData { get; }
        IRepository<CharacterClass> CharacterClassData { get; }
    }
}
