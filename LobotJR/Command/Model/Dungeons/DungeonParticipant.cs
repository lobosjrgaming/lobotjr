using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Data;
using System.ComponentModel.DataAnnotations;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// Record of a user participating in a dungeon run.
    /// </summary>
    public class DungeonParticipant : TableObject
    {
        /// <summary>
        /// Foreign key id for the history record this participant is in.
        /// </summary>
        public int HistoryId { get; set; }
        /// <summary>
        /// The dungeon history record this participant is in.
        /// </summary>
        public virtual DungeonHistory History { get; set; }
        /// <summary>
        /// The id of the user this entry is for.
        /// </summary>
        [Required]
        public string UserId { get; set; }
        /// <summary>
        /// The amount of time (in seconds) the user waited in the group finder
        /// queue. If the dungeon history record is not flagged as being a
        /// queue group, this will be 0.
        /// </summary>
        public int WaitTime { get; set; }
        /// <summary>
        /// The amount of experience the player earned. If the dungeon was a
        /// success, this will be a positive number. If the dungeon was a
        /// failure and the player died, this will be a negative number. If the
        /// dungeon was a failure but the player did not die, this will be
        /// zero.
        /// </summary>
        public int ExperienceEarned { get; set; }
        /// <summary>
        /// The amount of currency the player earned. If the dungeon was a
        /// success, this will be a positive number. If the dungeon was a
        /// failure and the player died, this will be a negative number. If the
        /// dungeon was a failure but the player did not die, this will be
        /// zero.
        /// </summary>
        public int CurrencyEarned { get; set; }
        /// <summary>
        /// Foreign key id for the item drop the player earned, if any.
        /// </summary>
        public int ItemDropId { get; set; }
        /// <summary>
        /// The item drop the player earned from this dungeon, if any.
        /// </summary>
        public virtual Item ItemDrop { get; set; }
        /// <summary>
        /// Foreign key id for the pet the player found, if any.
        /// </summary>
        public int PetDropId { get; set; }
        /// <summary>
        /// The pet the player found in this dungeon, if any.
        /// </summary>
        public virtual Pet PetDrop { get; set; }
    }
}
