using LobotJR.Twitch.Model;
using System;

namespace LobotJR.Command.Model.Fishing
{
    /// <summary>
    /// Represents a user's fishing data.
    /// </summary>
    public class Fisher
    {
        /// <summary>
        /// The user object for the user.
        /// </summary>
        public User User { get; set; }
        /// <summary>
        /// Whether the user has their line out to try and catch a fish.
        /// </summary>
        public bool IsFishing { get; set; }
        /// <summary>
        /// The id of the fish they have hooked, or -1 if nothing is on the line.
        /// </summary>
        public int HookedId { get; set; } = -1;
        /// <summary>
        /// The time of their last catch.
        /// </summary>
        public DateTime? CatchTime { get; set; }
        /// <summary>
        /// The time they hooked their current fish.
        /// </summary>
        public DateTime? HookedTime { get; set; }
    }
}
