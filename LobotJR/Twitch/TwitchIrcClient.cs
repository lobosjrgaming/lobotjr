﻿using LobotJR.Shared.Authentication;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LobotJR.Twitch
{
    /// <summary>
    /// IRC client built for Twitch.
    /// </summary>
    public class TwitchIrcClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string Url = "irc.chat.twitch.tv";
        private static readonly int SslPort = 6697;
        private static readonly int NonSslPort = 6667;
        private static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan ResponseLimit = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ReconnectTimerBase = TimeSpan.FromSeconds(1);

        private TcpClient Client;
        private StreamReader InputStream;
        private StreamWriter OutputStream;
        private TokenData TokenData;
        private TwitchClient TwitchClient;
        private bool IsSecure;

        private CancellationTokenSource CancellationTokenSource;

        private DateTime? LastReconnect;
        private TimeSpan ReconnectTimer = ReconnectTimerBase;

        private DateTime LastMessage;
        private bool PingSent;

        private Queue<string> MessageQueue = new Queue<string>();
        private RollingTimer Timer = new RollingTimer(TimeSpan.FromSeconds(30), 100);

        /// <summary>
        /// Creates a new IRC client.
        /// </summary>
        /// <param name="tokenData">The token data for the authenticated users.</param>
        /// <param name="twitchClient">The twitch client to use when refreshing
        /// the auth token.</param>
        public TwitchIrcClient(TokenData tokenData, TwitchClient twitchClient)
        {
            Client = new TcpClient();
            TokenData = tokenData;
            TwitchClient = twitchClient;
        }

        /// <summary>
        /// Connects the client to the twitch server, authenticates the chat
        /// user, and joins the channel of the broadcast user.
        /// </summary>
        /// <param name="secure">Whether or not to connect using SSL.</param>
        /// <returns>Whether or not the connection succeeded.</returns>
        public async Task<bool> Connect(bool secure = true)
        {
            IsSecure = secure;
            try
            {
                await Client.ConnectAsync(Url, secure ? SslPort : NonSslPort);
                if (Client.Connected)
                {
                    Stream stream = Client.GetStream();
                    if (secure)
                    {
                        var sslStream = new SslStream(stream);
                        await sslStream.AuthenticateAsClientAsync(Url);
                        stream = sslStream;
                    }
                    var encoding = new UTF8Encoding(false);
                    InputStream = new StreamReader(stream, encoding);
                    OutputStream = new StreamWriter(stream, encoding);
                    OutputStream.AutoFlush = false;

                    await WriteLines("CAP REQ :twitch.tv/tags twitch.tv/commands",
                        $"PASS oauth:{TokenData.ChatToken.AccessToken}",
                        $"NICK {TokenData.ChatUser.ToLower()}",
                        $"JOIN #{TokenData.BroadcastUser}");
                    ExpectResponse();
                    return true;
                }
                else
                {
                    Logger.Error("Connection failed!");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return false;
        }

        private async Task Reconnect()
        {
            Client.Dispose();
            Client = new TcpClient();
            var connected = await Connect(IsSecure);
            if (!connected)
            {
                LastReconnect = DateTime.Now;
                var newTime = Math.Min(ReconnectTimer.TotalSeconds * 2, 2048);
                ReconnectTimer = TimeSpan.FromSeconds(newTime);
                Logger.Error("Connection failed. Retrying in {seconds} seconds.", ReconnectTimer.TotalSeconds);
            }
            else
            {
                LastReconnect = null;
                ReconnectTimer = TimeSpan.FromSeconds(ReconnectTimerBase.TotalSeconds);
            }
        }

        private void ExpectResponse()
        {
            LastMessage = DateTime.Now - IdleLimit + ResponseLimit;
        }

        private async Task WriteLine(string line)
        {
            await OutputStream.WriteLineAsync(line);
            await OutputStream.FlushAsync();
        }

        private async Task WriteLines(params string[] lines)
        {
            foreach (var line in lines)
            {
                await OutputStream.WriteLineAsync(line);
            }
            await OutputStream.FlushAsync();
        }

        private async Task<string> Read()
        {
            if (Client.GetStream().DataAvailable)
            {
                var content = await InputStream.ReadLineAsync();
                return content;
            }
            return null;
        }

        /// <summary>
        /// Starts a thread that listens for messages and sends message events.
        /// </summary>
        /// <returns>The cancellation token source used to cancel the thread.</returns>
        public CancellationTokenSource Start()
        {
            CancellationTokenSource = new CancellationTokenSource();
            var task = Task.Factory.StartNew(Run, CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            return CancellationTokenSource;
        }

        private async Task Run()
        {
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                await Process();
                Thread.Sleep(100);
            }
            if (Client.Connected)
            {
                Client.Dispose();
            }
        }

        /// <summary>
        /// Sends any available queued messages and processes any incoming messages.
        /// </summary>
        /// <returns>A collection of messages that have been received since the
        /// last time this method was called.</returns>
        public async Task<IEnumerable<IrcMessage>> Process()
        {
            var output = new List<IrcMessage>();
            if (Client.Connected)
            {
                if (this.MessageQueue.Count > 0)
                {
                    var toSend = this.MessageQueue.Dequeue();
                    if (toSend != null && Timer.AvailableOccurrences() > 0)
                    {
                        await WriteLine($"PRIVMSG #{TokenData.BroadcastUser} :{toSend}");
                        Timer.AddOccurrence(DateTime.Now);
                        ExpectResponse();
                    }
                }

                var input = await Read();
                while (input != null)
                {
                    LastMessage = DateTime.Now;
                    var message = IrcMessage.Parse(input);
                    if (message != null)
                    {
                        if (message.IsChat || message.IsWhisper || message.IsUserNotice)
                        {
                            output.Add(message);
                        }
                        else if ("notice".Equals(message.Command, StringComparison.OrdinalIgnoreCase))
                        {
                            await TwitchClient.RefreshTokens();
                            await Reconnect();
                        }
                        else if ("pong".Equals(message.Command, StringComparison.OrdinalIgnoreCase))
                        {
                            PingSent = false;
                        }
                        else if ("ping".Equals(message.Command, StringComparison.OrdinalIgnoreCase))
                        {
                            await WriteLine($"PONG :{message.Message}");
                        }
                    }
                    input = await Read();
                }
            }

            if (DateTime.Now - LastMessage > IdleLimit && !PingSent)
            {
                await WriteLine("PING");
                PingSent = true;
            }
            else if (DateTime.Now - LastMessage > IdleLimit + ResponseLimit && PingSent)
            {
                Logger.Info("IRC client disconnected. Reconnecting...");
                await Reconnect();
            }
            else if (LastReconnect != null && DateTime.Now - LastReconnect > ReconnectTimer)
            {
                await Reconnect();
            }
            return output;
        }

        /// <summary>
        /// Queues a message to send out on the IRC client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void QueueMessage(string message)
        {
            this.MessageQueue.Enqueue(message);
        }
    }
}
