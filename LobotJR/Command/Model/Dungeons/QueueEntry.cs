using LobotJR.Command.Model.Player;
using System;
using System.Collections.Generic;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// A registration in the group finder queue for a player with the dungeons
    /// they want to queue for.
    /// </summary>
    public class QueueEntry
    {
        /// <summary>
        /// The player that this queue entry is for.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The time this player entered the queue.
        /// </summary>
        public DateTime QueueTime { get; set; } = DateTime.Now;
        /// <summary>
        /// The collection of dungeons the player is queued for.
        /// </summary>
        public IEnumerable<DungeonRun> Dungeons { get; set; }

        public QueueEntry(PlayerCharacter player, IEnumerable<DungeonRun> dungeons)
        {
            UserId = player.UserId;
            Dungeons = dungeons;
        }
    }
}
