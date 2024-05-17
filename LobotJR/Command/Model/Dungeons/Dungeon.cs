using LobotJR.Data;
using System.Collections.Generic;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// A dungeon that groups of players can attempt to complete.
    /// </summary>
    public class Dungeon : TableObject
    {
        /// <summary>
        /// The name of the dungeon as shown in the dungeon list.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// A short description of the dungeon.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The first line of text sent to players upon starting this dungeon.
        /// </summary>
        public string Introduction { get; set; }
        /// <summary>
        /// The text sent to players when an encounter is failed.
        /// </summary>
        public string FailureText { get; set; }
        /// <summary>
        /// The minimum level a player can attempt the dungeon.
        /// </summary>
        public int LevelMinimum { get; set; }
        /// <summary>
        /// The maximum level a player can attempt the dungeon.
        /// </summary>
        public int LevelMaximum { get; set; }
        /// <summary>
        /// The minimum level a player can attempt the heroic version of this
        /// dungeon.
        /// </summary>
        public int HeroicMinimum { get; set; }
        /// <summary>
        /// The maximum level a player can attempt the heroic version of this
        /// dungeon.
        /// </summary>
        public int HeroicMaximum { get; set; }
        /// <summary>
        /// The collection of loot drops in this dungeon.
        /// </summary>
        public virtual List<Loot> Loot { get; set; }
        /// <summary>
        /// The collection of encounters in this dungeon.
        /// </summary>
        public virtual List<Encounter> Encounters { get; set; }
    }
}
