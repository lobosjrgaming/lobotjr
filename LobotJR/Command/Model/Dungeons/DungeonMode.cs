using LobotJR.Data;
using SQLite.CodeFirst;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// Table that holds data for the various dungeon modes.
    /// </summary>
    public class DungeonMode : TableObject
    {
        /// <summary>
        /// The name of this mode.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The flag used to indicate this mode when referencing dungeon ids.
        /// </summary>
        [Unique]
        public string Flag { get; set; }
        /// <summary>
        /// Whether this mode is the default mode, and will be used if no flag
        /// is specified.
        /// </summary>
        public bool IsDefault { get; set; }

        public override string ToString()
        {
            return $"{Id} ({Name})";
        }
    }
}
