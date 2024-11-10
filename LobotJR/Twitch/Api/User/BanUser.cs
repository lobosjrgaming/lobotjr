using LobotJR.Twitch.Api.Authentication;
using LobotJR.Twitch.Api.Client;
using LobotJR.Utils.Api;
using NLog;
using RestSharp;
using System.Net;
using System.Threading.Tasks;

namespace LobotJR.Twitch.Api.User
{
    /// <summary>
    /// Provides methods to access the twitch Ban User API.
    /// https://dev.twitch.tv/docs/api/reference/#ban-user
    /// </summary>
    public class BanUser
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calls the twitch Ban User API to ban a user for a set duration.
        /// </summary>
        /// <param name="token">The OAuth token object to use for authentication.
        /// The id of the access token must match the moderator id.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <param name="broadcasterId">The id of the channel to ban the user from.</param>
        /// <param name="moderatorId">The id of the user executing the ban (must be a moderator of the channel).</param>
        /// <param name="userId">The id of the user to ban.</param>
        /// <param name="duration">The duration of the ban. Set this to null for a permanent ban.</param>
        /// <param name="reason">An optional string with the reason for the ban.</param>
        /// <returns>The http response code from the API.</returns>
        public static async Task<HttpStatusCode> Post(TokenResponse token, ClientData clientData, string broadcasterId, string moderatorId, string userId, int? duration, string reason)
        {
            var client = RestUtils.CreateStandardClient();
            var request = RestUtils.CreateStandardRequest("helix/moderation/bans", Method.Post, token.AccessToken, clientData.ClientId, Logger);
            request.AddParameter("broadcaster_id", broadcasterId, ParameterType.QueryString);
            request.AddParameter("moderator_id", moderatorId, ParameterType.QueryString);
            request.AddJsonBody(new BanRequest(userId, duration, reason));
            var response = await RestUtils.ExecuteWithRefresh(token, clientData, client, request);
            return response.StatusCode;
        }
    }
}