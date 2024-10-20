using LobotJR.Twitch.Api.Authentication;
using LobotJR.Twitch.Api.Client;
using LobotJR.Utils.Api;
using NLog;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Twitch.Api.Channel
{
    /// <summary>
    /// Provides methods to access the twitch Get Moderators API.
    /// https://dev.twitch.tv/docs/api/reference/#get-moderators
    /// </summary>
    public class Moderators
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calls the Twitch Get Moderators API to get the list of all
        /// moderators of a channel. Retrieves the first 100 users starting
        /// from the specified value.
        /// </summary>
        /// <param name="token">The OAuth token object for the user making the call.
        /// The token id must match the broadcaster id.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <param name="broadcasterId">The id of the channel to get moderators for.</param>
        /// <param name="start">The pagination cursor value to start from. If
        /// this is the first request, set to null.</param>
        /// <returns>The response body from the API, or null if the response code is not 200 (OK).</returns>
        public static async Task<RestResponse<ChannelUserResponse>> Get(TokenResponse token, ClientData clientData, string broadcasterId, string start = null)
        {
            var client = RestUtils.CreateStandardClient();
            var request = RestUtils.CreateStandardRequest("helix/moderation/moderators", Method.Get, token.AccessToken, clientData.ClientId, Logger);
            request.AddParameter("broadcaster_id", broadcasterId, ParameterType.QueryString);
            if (start != null)
            {
                request.AddParameter("after", start, ParameterType.QueryString);
            }
            request.AddParameter("first", 100, ParameterType.QueryString);
            return await RestUtils.ExecuteWithRefresh<ChannelUserResponse>(token, clientData, client, request);
        }

        /// <summary>
        /// Gets all moderators for a given channel.
        /// </summary>
        /// <param name="token">The OAuth token object for the user making the call.
        /// The token id must match the broadcaster id.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <param name="broadcasterId">The id of the channel to get moderators for.</param>
        /// <returns>A list of rest responses containing pages of all moderators for the channel.</returns>
        public static async Task<IEnumerable<RestResponse<ChannelUserResponse>>> GetAll(TokenResponse token, ClientData clientData, string broadcasterId)
        {
            List<RestResponse<ChannelUserResponse>> data = new List<RestResponse<ChannelUserResponse>>();
            string cursor = null;
            do
            {
                var response = await Get(token, clientData, broadcasterId, cursor);
                data.Add(response);
                cursor = response.Data?.Pagination?.Cursor;
            }
            while (cursor != null);
            return data;
        }
    }
}