using LobotJR.Shared.Authentication;
using LobotJR.Shared.Client;
using LobotJR.Shared.Utility;
using NLog;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Shared.User
{
    /// <summary>
    /// Provides methods to access the twitch Get Users API.
    /// https://dev.twitch.tv/docs/api/reference#get-users
    /// </summary>
    public class Users
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calls the twitch Get Users API with no parameters.
        /// </summary>
        /// <param name="token">The OAuth token object to use for authentication.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <returns>The user data of the authenticated user.</returns>
        public static async Task<RestResponse<UserResponse>> Get(TokenResponse token, ClientData clientData)
        {
            var client = RestUtils.CreateStandardClient();
            var request = RestUtils.CreateStandardRequest("helix/users", Method.Get, token.AccessToken, clientData.ClientId, Logger);
            return await RestUtils.ExecuteWithRefresh<UserResponse>(token, clientData, client, request);
        }

        /// <summary>
        /// Calls the twitch Get Users API with a list of usernames
        /// </summary>
        /// <param name="token">The OAuth token object to use for authentication.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <param name="users">A collection of usernames.</param>
        /// <returns>The user data of the users in the collection.</returns>
        public static async Task<RestResponse<UserResponse>> Get(TokenResponse token, ClientData clientData, IEnumerable<string> users)
        {
            var client = RestUtils.CreateStandardClient();
            var request = RestUtils.CreateStandardRequest("helix/users", Method.Get, token.AccessToken, clientData.ClientId, Logger);
            foreach (var user in users)
            {
                request.AddParameter("login", user, ParameterType.QueryString);
            }
            return await RestUtils.ExecuteWithRefresh<UserResponse>(token, clientData, client, request);
        }
    }
}