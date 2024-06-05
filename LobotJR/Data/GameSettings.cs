namespace LobotJR.Data
{
    /// <summary>
    /// Settings that modify the behavior of the game engine.
    /// </summary>
    public class GameSettings : TableObject
    {
        /// <summary>
        /// The frequency, in minutes, that experience is awarded to viewers.
        /// </summary>
        public int ExperienceFrequency { get; set; } = 15;
        /// <summary>
        /// The amount of experience granted when experience is awarded.
        /// </summary>
        public int ExperienceValue { get; set; } = 1;
        /// <summary>
        /// The amount of wolfcoins granted when experience is awarded.
        /// </summary>
        public int CoinValue { get; set; } = 3;
        /// <summary>
        /// The multiplier to experience and coin gain for being a subscriber.
        /// </summary>
        public int SubRewardMultiplier { get; set; } = 2;
        /// <summary>
        /// The base cost, per level, to change a player's class.
        /// </summary>
        public int RespecCost { get; set; } = 250;
        /// <summary>
        /// The cost to fetch info about another user.
        /// </summary>
        public int PryCost { get; set; } = 1;
        public int LevelGloatCost { get; set; } = 10;
        public int PetExperienceToLevel { get; set; } = 150;
        public int PetLevelMax { get; set; } = 10;
        public int PetFeedingAffection { get; set; } = 5;
        public int PetFeedingCost { get; set; } = 5;
        public int PetHungerMax { get; set; } = 100;

        /// <summary>
        /// The shortest time, in seconds, it can take to hook a fish. Default
        /// is 60 seconds.
        /// </summary>
        public int FishingCastMinimum { get; set; } = 60;
        /// <summary>
        /// The longest time, in seconds, it can take to hook a fish. Default
        /// is 600 seconds.
        /// </summary>        
        public int FishingCastMaximum { get; set; } = 600;
        /// <summary>
        /// How long, in seconds, a fish remains on the hook before it gets
        /// away. Default is 30 seconds.
        /// </summary>
        public int FishingHookLength { get; set; } = 30;
        /// <summary>
        /// Determines whether to use the weights associated with each fish
        /// rarity, or a standard normal distribution.
        /// </summary>
        public bool FishingUseNormalRarity { get; set; } = false;
        /// <summary>
        /// Determines whether to use distribute the fish weight and length
        /// using a normal distribution, or to use a stepped distribution of
        /// five size bands.
        /// </summary>
        public bool FishingUseNormalSizes { get; set; } = false;
        /// <summary>
        /// The wolfcoin cost for a user to have the bot post a message about
        /// their fishing records.
        /// </summary>
        public int FishingGloatCost { get; set; } = 25;

        /// <summary>
        /// How long, in minutes, a fishing tournament should last. Default is
        /// 15 minutes.
        /// </summary>
        public int FishingTournamentDuration { get; set; } = 15;
        /// <summary>
        /// How long, in minutes, between the end of a tournament and the start
        /// of the next. Default is 15 minutes.
        /// </summary>
        public int FishingTournamentInterval { get; set; } = 15;
        /// <summary>
        /// The shortest time, in seconds, it can take to hook a fish during a
        /// tournament. Default is 15 seconds.
        /// </summary>
        public int FishingTournamentCastMinimum { get; set; } = 15;
        /// <summary>
        /// The longest time, in seconds, it can take to hook a fish during a
        /// tournament. Default is 30 seconds.
        /// </summary>        
        public int FishingTournamentCastMaximum { get; set; } = 30;
    }
}
