using LobotJR.Data;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// Table that holds data for level ranges in each dungeon mode
    /// </summary>
    public class LevelRange : TableObject
    {
        /// <summary>
        /// The foreign key id for the dungeon this level range represents.
        /// </summary>
        [Required]
        public int DungeonId { get; set; }
        /// <summary>
        /// The dungeon this level range is for.
        /// </summary>
        public virtual Dungeon Dungeon { get; set; }
        /// <summary>
        /// The minimum level a player can attempt the dungeon for this mode.
        /// </summary>
        public int Minimum { get; set; }
        /// <summary>
        /// The maximum level a player can attempt the dungeon for this mode.
        /// </summary>
        public int Maximum { get; set; }
        /// <summary>
        /// The foreign key id for the dungeon mode this level range represents.
        /// </summary>
        public int ModeId { get; set; }
        /// <summary>
        /// The dungeon mode this level range represents.
        /// </summary>
        public virtual DungeonMode Mode { get; set; }
    }
}
