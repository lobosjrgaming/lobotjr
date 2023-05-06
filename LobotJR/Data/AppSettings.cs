namespace LobotJR.Data
{
    /// <summary>
    /// Settings that modify the behavior of the app.
    /// </summary>
    public class AppSettings : TableObject
    {
        /// <summary>
        /// The amount of time, in seconds, to wait between calls to fetch the
        /// ids for users not found in the id cache.
        /// </summary>
        public int GeneralCacheUpdateTime { get; set; } = 5;
        /// <summary>
        /// The maximum number of unique recipients that can be whispered in a
        /// 24-hour period. This is supposed to be 40, according to the twitch
        /// documentation, but lobot appears to have a higher max. This will
        /// update once we get a 429 response from twitch to set our new max.
        /// </summary>
        public int MaxWhisperRecipients { get; set; } = 0;

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

        /// <summary>
        /// The name of the logging file to write output data to.
        /// </summary>
        public string LoggingFile { get; set; } = "output.log";
        /// <summary>
        /// The max size of the logging file in megabytes.
        /// </summary>
        public int LoggingMaxSize { get; set; } = 8;
        /// <summary>
        /// The number of archived logging files and crash dumps to keep.
        /// </summary>
        public int LoggingMaxArchives { get; set; } = 8;
    }
}
