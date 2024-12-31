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
        /// <summary>
        /// An HTML color string used to represent this pet rarity on the
        /// controller page.
        /// </summary>
        public string Color { get; set; }

        public override string ToString()
        {
            return $"{Id} ({Name})";
        }
    }
}
