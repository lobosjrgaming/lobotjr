using LobotJR.Shared.Authentication;
using LobotJR.Shared.Client;
using LobotJR.Shared.Utility;
using NLog;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Shared.Channel
{
    /// <summary>
    /// Provides methods to access the twitch Ban User API.
    /// https://dev.twitch.tv/docs/api/reference/#get-broadcaster-subscriptions
    /// </summary>
    public class Subscriptions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calls the Twitch Get Broadcaster Subscriptions API to get the list
        /// of all subscribers to a channel. Retrieves the first 100 users
        /// starting from the specified value.
        /// </summary>
        /// <param name="token">The OAuth token object for the user making the call.
        /// The token id must match the broadcaster id.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <param name="broadcasterId">The id of the channel to get subscribers for.</param>
        /// <param name="start">The pagination cursor value to start from. If
        /// this is the first request, set to null.</param>
        /// <returns>The response body from the API, or null if the response code is not 200 (OK).</returns>
        public static async Task<RestResponse<SubscriptionResponse>> Get(TokenResponse token, ClientData clientData, string broadcasterId, string start = null)
        {
            var client = RestUtils.CreateStandardClient();
            var request = RestUtils.CreateStandardRequest("helix/subscriptions", Method.Get, token.AccessToken, clientData.ClientId, Logger);
            request.AddParameter("broadcaster_id", broadcasterId, ParameterType.QueryString);
            if (start != null)
            {
                request.AddParameter("after", start, ParameterType.QueryString);
            }
            request.AddParameter("first", 100, ParameterType.QueryString);
            return await RestUtils.ExecuteWithRefresh<SubscriptionResponse>(token, clientData, client, request);
        }

        /// <summary>
        /// Gets all users subscribed to a given channel.
        /// </summary>
        /// <param name="token">The OAuth token object for the user making the call.
        /// The token id must match the broadcaster id.</param>
        /// <param name="clientData">The client data for the app executing the request.</param>
        /// <param name="broadcasterId">The id of the channel to ban the user from.</param>
        /// <returns>A list of rest responses containing pages of all subscribers for the channel.</returns>
        public static async Task<IEnumerable<RestResponse<SubscriptionResponse>>> GetAll(TokenResponse token, ClientData clientData, string broadcasterId)
        {
            List<RestResponse<SubscriptionResponse>> data = new List<RestResponse<SubscriptionResponse>>();
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