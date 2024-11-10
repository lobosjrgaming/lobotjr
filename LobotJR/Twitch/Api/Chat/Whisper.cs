using LobotJR.Twitch.Api.Authentication;
using LobotJR.Twitch.Api.Client;
using LobotJR.Utils.Api;
using NLog;
using RestSharp;
using System.Threading.Tasks;

namespace LobotJR.Twitch.Api.Chat
{
    /// <summary>
    /// Provides methods to access the twitch Send Whisper API.
    /// https://dev.twitch.tv/docs/api/reference/#send-whisper
    /// </summary>
    public class Whisper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calls the twitch Send Whisper API to whisper a message to a user.
        /// </summary>
        /// <param name="token">The OAuth token object for the user making the
        /// call. The token id must match the sender id.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <param name="senderId">The id of the user sending the message.</param>
        /// <param name="userId">The id of the user to send the whisper to.</param>
        /// <returns>The rest response object for the API call.</returns>
        public static async Task<RestResponse> Post(TokenResponse token, ClientData clientData, string senderId, string userId, string message)
        {
            var client = RestUtils.CreateStandardClient();
            var request = RestUtils.CreateStandardRequest("helix/whispers", Method.Post, token.AccessToken, clientData.ClientId, Logger);
            request.AddParameter("from_user_id", senderId, ParameterType.QueryString);
            request.AddParameter("to_user_id", userId, ParameterType.QueryString);
            request.AddBody(new WhisperRequest(message));
            return await RestUtils.ExecuteWithRefresh(token, clientData, client, request);
        }
    }
}