using LobotJR.Twitch.Model;
using System;

namespace LobotJR.Command.Model.Twitch
{
    /// <summary>
    /// Request to fetch the user data from Twitch for a for a user that
    /// doesn't exist in the database.
    /// </summary>
    public class LookupRequest
    {
        /// <summary>
        /// The name of the user
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// The callback to execute once the lookup is complete.
        /// </summary>
        public Action<User> Callback { get; set; }

        public LookupRequest(string username, Action<User> callback)
        {
            Username = username;
            Callback = callback;
        }
    }
}
