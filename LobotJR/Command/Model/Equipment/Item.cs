using LobotJR.Command.Model.Player;
using LobotJR.Data;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Equipment
{
    /// <summary>
    /// An item players can have. Includes equipment, keys, consumables, etc.
    /// </summary>
    public class Item : TableObject, IPlayerStats
    {
        /// <summary>
        /// The foreign key id for the item quality.
        /// </summary>
        [Required]
        public int QualityId { get; set; }
        /// <summary>
        /// The quality/rarity of the item.
        /// </summary>
        public virtual ItemQuality Quality { get; set; }

        /// <summary>
        /// The foreign key id for the item slot.
        /// </summary>
        [Required]
        public int SlotId { get; set; }
        /// <summary>
        /// The equipment slot the item goes in.
        /// </summary>
        public virtual ItemSlot Slot { get; set; }

        /// <summary>
        /// The foreign key id for the item type.
        /// </summary>
        [Required]
        public int TypeId { get; set; }
        /// <summary>
        /// The gear type of the item, which determines which classes can
        /// equip it.
        /// </summary>
        public virtual ItemType Type { get; set; }
        /// <summary>
        /// The name of the item.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The flavor text description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The maximum number of this item a player can have.
        /// </summary>
        public int Max { get; set; }
        /// <summary>
        /// The chance of successfully completing a dungeon.
        /// </summary>
        public float SuccessChance { get; set; }
        /// <summary>
        /// The chance for items to drop.
        /// </summary>
        public float ItemFind { get; set; }
        /// <summary>
        /// The amount of coins earned.
        /// </summary>
        public float CoinBonus { get; set; }
        /// <summary>
        /// The experience earned.
        /// </summary>
        public float XpBonus { get; set; }
        /// <summary>
        /// The chance for a player to avoid death after failing a dungeon.
        /// </summary>
        public float PreventDeathBonus { get; set; }
    }
}
