using LobotJR.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Equipment
{

    /// <summary>
    /// Table that holds player inventories.
    /// </summary>
    public class Inventory : TableObject
    {
        /// <summary>
        /// The Twitch ID of the user who owns this item.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The foreign key id for this item.
        /// </summary>
        [Required]
        public int ItemId { get; set; }
        /// <summary>
        /// An item.
        /// </summary>
        public virtual Item Item { get; set; }
        /// <summary>
        /// The quantity of this item the user has.
        /// </summary>
        public int Count { get; set; } = 1;
        /// <summary>
        /// Whether or not this item is currently equipped.
        /// </summary>
        public bool IsEquipped { get; set; }
        /// <summary>
        /// The datetime for when the inventory record was created.
        /// </summary>
        public DateTime TimeAdded { get; set; }
    }
}
