using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LobotJR.Auth
{
    public class AuthCallback
    {
        public static readonly IEnumerable<string> ChatScopes = new List<string>() { "chat:read", "chat:edit", "whispers:read", "whispers:edit", "channel:moderate", "user:manage:whispers", "moderator:manage:banned_users", "moderator:read:chatters" };
        public static readonly IEnumerable<string> BroadcastScopes = new List<string>() { "channel:read:subscriptions", "moderation:read", "channel:read:vips" };
        private readonly string ResponseTemplate = "<html><body><h3>{0}</h3><p>{1}</p></body></html>";
        protected readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
        protected DateTime AuthStart = DateTime.Now;
        public readonly string RedirectUri = "http://localhost:9000/";
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

        public async Task<string> GetAuthCode(string clientId)
        {
            return await GetAuthCode(BuildAuthUrl(clientId, ChatScopes), State);
        }

        protected void AddQuery(UriBuilder builder, string rawKey, string rawValue)
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

        protected async Task<string> GetAuthCode(string url, string state)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri);
            listener.Start();

            Process.Start(new ProcessStartInfo()
            {
                FileName = url,
                UseShellExecute = true
            });

            var context = await listener.GetContextAsync();
            var queryString = context.Request.QueryString;
            var queryDict = queryString.AllKeys.Select(x => x ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToDictionary(x => x, x => queryString.Get(x) ?? "");
            var code = await ProcessResponse(context.Response.OutputStream, queryDict, state);
            listener.Close();
            return code;
        }
    }
}
