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
        /// <summary>
        /// The cost to gloat about a player's level.
        /// </summary>
        public int LevelGloatCost { get; set; } = 25;
        /// <summary>
        /// The cost to gloat about a player's pet.
        /// </summary>
        public int PetGloatCost { get; set; } = 25;
        /// <summary>
        /// The amount of experience a pet needs to gain a level.
        /// </summary>
        public int PetExperienceToLevel { get; set; } = 150;
        /// <summary>
        /// The maximum level a pet can be.
        /// </summary>
        public int PetLevelMax { get; set; } = 10;
        /// <summary>
        /// The amount of affection a pet gains when fed.
        /// </summary>
        public int PetFeedingAffection { get; set; } = 5;
        /// <summary>
        /// The cost to feed a pet.
        /// </summary>
        public int PetFeedingCost { get; set; } = 5;
        /// <summary>
        /// The value the pet's hunger is set to when fed.
        /// </summary>
        public int PetHungerMax { get; set; } = 100;
        /// <summary>
        /// The max number of players in a dungeon party.
        /// </summary>
        public int DungeonPartySize { get; set; } = 3;
        /// <summary>
        /// The base cost for running a dungeon.
        /// </summary>
        public int DungeonBaseCost { get; set; } = 25;
        /// <summary>
        /// The additional cost, per level, for running a dungeon.
        /// </summary>
        public int DungeonLevelCost { get; set; } = 10;
        /// <summary>
        /// The amount of time, in milliseconds, between each dungeon step
        /// being processed.
        /// </summary>
        public int DungeonStepTime { get; set; } = 9000;
        /// <summary>
        /// The base chance for a player to die when the party fails a dungeon
        /// encounter.
        /// </summary>
        public float DungeonDeathChance { get; set; } = 0.25f;
        /// <summary>
        /// The base chance for a player to gain bonus experience from
        /// completing a dungeon.
        /// </summary>
        public float DungeonCritChance { get; set; } = 0.25f;
        /// <summary>
        /// The additional amount multiplier applied to the experience gained
        /// when a player triggers bonus experience upon completing a dungeon.
        /// The bonus experience is this value plus one, times the normal
        /// experience amount.
        /// </summary>
        public float DungeonCritBonus { get; set; } = 1f;
        /// <summary>
        /// True if the dungeon level ranges are used to determine if a player
        /// can run a dungeon. If this value is false, any player can run any
        /// dungeon regardless of level.
        /// </summary>
        public bool DungeonLevelRestrictions { get; set; } = false;
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
