using LobotJR.Data;

namespace LobotJR.Command.Model.Equipment
{
    /// <summary>
    /// Lookup table for item quality.
    /// </summary>
    public class ItemQuality : TableObject
    {
        /// <summary>
        /// The name of the item quality.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The base drop rate for items of this quality.
        /// </summary>
        public int DropRatePenalty { get; set; }
    }
}
