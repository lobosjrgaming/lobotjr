using LobotJR.Data;

namespace LobotJR.Command.Model.Experience
{
    /// <summary>
    /// Class that holds the data for a player character.
    /// </summary>
    public class PlayerCharacter : TableObject
    {
        /// <summary>
        /// The Twitch ID of the player.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The foreign key id for the player's class.
        /// </summary>
        public int CharacterClassId { get; set; }
        /// <summary>
        /// The character class this player has selected.
        /// </summary>
        public virtual CharacterClass CharacterClass { get; set; }
        /// <summary>
        /// The total experience the player has earned.
        /// </summary>
        public int Experience { get; set; }
        /// <summary>
        /// The current wolfcoin count for the player.
        /// </summary>
        public int Currency { get; set; }
        /// <summary>
        /// The player's current level.
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// The number of times the player has prestiged.
        /// </summary>
        public int Prestige { get; set; }
    }
}
