namespace LobotJR.Shared.User
{

    /// <summary>
    /// A user object containing identifying information from Twitch.
    /// </summary>
    public class TwitchUserData
    {
        /// <summary>
        /// The user's Twitch id.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The user’s login name.
        /// </summary>
        public string UserLogin { get; set; }
        /// <summary>
        /// The user’s display name.
        /// </summary>
        public string UserName { get; set; }
    }
}
