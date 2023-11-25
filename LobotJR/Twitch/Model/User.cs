using LobotJR.Data;

namespace LobotJR.Twitch.Model
{
    /// <summary>
    /// Maps the username to their twitch id.
    /// </summary>
    public class User : TableObject
    {
        /// <summary>
        /// User's current username.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Id from twitch used to identify the user.
        /// </summary>
        public string TwitchId { get; set; }
        /// <summary>
        /// Whether or not this user is a moderator in the streamer's channel.
        /// </summary>
        public bool IsMod { get; set; }
        /// <summary>
        /// Whether or not this user is a VIP in the streamer's channel.
        /// </summary>
        public bool IsVip { get; set; }
        /// <summary>
        /// Whether or not this user is subscribed to the streamer's channel.
        /// </summary>
        public bool IsSub { get; set; }
        /// <summary>
        /// Whether or not this user is an admin on the bot. Typically this
        /// will be for the streamer and bot accounts.
        /// </summary>
        public bool IsAdmin { get; set; }

        public User() { }

        public User(string name, string id)
        {
            Username = name;
            TwitchId = id;
        }
    }
}
