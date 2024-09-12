using LobotJR.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// History record of a dungeon run completion.
    /// </summary>
    public class DungeonHistory : TableObject
    {
        /// <summary>
        /// The timestamp for when the run was completed.
        /// </summary>
        [Required]
        public DateTime Date { get; set; } = DateTime.Now;
        /// <summary>
        /// True if this dungeon group was formed using the Group Finder.
        /// </summary>
        public bool IsQueueGroup { get; set; } = false;
        /// <summary>
        /// Foreign key id of the dungeon that was run.
        /// </summary>
        [Required]
        public int DungeonId { get; set; }
        /// <summary>
        /// The dungeon that was run.
        /// </summary>
        public virtual Dungeon Dungeon { get; set; }
        /// <summary>
        /// Foreign key id of the mode the dungeon was run in.
        /// </summary>
        [Required]
        public int ModeId { get; set; }
        /// <summary>
        /// The mode the dungeon was run in.
        /// </summary>
        public virtual DungeonMode Mode { get; set; }
        /// <summary>
        /// The number of encounters that the party completed.
        /// </summary>
        public int StepsComplete { get; set; } = 0;
        /// <summary>
        /// True if the dungeon was completed successfully.
        /// </summary>
        public bool Success { get; set; } = false;
        /// <summary>
        /// Collection of participant records for each party member.
        /// </summary>
        public virtual List<DungeonParticipant> Participants { get; set; } = new List<DungeonParticipant>();
    }
}
