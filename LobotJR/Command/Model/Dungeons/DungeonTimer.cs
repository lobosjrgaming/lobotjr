using LobotJR.Data;
using System;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// Timers that represent periodic dungeon events.
    /// </summary>
    public class DungeonTimer : TableObject
    {
        /// <summary>
        /// The name of the dungeon event.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The baseline time when this timer resets. Timers are cleared each
        /// multiple of Length after BaseTime. If BaseTime is not set, the
        /// timer is cleared once Length has elapsed from the time the lockout
        /// is set.
        /// </summary>
        public DateTime? BaseTime { get; set; }
        /// <summary>
        /// How long the timer lasts, in minutes.
        /// </summary>
        public int Length { get; set; }
    }
}
