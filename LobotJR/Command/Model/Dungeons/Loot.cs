using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// The loot that can be dropped in a dungeon.
    /// </summary>
    public class Loot : TableObject
    {
        /// <summary>
        /// The foreign key id for the dungeon this loot is dropped from.
        /// </summary>
        [Required]
        public int DungeonId { get; set; }
        /// <summary>
        /// The dungeon this loot is found in.
        /// </summary>
        public virtual Dungeon Dungeon { get; set; }
        /// <summary>
        /// The foreign key id for the item to be dropped.
        /// </summary>
        [Required]
        public int ItemId { get; set; }
        /// <summary>
        /// The item that is dropped.
        /// </summary>
        public virtual Item Item { get; set; }
        /// <summary>
        /// The chance this item is dropped.
        /// </summary>
        public double DropChance { get; set; }
        /// <summary>
        /// Whether the item only drops in heroic mode dungeons.
        /// </summary>
        public bool IsHeroic { get; set; }
    }
}
