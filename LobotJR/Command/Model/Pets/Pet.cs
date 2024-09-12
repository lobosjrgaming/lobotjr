using LobotJR.Data;

namespace LobotJR.Command.Model.Pets
{
    /// <summary>
    /// The data that describes a pet players can find.
    /// </summary>
    public class Pet : TableObject
    {
        /// <summary>
        /// The name of this type of pet.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The description text for the pet.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The foreign key id for the pet rarity.
        /// </summary>
        public int RarityId { get; set; }
        /// <summary>
        /// How rare this pet is.
        /// </summary>
        public virtual PetRarity Rarity { get; set; }
    }
}