using LobotJR.Twitch.Api.User;
using LobotJR.Utils.Api;
using System.Collections.Generic;

namespace LobotJR.Twitch.Api.Chat
{
    /// <summary>
    /// Response object for the Get Chatters endpoint.
    /// </summary>
    public class ChattersResponse
    {
        /// <summary>
        /// The list of users that are connected to the broadcaster’s chat
        /// room. The list is empty if no users are connected to the chat room.
        /// </summary>
        public IEnumerable<TwitchUserData> Data { get; set; }
        /// <summary>
        /// Contains the information used to page through the list of results.
        /// The object is empty if there are no more pages left to page
        /// through. Read More: https://dev.twitch.tv/docs/api/guide#pagination
        /// </summary>
        public Pagination Pagination { get; set; }
        /// <summary>
        /// The total number of users in chat.
        /// </summary>
        public int Total { get; set; }
    }
}
