using LobotJR.Data;

namespace LobotJR.Command.Model.Equipment
{
    /// <summary>
    /// Lookup table for the type of an item.
    /// </summary>
    public class ItemType : TableObject
    {
        /// <summary>
        /// The name for the type or category of item.
        /// </summary>
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Id} ({Name})";
        }
    }
}
