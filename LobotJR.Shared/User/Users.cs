using LobotJR.Shared.Authentication;
using LobotJR.Shared.Client;
using LobotJR.Shared.Utility;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private static readonly int UriMaxLength = 2083;
        private static readonly int ApiRateLimit = 800;

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
        public static async Task<IEnumerable<RestResponse<UserResponse>>> Get(TokenResponse token, ClientData clientData, IEnumerable<string> users)
        {
            var data = new List<RestResponse<UserResponse>>();
            var cursor = 0;
            var total = users.Count();
            var userBatch = users.Take(100);
            var start = DateTime.Now;
            var requestCount = 0;
            var logTime = DateTime.Now;
            do
            {
                if (requestCount >= ApiRateLimit - 1)
                {
                    if (DateTime.Now - start < TimeSpan.FromMinutes(1))
                    {
                        var remainingTime = TimeSpan.FromMinutes(1) - (DateTime.Now - start);
                        Logger.Info("API Rate limit hit fetching users, suspending for {remainingTime}ms.", remainingTime.TotalMilliseconds);
                        Thread.Sleep((int)remainingTime.TotalMilliseconds);
                        start = DateTime.Now;
                    }
                }
                if (DateTime.Now - logTime > TimeSpan.FromSeconds(5))
                {
                    var elapsed = DateTime.Now - start;
                    var estimate = elapsed - TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / cursor * total);
                    Logger.Info("{count} total users processed. {elapsed} time elapsed, {estimate} estimated remaining.", cursor, elapsed.ToString("hh\\:mm\\:ss"), estimate.ToString("hh\\:mm\\:ss"));
                    logTime = DateTime.Now;
                }
                var client = RestUtils.CreateStandardClient();
                var request = RestUtils.CreateStandardRequest("helix/users", Method.Get, token.AccessToken, clientData.ClientId, Logger);
                var uriLengthLeft = UriMaxLength - request.Resource.Length;
                var actualUserCount = 0;
                foreach (var user in userBatch)
                {
                    if (uriLengthLeft > user.Length + 7)
                    {
                        uriLengthLeft -= user.Length + 7;
                        request.AddParameter("login", user.Trim(), ParameterType.QueryString, false);
                        actualUserCount++;
                    }
                    else
                    {
                        break;
                    }
                }
                data.Add(await RestUtils.ExecuteWithRefresh<UserResponse>(token, clientData, client, request));
                requestCount++;
                cursor += actualUserCount;
                userBatch = users.Skip(cursor).Take(100);
            }
            while (userBatch.Any());
            Logger.Info("Data for {count} users retrieved!", cursor);
            return data;
        }
    }
}