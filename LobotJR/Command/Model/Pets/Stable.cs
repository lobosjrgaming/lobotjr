using LobotJR.Command.View.Pets;
using LobotJR.Data;

namespace LobotJR.Command.Model.Pets
{
    /// <summary>
    /// The status of pets owned by a player.
    /// </summary>
    public class Stable : TableObject
    {
        /// <summary>
        /// The Twitch ID of the player this pet belongs to.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The foreign key id for the pet object.
        /// </summary>
        public int PetId { get; set; }
        /// <summary>
        /// The pet object represented by this stable entry.
        /// </summary>
        public virtual Pet Pet { get; set; }
        /// <summary>
        /// The name given to the pet by the player.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The pet's current level.
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// The pet's total experience gained.
        /// </summary>
        public int Experience { get; set; }
        /// <summary>
        /// The pet's current affection level toward the player.
        /// </summary>
        public int Affection { get; set; }
        /// <summary>
        /// How hungry the pet is.
        /// </summary>
        public int Hunger { get; set; }
        /// <summary>
        /// Whether or not this pet is sparkly.
        /// </summary>
        public bool IsSparkly { get; set; }
        /// <summary>
        /// Whether or not this pet is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return $"{Name} the level {Level} {PetView.GetPetName(this)}";
        }
    }
}
