using LobotJR.Data;

namespace LobotJR.Command.Model.Equipment
{
    /// <summary>
    /// Lookup table for the slot an item is equipped in.
    /// </summary>
    public class ItemSlot : TableObject
    {
        /// <summary>
        /// The name of the slot an item can be equipped to.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// How many of items a player can equip in this slot.
        /// </summary>
        public int MaxEquipped { get; set; } = 1;

        public override string ToString()
        {
            return $"{Id} ({Name})";
        }
    }
}
