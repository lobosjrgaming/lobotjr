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

        public override bool Equals(object obj)
        {
            return obj is TwitchUserData other && (
                (string.IsNullOrWhiteSpace(other.UserId) && string.IsNullOrWhiteSpace(UserId))
                || other.UserId.Equals(UserId));
        }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }
    }
}
