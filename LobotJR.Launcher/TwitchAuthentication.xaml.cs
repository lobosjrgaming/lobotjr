﻿using LobotJR.Shared.Authentication;
using LobotJR.Shared.Client;
using LobotJR.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LobotJR.Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly IEnumerable<string> _chatScopes = new List<string>(new string[] { "chat:read", "chat:edit", "whispers:read", "whispers:edit", "channel:moderate", "user:manage:whispers", "moderator:manage:banned_users", "moderator:read:chatters" });
        private static readonly IEnumerable<string> _broadcastScopes = new List<string>(new string[] { "channel:read:subscriptions", "moderation:read", "channel:read:vips" });

        private DispatcherTimer _timer;
        private int _chatUrlTimer;
        private string _chatUrlCaption;
        private int _streamerUrlTimer;
        private string _streamerUrlCaption;

        private ClientData _clientData;
        private readonly string _state = Guid.NewGuid().ToString();

        private TokenData _tokenData;


        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _chatUrlCaption = ChatUrl.Content.ToString();
            _streamerUrlCaption = ChatUrl.Content.ToString();
            SetEnabled(false);

            _clientData = LoadClientData();
            _tokenData = await LoadTokenData();
            RestLogger.SetSensitiveData(_clientData, _tokenData);

            if (_tokenData.ChatToken != null || _tokenData.BroadcastToken != null)
            {
                await ValidateTokens();
            }
            else
            {
                SetEnabled(true);
            }
        }

        private void SetEnabled(bool enabled)
        {
            ChatToken.IsEnabled = enabled;
            ChatUrl.IsEnabled = enabled;
            StreamerToken.IsEnabled = enabled;
            StreamerUrl.IsEnabled = enabled;
            Validate.IsEnabled = enabled;
            UpdateClientData.IsEnabled = enabled;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_chatUrlTimer > 0)
            {
                _chatUrlTimer -= _timer.Interval.Seconds;
                if (_chatUrlTimer == 0)
                {
                    ChatUrl.IsEnabled = true;
                    ChatUrl.Content = _chatUrlCaption;
                }
            }
            if (_streamerUrlTimer > 0)
            {
                _streamerUrlTimer -= _timer.Interval.Seconds;
                if (_streamerUrlTimer == 0)
                {
                    StreamerUrl.IsEnabled = true;
                    StreamerUrl.Content = _streamerUrlCaption;
                }
            }
        }

        private void LaunchBot()
        {
            FileUtils.WriteTokenData(_tokenData);
            Dispatcher.Invoke(() =>
            {
                Hide();
                using (var lobot = new Process())
                {
                    lobot.StartInfo.FileName = "LobotJR.exe";
                    lobot.StartInfo.UseShellExecute = true;
                    lobot.Start();
                }
                Close();
            });
        }

        private async Task<TokenResponse> HandleAuthResponse(Uri uri)
        {
            var returnValues = uri.Query.Substring(1).Split('&')
                .Select(x => x.Split('=')).ToDictionary(key => key[0], value => value[1]);

            if (returnValues["state"] == _state)
            {
                var tokenData = await AuthToken.Fetch(_clientData.ClientId, _clientData.ClientSecret, returnValues["code"], _clientData.RedirectUri);
                return tokenData;
            }
            else
            {
                MessageBox.Show(this, "CSRF attack detected, token rejected.", "Security Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return null;
            }
        }

        private void AddQuery(UriBuilder builder, string rawKey, string rawValue)
        {
            var key = Uri.EscapeUriString(rawKey);
            var value = Uri.EscapeUriString(rawValue);
            if (!string.IsNullOrWhiteSpace(builder.Query))
            {
                builder.Query = builder.Query.Substring(1) + "&" + key + "=" + value;
            }
            else
            {
                builder.Query = key + "=" + value;
            }
        }

        private ClientData LoadClientData()
        {
            ClientData clientData;
            if (FileUtils.HasClientData())
            {
                clientData = FileUtils.ReadClientData();
                if (string.IsNullOrWhiteSpace(clientData.ClientId)
                    || string.IsNullOrWhiteSpace(clientData.ClientSecret)
                    || string.IsNullOrWhiteSpace(clientData.RedirectUri))
                {
                    LaunchClientDataUpdater(clientData);
                }
            }
            else
            {
                clientData = new ClientData()
                {
                    ClientSecret = FileUtils.ReadLegacySecret()
                };
                LaunchClientDataUpdater(clientData);
            }
            return clientData;
        }

        private async Task<TokenData> LoadTokenData()
        {
            if (FileUtils.HasTokenData())
            {
                var tokenData = FileUtils.ReadTokenData();
                RestLogger.SetSensitiveData(_clientData, _tokenData);
                if (tokenData != null)
                {
                    if (tokenData.ChatToken != null)
                    {
                        var validationResponse = await AuthToken.Validate(tokenData.ChatToken.AccessToken);
                        if (validationResponse == null)
                        {
                            var response = await AuthToken.Refresh(_clientData.ClientId, _clientData.ClientSecret, tokenData.ChatToken.RefreshToken);
                            tokenData.ChatToken = response.Data;
                        }
                        else if (!validationResponse.Login.Equals(tokenData.ChatUser) || _chatScopes.Any(x => !validationResponse.Scopes.Contains(x)))
                        {
                            tokenData.ChatToken = null;
                        }
                    }
                    if (tokenData.BroadcastToken != null)
                    {
                        var validationResponse = await AuthToken.Validate(tokenData.BroadcastToken.AccessToken);
                        if (validationResponse == null)
                        {
                            var response = await AuthToken.Refresh(_clientData.ClientId, _clientData.ClientSecret, tokenData.BroadcastToken.RefreshToken);
                            tokenData.BroadcastToken = response.Data;
                        }
                        else if (!validationResponse.Login.Equals(tokenData.BroadcastUser) || _broadcastScopes.Any(x => !validationResponse.Scopes.Contains(x)))
                        {
                            tokenData.BroadcastToken = null;
                        }
                    }
                    return tokenData;
                }
            }
            return new TokenData();
        }

        private void LaunchClientDataUpdater(ClientData clientData)
        {
            var updateModal = new UpdateConfig
            {
                ClientIdValue = clientData.ClientId,
                ClientSecretValue = clientData.ClientSecret,
                RedirectUriValue = clientData.RedirectUri
            };
            var result = updateModal.ShowDialog();
            if (!result.HasValue || !result.Value)
            {
                MessageBox.Show(this, "Unable to launch lobot without proper client config. Closing.", "Missing Client Config", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            else
            {
                clientData.ClientId = updateModal.ClientIdValue;
                clientData.ClientSecret = updateModal.ClientSecretValue;
                clientData.RedirectUri = updateModal.RedirectUriValue;
                FileUtils.WriteClientData(clientData);
            }
        }

        private string BuildTwitchAuthUrl(IEnumerable<string> scopes)
        {
            var builder = new UriBuilder("https", "id.twitch.tv")
            {
                Path = "oauth2/authorize"
            };
            AddQuery(builder, "client_id", _clientData.ClientId);
            AddQuery(builder, "redirect_uri", _clientData.RedirectUri);
            AddQuery(builder, "response_type", "code");
            AddQuery(builder, "scope", string.Join(" ", scopes));
            AddQuery(builder, "force_verify", "true");
            AddQuery(builder, "state", _state);
            return builder.Uri.ToString();
        }

        private void UpdateClientData_Click(object sender, RoutedEventArgs e)
        {
            LaunchClientDataUpdater(_clientData);
            BuildTwitchAuthUrl(_chatScopes);
        }

        private void ChatUrl_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(BuildTwitchAuthUrl(_chatScopes));
            ChatUrl.Content = "Copied!";
            ChatUrl.IsEnabled = false;
            _chatUrlTimer = 3;
        }

        private void StreamerUrl_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(BuildTwitchAuthUrl(_broadcastScopes));
            StreamerUrl.Content = "Copied!";
            StreamerUrl.IsEnabled = false;
            _streamerUrlTimer = 3;
        }

        private async void Validate_Click(object sender, RoutedEventArgs e)
        {
            SetEnabled(false);

            if (!string.IsNullOrWhiteSpace(ChatToken.Text))
            {
                _tokenData.ChatToken = await HandleAuthResponse(new Uri(ChatToken.Text));
            }

            if (!string.IsNullOrWhiteSpace(StreamerToken.Text))
            {
                _tokenData.BroadcastToken = await HandleAuthResponse(new Uri(StreamerToken.Text));
            }

            await ValidateTokens();
        }

        private async Task ValidateTokens()
        {
            var resultText = "Token validation failed. Try again.";
            ChatToken.Text = "Validating token...";
            if (_tokenData.ChatToken != null)
            {
                var validationResponse = await AuthToken.Validate(_tokenData.ChatToken.AccessToken);
                _tokenData.ChatUser = validationResponse.Login;
                _tokenData.ChatId = validationResponse.UserId;
                if (_tokenData.ChatToken != null)
                {
                    resultText = "Token validated successfully!";
                }
            }
            ChatToken.Text = resultText;

            resultText = "Token validation failed. Try again.";
            StreamerToken.Text = "Validating token...";
            if (_tokenData.BroadcastToken != null)
            {
                var validationResponse = await AuthToken.Validate(_tokenData.BroadcastToken.AccessToken);
                _tokenData.BroadcastUser = validationResponse.Login;
                _tokenData.BroadcastId = validationResponse.UserId;
                if (_tokenData.BroadcastToken != null)
                {
                    resultText = "Token validated successfully!";
                }
            }
            StreamerToken.Text = resultText;

            if (_tokenData.ChatToken != null && _tokenData.BroadcastToken != null)
            {
                LaunchBot();
            }
            else
            {
                SetEnabled(true);
            }
        }
    }
}