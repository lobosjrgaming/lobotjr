using Autofac;
using LobotJR.Command;
using LobotJR.Command.System;
using LobotJR.Command.System.Player;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Data.Import;
using LobotJR.Data.Migration;
using LobotJR.Shared.Authentication;
using LobotJR.Shared.Client;
using LobotJR.Shared.Utility;
using LobotJR.Trigger;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading.Tasks;

namespace LobotJR
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static bool isLive = false;
        private static bool hasCrashed = false;

        static async Task Main(string[] args)
        {
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole(layout: "${time}|${level:uppercase=true}|${message:withexception=true}");
            });
            Logger.Info("Launching Lobot version {version}", Assembly.GetExecutingAssembly().GetName().Version);
            while (true)
            {
                try
                {
                    await Initialize();
                }
                catch (Exception ex)
                {
                    var now = DateTime.UtcNow;
                    var folder = $"CrashDump.{now.ToString("yyyyMMddTHHmmssfffZ")}";
                    Logger.Error(ex);
                    Logger.Error("The application has encountered an unexpected error: {message}", ex.Message);
                    Directory.CreateDirectory(folder);
                    File.Copy("./output.log", $"{folder}/output.log");
                    ZipFile.CreateFromDirectory(folder, $"{folder}.zip");
                    File.Delete($"{folder}/output.log");
                    Directory.Delete(folder);
                    Logger.Error("The full details of the error can be found in {file}", $"{folder}.zip");
                    hasCrashed = true;
                    CrashAlert();
                }
            }
        }

        private static void CrashAlert()
        {
            if (isLive)
            {
                if (hasCrashed)
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
                    hasCrashed = false;
                }
            }
        }

        private static async Task UpdateDatabase(ClientData clientData, TokenData tokenData)
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

        private static ILifetimeScope CreateApplicationScope(ClientData clientData, TokenData tokenData)
        {
            var container = AutofacSetup.Setup(clientData, tokenData);
            return container.BeginLifetimeScope();
        }

        private static void SeedDatabase(IConnectionManager connectionManager, UserSystem userSystem, ClientData clientData, TokenData tokenData)
        {
            using (connectionManager.OpenConnection())
            {
                connectionManager.SeedMetadata();
                connectionManager.SeedAppSettings();
                connectionManager.SeedGameSettings();
                userSystem.LastUpdate = DateTime.MinValue;
                userSystem.SetBotUsers(userSystem.GetOrCreateUser(tokenData.BroadcastId, tokenData.BroadcastUser), userSystem.GetOrCreateUser(tokenData.ChatId, tokenData.ChatUser));
            }
        }

        private static void ConfigureLogging(IConnectionManager connectionManager)
        {
            using (connectionManager.OpenConnection())
            {
                var appSettings = connectionManager.CurrentConnection.AppSettings.Read().First();
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

        private static async Task ImportLegacyData(UserSystem userSystem)
        {
            using (var database = new SqliteRepositoryManager())
            {
                await DataImporter.ImportLegacyData(database, userSystem);
            }
        }

        private static void HandleSubNotifications(IEnumerable<IrcMessage> notifications, UserSystem userSystem)
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
                            var subUser = userSystem.GetOrCreateUser(userId, user);
                            if (!subUser.IsSub)
                            {
                                userSystem.SetSub(subUser);
                                Logger.Info("Added {user} to the subs list.", user);
                            }
                        }
                    }
                    else if (subMessage.Equals("subgift", StringComparison.OrdinalIgnoreCase))
                    {
                        if (sub.Tags.TryGetValue("msg-param-recipient-name", out var user) && sub.Tags.TryGetValue("msg-param-recipient-id", out var userId))
                        {
                            var subUser = userSystem.GetOrCreateUser(userId, user);
                            if (!subUser.IsSub)
                            {
                                userSystem.SetSub(subUser);
                                Logger.Info("Added {user} to the subs list.", user);
                            }
                        }
                    }
                }
            }
        }

        private static async Task HandleTriggersAndCommands(IEnumerable<IrcMessage> messages, UserSystem userSystem, ICommandManager commandManager, TriggerManager triggerManager, ITwitchIrcClient ircClient, ITwitchClient twitchClient)
        {
            foreach (var message in messages)
            {
                if (!string.IsNullOrWhiteSpace(message.Message))
                {
                    var chatter = userSystem.GetOrCreateUser(message.UserId, message.UserName);
                    if (message.Message[0] == CommandManager.Prefix)
                    {
                        // This can't be inside of the command module manager since that automatically catches exceptions thrown by commands
                        if (message.Message == "!testcrash" && chatter.IsAdmin)
                        {
                            throw new Exception($"Test crash initiated by {message.UserName} at {DateTime.Now.ToString("yyyyMMddTHHmmssfffZ")}");
                        }
                        var result = commandManager.ProcessMessage(message.Message.Substring(1), chatter, message.IsWhisper);
                        if (result != null && result.Processed)
                        {
                            await commandManager.HandleResult(message.Message, result, ircClient, twitchClient);
                            continue;
                        }
                    }
                    else if (message.IsChat)
                    {
                        userSystem.UpdateUser(chatter, message);
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

        private static async Task RunBot(ILifetimeScope scope)
        {
            var twitchClient = scope.Resolve<ITwitchClient>();
            var ircClient = scope.Resolve<ITwitchIrcClient>();
            var systemManager = scope.Resolve<ISystemManager>();
            var commandManager = scope.Resolve<ICommandManager>();
            var connectionManager = scope.Resolve<IConnectionManager>();
            var triggerManager = scope.Resolve<TriggerManager>();

            var userSystem = scope.Resolve<UserSystem>();
            var playerSystem = scope.Resolve<PlayerSystem>();

            await ImportLegacyData(userSystem);
            playerSystem.ExperienceToggled += (bool enabled) => { isLive = enabled; };
            if (isLive)
            {
                playerSystem.EnableAwards(new User("Auto Recovery", ""));
                CrashAlert();
            }

            commandManager.InitializeModules();
            commandManager.PushNotifications +=
                (User user, CommandResult commandResult) =>
                {
                    string message = "Push Notification";
                    commandManager.HandleResult(message, commandResult, ircClient, twitchClient);
                };

            using (connectionManager.OpenConnection())
            {
                systemManager.Initialize();
            }

            await ircClient.Connect();

            while (true)
            {
                await systemManager.Process();
                await twitchClient.ProcessQueue();
                var ircMessages = await ircClient.Process();

                if (ircMessages.Any())
                {
                    using (connectionManager.OpenConnection())
                    {
                        HandleSubNotifications(ircMessages.Where(x => x.IsUserNotice), userSystem);
                        await HandleTriggersAndCommands(ircMessages.Where(x => x.IsChat || x.IsWhisper), userSystem, commandManager, triggerManager, ircClient, twitchClient);
                    }
                }
                // Nominal delay so it doesn't chew up the CPU
                await Task.Delay(10);
            }
        }

        private static async Task Initialize()
        {
            bool twitchPlays = false;
            var clientData = FileUtils.ReadClientData();
            var tokenData = FileUtils.ReadTokenData();

            RestLogger.SetSensitiveData(clientData, tokenData);
            await UpdateDatabase(clientData, tokenData);
            using (var scope = CreateApplicationScope(clientData, tokenData))
            {
                var connectionManager = scope.Resolve<IConnectionManager>();
                var userSystem = scope.Resolve<UserSystem>();
                using (connectionManager.OpenConnection())
                {
                    SeedDatabase(connectionManager, userSystem, clientData, tokenData);
                    ConfigureLogging(connectionManager);
                    var appSettings = connectionManager.CurrentConnection.AppSettings.Read().First();
                    twitchPlays = appSettings.TwitchPlays;
                }

                if (twitchPlays)
                {
                    Logger.Info("Twitch Plays mode enabled!");
                    var ircClient = scope.Resolve<ITwitchIrcClient>();
                    await ircClient.Connect();
                    await new ChatController().Play(ircClient);
                }
                else
                {
                    await RunBot(scope);
                }
            }
        }
    }
}