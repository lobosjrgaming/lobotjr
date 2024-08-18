using LobotJR.Command.Model.Player;

namespace LobotJR.Command.Model.General
{
    /// <summary>
    /// Represents a bet a player placed.
    /// </summary>
    public class Bet
    {
        /// <summary>
        /// The player that placed the bet.
        /// </summary>
        public PlayerCharacter Player { get; set; }
        /// <summary>
        /// The amount the player bet.
        /// </summary>
        public int Amount { get; set; }
        /// <summary>
        /// True if the player voted for success, false if they voted for
        /// failure.
        /// </summary>
        public bool VoteSuccess { get; set; }
    }
}
