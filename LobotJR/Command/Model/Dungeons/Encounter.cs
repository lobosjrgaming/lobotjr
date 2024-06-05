﻿using LobotJR.Data;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// A single encounter in a dungeon.
    /// </summary>
    public class Encounter : TableObject
    {
        /// <summary>
        /// The foreign key id for the dungeon this encounter is in.
        /// </summary>
        [Required]
        public int DungeonId { get; set; }
        /// <summary>
        /// The dungeon this encounter is found in.
        /// </summary>
        public virtual Dungeon Dungeon { get; set; }
        /// <summary>
        /// The name of the enemy encountered.
        /// </summary>
        public string Enemy { get; set; }
        /// <summary>
        /// The difficulty rating of this encounter, used to determine success
        /// chance.
        /// </summary>
        public int Difficulty { get; set; }
        /// <summary>
        /// The difficulty rating of this encounter when running the dungeon in
        /// heroic mode, used to determine success chance.
        /// </summary>
        public int HeroicDifficulty { get; set; }
        /// <summary>
        /// The text sent to players before this encounter is resolved.
        /// </summary>
        public string SetupText { get; set; }
        /// <summary>
        /// The text sent to players after this encounter is successfully
        /// cleared.
        /// </summary>
        public string CompleteText { get; set; }
    }
}
