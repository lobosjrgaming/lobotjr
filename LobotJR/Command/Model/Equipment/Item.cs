using LobotJR.Data;

namespace LobotJR.Command.Model.Equipment
{
    public class ItemQuality : TableObject
    {
        public string Name { get; set; }
        public int DropRate { get; set; }
    }

    /// <summary>
    /// Lookup table for the 
    /// </summary>
    public class ItemSlot : TableObject
    {
        public string Name { get; set; }
    }

    public class ItemType : TableObject
    {
        public string Name { get; set; }
    }

    public class Stats : TableObject
    {
        /// <summary>
        /// The chance of successfully completing a dungeon.
        /// </summary>
        float SuccessChance { get; set; }
        /// <summary>
        /// The chance for items to drop.
        /// </summary>
        int ItemFind { get; set; }
        /// <summary>
        /// The amount of coins earned.
        /// </summary>
        int CoinBonus { get; set; }
        /// <summary>
        /// The experience earned.
        /// </summary>
        int XpBonus { get; set; }
        /// <summary>
        /// The chance for a player to avoid death after failing a dungeon.
        /// </summary>
        float PreventDeathBonus { get; set; }
    }

    public class Item : Stats
    {
        /// <summary>
        /// The quality/rarity of the item.
        /// </summary>
        public ItemQuality Quality { get; set; }
        /// <summary>
        /// The equipment slot the item goes in.
        /// </summary>
        public ItemSlot Slot { get; set; }
        /// <summary>
        /// The gear type of the item, which determines which classes can equip.
        /// </summary>
        public ItemType Type { get; set; }
        /// <summary>
        /// The name of the item.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The flavor text description.
        /// </summary>
    }

    public class Inventory : TableObject
    {
        public string UserId { get; set; }
        public virtual Item Item { get; set; }
    }

    public class Equipment : TableObject
    {
        public string UserId { get; set; }
        public virtual Item Item { get; set; }
    }
}
