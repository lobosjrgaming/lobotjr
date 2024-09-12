namespace LobotJR.Command.Model.AccessControl
{
    /// <summary>
    /// Enumeration of flags representing Twitch-level groups of users.
    /// </summary>
    public enum GroupFlags
    {
        /// <summary>
        /// Channel moderator.
        /// </summary>
        Mod,
        /// <summary>
        /// Channel VIP.
        /// </summary>
        Vip,
        /// <summary>
        /// Channel subscriber.
        /// </summary>
        Sub,
        /// <summary>
        /// Special group that contains the chat and broadcast users.
        /// </summary>
        Admin
    }
}
