using LobotJR.Data;

namespace LobotJR.Command.Model.Fishing
{
    /// <summary>
    /// Holds the data used to describe a fish.
    /// </summary>
    public class Fish : TableObject
    {
        /// <summary>
        /// The foreign key id for the fish's size category.
        /// </summary>
        public int SizeCategoryId { get; set; }
        /// <summary>
        /// The size category of this fish.
        /// </summary>
        public virtual FishSize SizeCategory { get; set; }
        /// <summary>
        /// The foreign key id for the fish's rarity.
        /// </summary>
        public int RarityId { get; set; }
        /// <summary>
        /// Rarity category of the fish.
        /// </summary>
        public virtual FishRarity Rarity { get; set; }
        /// <summary>
        /// The display name of the fish.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The minimum length of this fish.
        /// </summary>
        public float MinimumLength { get; set; }
        /// <summary>
        /// The maximum length of this fish.
        /// </summary>
        public float MaximumLength { get; set; }
        /// <summary>
        /// The minimum weight of this fish.
        /// </summary>
        public float MinimumWeight { get; set; }
        /// <summary>
        /// The maximum weight of this fish.
        /// </summary>
        public float MaximumWeight { get; set; }
        /// <summary>
        /// The flavor text given to describe the fish.
        /// </summary>
        public string FlavorText { get; set; }
    }

    public class FishRarity : TableObject
    {
        /// <summary>
        /// The name that represents this level of rarity.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The relative weight of this rarity.
        /// </summary>
        public float Weight { get; set; }

        public override string ToString()
        {
            return $"{Id} ({Name})";
        }
    }

    public class FishSize : TableObject
    {
        /// <summary>
        /// The name that represents this size category.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Message given to user when a fish of this size is hooked.
        /// </summary>
        public string Message { get; set; }

        public override string ToString()
        {
            return $"{Id} ({Name})";
        }
    }
}
