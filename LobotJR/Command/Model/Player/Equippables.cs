using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Player
{
    /// <summary>
    /// Junction that maps character classes to the item types they can equip.
    /// </summary>
    public class Equippables : TableObject
    {
        /// <summary>
        /// The foreign key id for the item type that can be equipped.
        /// </summary>
        [Required]
        public int ItemTypeId { get; set; }
        /// <summary>
        /// The item type that can be equipped by this class.
        /// </summary>
        public virtual ItemType ItemType { get; set; }
        /// <summary>
        /// The foreign key id for the character class that can equip this item
        /// type.
        /// </summary>
        [Required]
        public int CharacterClassId { get; set; }
        /// <summary>
        /// The character class that can equip this item type.
        /// </summary>
        public virtual CharacterClass CharacterClass { get; set; }
    }
}
