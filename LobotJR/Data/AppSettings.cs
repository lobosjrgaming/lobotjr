namespace LobotJR.Data
{
    /// <summary>
    /// Settings that modify the behavior of the app.
    /// </summary>
    public class AppSettings : TableObject
    {
        /// <summary>
        /// The amount of time, in minutes, to wait between calls to update the
        /// viewer, mod, sub, and vip lists.
        /// </summary>
        public int UserDatabaseUpdateTime { get; set; } = 15;
        /// <summary>
        /// The amount of time, in seconds, to wait between the initial request
        /// for a user lookup and the actual call to allow for additional
        /// requests to be batched.
        /// </summary>
        public int UserLookupBatchTime { get; set; } = 5;
        /// <summary>
        /// The maximum number of unique recipients that can be whispered in a
        /// 24-hour period. This is supposed to be 40, according to the twitch
        /// documentation, but lobot appears to have a higher max. This will
        /// update once we get a 429 response from twitch to set our new max.
        /// </summary>
        public int MaxWhisperRecipients { get; set; } = 0;
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
