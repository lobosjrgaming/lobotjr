using LobotJR.Shared.Utility;
using NLog;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace LobotJR.Shared.Channel
{
    /// <summary>
    /// Provides methods to access the twitch Get VIPs API.
    /// https://dev.twitch.tv/docs/api/reference/#get-moderators
    /// </summary>
    public class VIPs
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calls the Twitch Get Moderators API to get the list of all VIPs of
        /// a channel. Retrieves the first 100 users starting from the
        /// specified value.
        /// </summary>
        /// <param name="token">The OAuth token for the user making the call.
        /// The token id must match the broadcaster id.</param>
        /// <param name="clientId">The client id of the application.</param>
        /// <param name="broadcasterId">The id of the channel to get VIPs for.</param>
        /// <param name="start">The pagination cursor value to start from. If
        /// this is the first request, set to null.</param>
        /// <returns>The response body from the API, or null if the response code is not 200 (OK).</returns>
        public static async Task<RestResponse<ChannelUserResponse>> Get(string token, string clientId, string broadcasterId, string start = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new RestClient("https://api.twitch.tv");
            client.UseNewtonsoftJson(SerializerSettings.Default);
            var request = new RestRequest("helix/channels/vips", Method.Get);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Client-ID", clientId);
            request.AddParameter("broadcaster_id", broadcasterId, ParameterType.QueryString);
            if (start != null)
            {
                request.AddParameter("after", start, ParameterType.QueryString);
            }
            request.AddParameter("first", 100, ParameterType.QueryString);
            RestLogger.AddLogging(request, Logger);
            return await client.ExecuteAsync<ChannelUserResponse>(request);
        }

        /// <summary>
        /// Gets all VIPs for a given channel.
        /// </summary>
        /// <param name="token">The OAuth token for the user making the call.
        /// The token id must match the broadcaster id.</param>
        /// <param name="clientId">The client id of the application.</param>
        /// <param name="broadcasterId">The id of the channel to get VIPs for.</param>
        /// <returns>A list of rest responses containing pages of all VIPs for the channel.</returns>
        public static async Task<IEnumerable<RestResponse<ChannelUserResponse>>> GetAll(string token, string clientId, string broadcasterId)
        {
            List<RestResponse<ChannelUserResponse>> data = new List<RestResponse<ChannelUserResponse>>();
            string cursor = null;
            do
            {
                var response = await Get(token, clientId, broadcasterId, cursor);
                data.Add(response);
                cursor = response.Data?.Pagination?.Cursor;
            }
            while (cursor != null);
            return data;
        }
    }
}