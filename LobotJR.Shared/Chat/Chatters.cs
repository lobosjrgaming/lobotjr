using LobotJR.Shared.Utility;
using NLog;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace LobotJR.Shared.Chat
{
    /// <summary>
    /// Provides methods to access the twitch Get Chatters API.
    /// https://dev.twitch.tv/docs/api/reference/#get-chatters
    /// </summary>
    public class Chatters
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calls the Twitch Get Chatters endpoint to get all users in a given
        /// broadcaster's chat channel. Retrieves the first 1000 users starting
        /// from the specified value.
        /// </summary>
        /// <param name="token">The OAuth token for the user making the call.
        /// The token id must match the moderator id.</param>
        /// <param name="clientId">The client id of the application.</param>
        /// <param name="broadcasterId">The id of the channel to get chatters for.</param>
        /// <param name="moderatorId">The id of the user making the API call.
        /// This user must have moderator priveleges in the channel of the
        /// broadcaster.</param>
        /// <param name="start">The pagination cursor value to start from. If
        /// this is the first request, set to null.</param>
        /// <returns>The response body from the API, or null if the response code is not 200 (OK).</returns>
        public static async Task<RestResponse<ChattersResponse>> Get(string token, string clientId, string broadcasterId, string moderatorId, string start)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new RestClient("https://api.twitch.tv");
            client.UseNewtonsoftJson(SerializerSettings.Default);
            var request = new RestRequest("helix/chat/chatters", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Client-ID", clientId);
            request.AddParameter("broadcaster_id", broadcasterId, ParameterType.QueryString);
            request.AddParameter("moderator_id", moderatorId, ParameterType.QueryString);
            if (start != null)
            {
                request.AddParameter("after", start, ParameterType.QueryString);
            }
            request.AddParameter("first", 1000, ParameterType.QueryString);
            RestLogger.AddLogging(request, Logger);
            return await client.ExecuteAsync<ChattersResponse>(request);
        }

        /// <summary>
        /// Gets all users in a given chat channel.
        /// </summary>
        /// <param name="token">The OAuth token for the user making the call.
        /// The token id must match the moderator id.</param>
        /// <param name="clientId">The client id of the application.</param>
        /// <param name="broadcasterId">The id of the channel to ban the user from.</param>
        /// <param name="moderatorId">The id of the user with moderator priveleges executing the call.</param>
        /// <returns>A list of rest responses containing pages of all users in chat.</returns>
        public static async Task<IEnumerable<RestResponse<ChattersResponse>>> GetAll(string token, string clientId, string broadcasterId, string moderatorId)
        {
            List<RestResponse<ChattersResponse>> data = new List<RestResponse<ChattersResponse>>();
            string cursor = null;
            do
            {
                var response = await Get(token, clientId, broadcasterId, moderatorId, cursor);
                data.Add(response);
                cursor = response.Data?.Pagination?.Cursor;
            }
            while (cursor != null);
            return data;
        }
    }
}
