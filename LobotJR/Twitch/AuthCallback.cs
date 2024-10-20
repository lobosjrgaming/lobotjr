using LobotJR.Twitch.Api.Authentication;
using LobotJR.Twitch.Api.Client;
using LobotJR.Utils;
using LobotJR.Utils.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LobotJR.Twitch
{
    public class AuthCallback
    {
        public static readonly IEnumerable<string> ChatScopes = new List<string>(new string[] { "chat:read", "chat:edit", "whispers:read", "whispers:edit", "channel:moderate", "user:manage:whispers", "moderator:manage:banned_users", "moderator:read:chatters" });
        public static readonly IEnumerable<string> BroadcastScopes = new List<string>(new string[] { "channel:read:subscriptions", "moderation:read", "channel:read:vips" });
        public static readonly string RedirectUri = "http://localhost:9000/";
        private readonly string ResponseTemplate = "<html><body><h3>{0}</h3><p>{1}</p></body></html>";
        protected readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
        protected DateTime AuthStart = DateTime.Now;
        public string State { get; private set; } = Guid.NewGuid().ToString();

        private string BuildAuthUrl(string clientId, IEnumerable<string> scopes)
        {
            var builder = new UriBuilder("https", "id.twitch.tv")
            {
                Path = "oauth2/authorize"
            };
            AddQuery(builder, "client_id", clientId);
            AddQuery(builder, "redirect_uri", RedirectUri);
            AddQuery(builder, "response_type", "code");
            AddQuery(builder, "scope", string.Join(" ", scopes));
            AddQuery(builder, "force_verify", "true");
            AddQuery(builder, "state", State);
            return builder.Uri.ToString();
        }

        private void AddQuery(UriBuilder builder, string rawKey, string rawValue)
        {
            var key = Uri.EscapeDataString(rawKey);
            var value = Uri.EscapeDataString(rawValue);
            if (!string.IsNullOrWhiteSpace(builder.Query))
            {
                builder.Query = builder.Query.Substring(1) + "&" + key + "=" + value;
            }
            else
            {
                builder.Query = key + "=" + value;
            }
        }

        private async Task SendResponse(Stream outputStream, string header, string body)
        {
            var toSend = string.Format(ResponseTemplate, header, body);
            var bytes = Encoding.UTF8.GetBytes(toSend);
            await outputStream.WriteAsync(bytes, 0, bytes.Length);
            await outputStream.FlushAsync();
        }

        private async Task<string> ProcessResponse(Stream outputStream, IDictionary<string, string> query, string expectedState)
        {
            var header = "Error!";
            var body = "Unexpected error.";
            var code = string.Empty;
            if (query.TryGetValue("error", out var error))
            {
                if (query.TryGetValue("error_description", out var errorDescription))
                {
                    body = $"{error}: {errorDescription}. Close this window and try again.";
                }
                else
                {
                    body = $"{error}. Close this window and try again.";
                }
            }
            if (query.TryGetValue("state", out var state))
            {
                if (!state.Equals(expectedState))
                {
                    body = "CSRF attack detected. Check your firewall settings.";
                }
                if (query.TryGetValue("code", out var requestCode))
                {
                    header = "Authentication complete!";
                    body = "You may now close this window.";
                    code = requestCode;
                }
            }
            await SendResponse(outputStream, header, body);
            return code;
        }

        private async Task<string> GetAuthCode(string url, string state)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri);
            listener.Start();

            Process.Start(new ProcessStartInfo()
            {
                FileName = url,
                UseShellExecute = true
            });

            var contextTask = listener.GetContextAsync();
            if (contextTask.Wait(TimeSpan.FromMinutes(1)))
            {
                var context = contextTask.Result;
                var queryString = context.Request.QueryString;
                var queryDict = queryString.AllKeys.Select(x => x ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToDictionary(x => x, x => queryString.Get(x) ?? "");
                var code = await ProcessResponse(context.Response.OutputStream, queryDict, state);
                listener.Close();
                return code;
            }
            return null;
        }

        public async Task<TokenResponse> GetChatAuthCode(ClientData clientData)
        {
            var code = await GetAuthCode(BuildAuthUrl(clientData.ClientId, ChatScopes), State);
            if (!string.IsNullOrWhiteSpace(code))
            {
                return await AuthToken.Fetch(clientData.ClientId, clientData.ClientSecret, code, RedirectUri);
            }
            return null;
        }

        public async Task<TokenResponse> GetBroadcastAuthCode(ClientData clientData)
        {
            var code = await GetAuthCode(BuildAuthUrl(clientData.ClientId, BroadcastScopes), State);
            if (!string.IsNullOrWhiteSpace(code))
            {
                return await AuthToken.Fetch(clientData.ClientId, clientData.ClientSecret, code, RedirectUri);
            }
            return null;
        }

        public async Task<TokenData> LoadTokens(ClientData clientData)
        {
            if (FileUtils.HasTokenData())
            {
                var tokenData = FileUtils.ReadTokenData();
                if (await ValidateAndRefresh(clientData, tokenData))
                {
                    return tokenData;
                }
            }
            return new TokenData();
        }

        public async Task<bool> ValidateAndRefresh(ClientData clientData, TokenData tokenData)
        {
            RestLogger.SetSensitiveData(clientData, tokenData);
            if (tokenData != null)
            {
                if (tokenData.ChatToken != null)
                {
                    var validationResponse = await AuthToken.Validate(tokenData.ChatToken.AccessToken);
                    if (validationResponse == null)
                    {
                        var response = await AuthToken.Refresh(clientData.ClientId, clientData.ClientSecret, tokenData.ChatToken.RefreshToken);
                        tokenData.ChatToken = response.Data;
                        validationResponse = await AuthToken.Validate(tokenData.ChatToken.AccessToken);
                        tokenData.ChatUser = validationResponse?.Login;
                        tokenData.ChatId = validationResponse?.UserId;
                    }
                    else if (!validationResponse.Login.Equals(tokenData.ChatUser) || ChatScopes.Any(x => !validationResponse.Scopes.Contains(x)))
                    {
                        tokenData.ChatToken = null;
                    }
                }
                if (tokenData.BroadcastToken != null)
                {
                    var validationResponse = await AuthToken.Validate(tokenData.BroadcastToken.AccessToken);
                    if (validationResponse == null)
                    {
                        var response = await AuthToken.Refresh(clientData.ClientId, clientData.ClientSecret, tokenData.BroadcastToken.RefreshToken);
                        tokenData.BroadcastToken = response.Data;
                        validationResponse = await AuthToken.Validate(tokenData.BroadcastToken.AccessToken);
                        tokenData.BroadcastUser = validationResponse?.Login;
                        tokenData.BroadcastId = validationResponse?.UserId;
                    }
                    else if (!validationResponse.Login.Equals(tokenData.BroadcastUser) || BroadcastScopes.Any(x => !validationResponse.Scopes.Contains(x)))
                    {
                        tokenData.BroadcastToken = null;
                    }
                }
                return tokenData.BroadcastToken != null && tokenData.ChatToken != null;
            }
            return false;
        }
    }
}
