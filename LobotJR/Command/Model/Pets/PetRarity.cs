using LobotJR.Data;

namespace LobotJR.Command.Model.Pets
{
    /// <summary>
    /// Lookup table for the pet rarity.
    /// </summary>
    public class PetRarity : TableObject
    {
        /// <summary>
        /// Name for this level of rarity.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The base droprate for pets with this rarity.
        /// </summary>
        public float DropRate { get; set; }
    }
}
