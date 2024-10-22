using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Data;
using LobotJR.Data.Import;
using LobotJR.Data.Migration;
using LobotJR.Trigger;
using LobotJR.Twitch;
using LobotJR.Twitch.Api.Authentication;
using LobotJR.Twitch.Api.Client;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using LobotJR.Utils.Api;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

namespace LobotJR
{
    public class Bot
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private string LogFile = "output.log";

        public delegate void BotTerminatedEvent();
        public event BotTerminatedEvent BotTerminated;

        public ILifetimeScope Scope { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; } = new CancellationTokenSource();
        public bool TwitchPlays { get; private set; }
        public bool IsLive { get; private set; } = false;
        public bool HasCrashed { get; private set; } = false;

        private void CrashAlert()
        {
            if (IsLive)
            {
                if (HasCrashed)
                {
                    var alertFile = "./Resources/alert.wav";
                    var alertDefault = "./Resources/alert.default.wav";
                    if (!File.Exists(alertFile) && File.Exists(alertDefault))
                    {
                        File.Copy(alertDefault, alertFile);
                    }
                    if (File.Exists(alertFile))
                    {
                        using (var player = new SoundPlayer(alertFile))
                        {
                            player.PlaySync();
                        }
                    }
                    HasCrashed = false;
                }
            }
        }

        private async Task UpdateDatabase(ClientData clientData, TokenData tokenData)
        {
            var updaterContainer = AutofacSetup.SetupUpdater(clientData, tokenData);
            using (var updaterScope = updaterContainer.BeginLifetimeScope())
            {
                var updater = updaterScope.Resolve<SqliteDatabaseUpdater>();
                updater.Initialize();
                if (updater.CurrentVersion < updater.LatestVersion)
                {
                    Logger.Info("Database is out of date, updating to {version}. This could take a few minutes.", updater.LatestVersion);
                    var updateResult = await updater.UpdateDatabase();
                    if (!updateResult.Success)
                    {
                        throw new Exception($"Error occurred updating database from {updateResult.PreviousVersion} to {updateResult.NewVersion}. {updateResult.DebugOutput}");
                    }
                    updater.WriteUpdatedVersion();
                    Logger.Info("Update complete!");
                }
            }
        }

        private ILifetimeScope CreateApplicationScope(ClientData clientData, TokenData tokenData)
        {
            var container = AutofacSetup.Setup(clientData, tokenData);
            return container.BeginLifetimeScope();
        }

        private async Task SeedDatabase(IConnectionManager connectionManager, UserController userController, TokenData tokenData)
        {
            using (await connectionManager.OpenConnection())
            {
                connectionManager.SeedData();
                userController.SetBotUsers(userController.GetOrCreateUser(tokenData.BroadcastId, tokenData.BroadcastUser), userController.GetOrCreateUser(tokenData.ChatId, tokenData.ChatUser));
            }
        }

        private async Task ConfigureLogging(IConnectionManager connectionManager)
        {
            using (await connectionManager.OpenConnection())
            {
                var appSettings = connectionManager.CurrentConnection.AppSettings.Read().First();
                LogFile = appSettings.LoggingFile;
                LogManager.Setup().LoadConfiguration(builder =>
                {
                    builder.ForLogger().FilterMinLevel(LogLevel.Debug)
                        .WriteToFile(fileName: appSettings.LoggingFile,
                        archiveAboveSize: 1024 * 1024 * appSettings.LoggingMaxSize,
                        maxArchiveFiles: appSettings.LoggingMaxArchives);
                });
                var crashDumps = Directory.GetFiles(Directory.GetCurrentDirectory(), "CrashDump.*.log", SearchOption.TopDirectoryOnly);
                var toDelete = crashDumps.OrderByDescending(x => x).Skip(appSettings.LoggingMaxArchives);
                foreach (var file in toDelete)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error attempting to delete crash dump {file}", file);
                        Logger.Error(ex);
                    }
                }
            }
        }

        private void HandleSubNotifications(IEnumerable<IrcMessage> notifications, UserController userController)
        {
            foreach (var sub in notifications)
            {
                if (sub.Tags.TryGetValue("msg-id", out var subMessage))
                {
                    if (subMessage.Equals("sub", StringComparison.OrdinalIgnoreCase)
                        || subMessage.Equals("resub", StringComparison.OrdinalIgnoreCase))
                    {
                        if (sub.Tags.TryGetValue("login", out var user) && sub.Tags.TryGetValue("user-id", out var userId))
                        {
                            var subUser = userController.GetOrCreateUser(userId, user);
                            if (!subUser.IsSub)
                            {
                                userController.SetSub(subUser);
                                Logger.Info("Added {user} to the subs list.", user);
                            }
                        }
                    }
                    else if (subMessage.Equals("subgift", StringComparison.OrdinalIgnoreCase))
                    {
                        if (sub.Tags.TryGetValue("msg-param-recipient-name", out var user) && sub.Tags.TryGetValue("msg-param-recipient-id", out var userId))
                        {
                            var subUser = userController.GetOrCreateUser(userId, user);
                            if (!subUser.IsSub)
                            {
                                userController.SetSub(subUser);
                                Logger.Info("Added {user} to the subs list.", user);
                            }
                        }
                    }
                }
            }
        }

        private async Task HandleTriggersAndCommands(IEnumerable<IrcMessage> messages, UserController userController, ICommandManager commandManager, TriggerManager triggerManager, ITwitchIrcClient ircClient, ITwitchClient twitchClient)
        {
            foreach (var message in messages)
            {
                if (!string.IsNullOrWhiteSpace(message.Message))
                {
                    var chatter = userController.GetOrCreateUser(message.UserId, message.UserName);
                    if (message.Message[0] == CommandManager.Prefix)
                    {
                        // This can't be inside of the command view manager since that automatically catches exceptions thrown by commands
                        if (message.Message == "!testcrash" && chatter.IsAdmin)
                        {
                            throw new Exception($"Test crash initiated by {message.UserName} at {DateTime.Now:yyyyMMddTHHmmssfffZ}");
                        }
                        var result = commandManager.ProcessMessage(message.Message.Substring(1), chatter, message.IsWhisper);
                        if (result != null && result.Processed)
                        {
                            await commandManager.HandleResult(message.Message, result, ircClient, twitchClient, message.IsInternal);
                            continue;
                        }
                    }
                    else if (message.IsChat)
                    {
                        userController.UpdateUser(chatter, message);
                        var triggerResult = triggerManager.ProcessTrigger(message.Message, chatter);
                        if (triggerResult != null && triggerResult.Processed)
                        {
                            await triggerManager.HandleResult(triggerResult, ircClient, twitchClient);
                            continue;
                        }
                    }
                }
            }
        }

        private async Task RunBot()
        {
            var connectionManager = Scope.Resolve<IConnectionManager>();
            var controllerManager = Scope.Resolve<IControllerManager>();
            var commandManager = Scope.Resolve<ICommandManager>();
            var triggerManager = Scope.Resolve<TriggerManager>();
            var twitchClient = Scope.Resolve<TwitchClient>();
            var ircClient = Scope.Resolve<ITwitchIrcClient>();
            var userController = Scope.Resolve<UserController>();
            var lastTime = DateTime.Now;
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    using (await connectionManager.OpenConnection())
                    {
                        await controllerManager.Process();
                        await twitchClient.ProcessQueue();
                        var ircMessages = await ircClient.Process();

                        if (ircMessages.Any())
                        {
                            HandleSubNotifications(ircMessages.Where(x => x.IsUserNotice), userController);
                            await HandleTriggersAndCommands(ircMessages.Where(x => x.IsChat || x.IsWhisper), userController, commandManager, triggerManager, ircClient, twitchClient);
                        }
                    }
                    var now = DateTime.Now;
                    if (lastTime.Day != now.Day)
                    {
                        // db contexts don't get freed until garbage collection runs, so we need this to not get file lock errors
                        GC.Collect();
                        var existing = Directory.GetFiles(".", "data.sqlite.*.archive").OrderBy(x => x);
                        var toDelete = existing.Take(existing.Count() - 4);
                        if (toDelete.Any())
                        {
                            foreach (var file in toDelete)
                            {
                                File.Delete(file);
                            }
                        }
                        File.Copy(".\\data.sqlite", $".\\data.sqlite.{now:yyyyMMdd}.archive");
                    }
                    lastTime = now;
                    // Nominal delay so it doesn't chew up the CPU
                    await Task.Delay(10);
                }
                catch (Exception ex)
                {
                    var now = DateTime.UtcNow;
                    var folder = $"CrashDump.{now:yyyyMMddTHHmmssfffZ}";
                    Logger.Fatal(ex);
                    Logger.Fatal("The application has encountered an unexpected error: {message}", ex.Message);
                    Directory.CreateDirectory(folder);
                    File.Copy($"./{LogFile}", $"{folder}/{LogFile}");
                    ZipFile.CreateFromDirectory(folder, $"{folder}.zip");
                    File.Delete($"{folder}/{LogFile}");
                    Directory.Delete(folder);
                    Logger.Fatal("The full details of the error can be found in {file}", $"{folder}.zip");
                    HasCrashed = true;
                    CrashAlert();
                }
            }
            Scope.Dispose();
            BotTerminated?.Invoke();
        }

        private async Task RunTwitchPlays()
        {
            Logger.Info("Twitch Plays mode enabled!");
            var ircClient = Scope.Resolve<ITwitchIrcClient>();
            await new ChatController(ircClient, CancellationTokenSource).Play();
            Scope.Dispose();
            BotTerminated?.Invoke();
        }

        public async Task PreLoad(ClientData clientData, TokenData tokenData)
        {
            RestLogger.SetSensitiveData(clientData, tokenData);
            await UpdateDatabase(clientData, tokenData);
            Scope = CreateApplicationScope(clientData, tokenData);
            var connectionManager = Scope.Resolve<IConnectionManager>();
            var userController = Scope.Resolve<UserController>();
            await SeedDatabase(connectionManager, userController, tokenData);
            await ConfigureLogging(connectionManager);
            using (await connectionManager.OpenConnection())
            {
                var appSettings = connectionManager.CurrentConnection.AppSettings.Read().First();
                TwitchPlays = appSettings.TwitchPlays;
            }
        }

        public async Task Initialize()
        {
            var connectionManager = Scope.Resolve<IConnectionManager>();
            var userController = Scope.Resolve<UserController>();
            var twitchClient = Scope.Resolve<ITwitchClient>();
            var ircClient = Scope.Resolve<ITwitchIrcClient>();
            var controllerManager = Scope.Resolve<IControllerManager>();
            var commandManager = Scope.Resolve<ICommandManager>();
            var triggerManager = Scope.Resolve<TriggerManager>();
            var playerController = Scope.Resolve<PlayerController>();

            await DataImporter.ImportLegacyData(connectionManager, userController);
            playerController.ExperienceToggled += (bool enabled) => { IsLive = enabled; };
            if (IsLive)
            {
                playerController.EnableAwards(new User("Auto Recovery", ""));
                CrashAlert();
            }

            using (await connectionManager.OpenConnection())
            {
                controllerManager.Initialize();
                twitchClient.Initialize();
            }

            commandManager.InitializeViews();
            commandManager.PushNotifications +=
                async (User user, CommandResult commandResult) =>
                {
                    string message = "Push Notification";
                    commandResult.Sender = user;
                    await commandManager.HandleResult(message, commandResult, ircClient, twitchClient);
                };

            var lastTime = DateTime.Now;
            await ircClient.Connect();
        }

        public void ProcessCommand(string message, string username, string userid)
        {
            var ircClient = Scope.Resolve<ITwitchIrcClient>();
            ircClient.InjectMessage(message, username, userid);
        }

        public CancellationTokenSource Start()
        {
            if (TwitchPlays)
            {
                Task.Factory.StartNew(RunTwitchPlays, CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            else
            {
                Task.Factory.StartNew(RunBot, CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            return CancellationTokenSource;
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }
    }
}