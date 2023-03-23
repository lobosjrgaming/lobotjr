using LobotJR.Shared.User;
using LobotJR.Shared.Utility;
using System.Collections.Generic;

namespace LobotJR.Shared.Channel
{
    /// <summary>
    /// The response from a call to get a list of specific users for a channel,
    /// such as moderators or VIPs.
    /// </summary>
    public class ChannelUserResponse
    {
        /// <summary>
        /// The list of users that meet some criteria for a channel.
        /// </summary>
        public IEnumerable<TwitchUserData> Data { get; set; }
        /// <summary>
        /// Contains the information used to page through the list of results.
        /// The object is empty if there are no more pages left to page
        /// through. Read More: https://dev.twitch.tv/docs/api/guide#pagination
        /// </summary>
        public Pagination Pagination { get; set; }
    }
}
