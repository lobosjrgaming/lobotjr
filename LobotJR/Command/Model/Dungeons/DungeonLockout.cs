using LobotJR.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// Entry created when a player completes a DungeonTimer event.
    /// </summary>
    public class DungeonLockout : TableObject
    {
        /// <summary>
        /// The Twitch ID of the player.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The foreign key id for the associated timer.
        /// </summary>
        [Required]
        public int TimerId { get; set; }
        /// <summary>
        /// The DungeonTimer event that was completed.
        /// </summary>
        public virtual DungeonTimer Timer { get; set; }
        /// <summary>
        /// The time the event was completed.
        /// </summary>
        public DateTime Time { get; set; }
    }
}
