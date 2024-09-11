using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
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

        private static string logFile = "output.log";
        private static bool isLive = false;
        private static bool hasCrashed = false;
        private static bool doRecover = true;

        static async Task Main()
        {
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole(layout: "${time}|${level:uppercase=true}|${message:withexception=true}");
            });
            Logger.Info("Launching Lobot version {version}", Assembly.GetExecutingAssembly().GetName().Version);
            var clientData = FileUtils.ReadClientData();
            var tokenData = FileUtils.ReadTokenData();
            //This is outside of the try loop because if it fails, it will never succeed until the database state is corrected
            await ConfigureDatabase(clientData, tokenData);
            while (doRecover)
            {
                try
                {
                    await Initialize(clientData, tokenData);
                }
                catch (Exception ex)
                {
                    var now = DateTime.UtcNow;
                    var folder = $"CrashDump.{now:yyyyMMddTHHmmssfffZ}";
                    Logger.Error(ex);
                    Logger.Error("The application has encountered an unexpected error: {message}", ex.Message);
                    Directory.CreateDirectory(folder);
                    File.Copy($"./{logFile}", $"{folder}/{logFile}");
                    ZipFile.CreateFromDirectory(folder, $"{folder}.zip");
                    File.Delete($"{folder}/{logFile}");
                    Directory.Delete(folder);
                    Logger.Error("The full details of the error can be found in {file}", $"{folder}.zip");
                    hasCrashed = true;
                    CrashAlert();
                }
            }
            Console.ReadKey();
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

        private static void SeedDatabase(IConnectionManager connectionManager, UserController userController, TokenData tokenData)
        {
            using (connectionManager.OpenConnection())
            {
                connectionManager.SeedData();
                userController.LastUpdate = DateTime.MinValue;
                userController.SetBotUsers(userController.GetOrCreateUser(tokenData.BroadcastId, tokenData.BroadcastUser), userController.GetOrCreateUser(tokenData.ChatId, tokenData.ChatUser));
            }
        }

        private static void ConfigureLogging(IConnectionManager connectionManager)
        {
            using (connectionManager.OpenConnection())
            {
                var appSettings = connectionManager.CurrentConnection.AppSettings.Read().First();
                logFile = appSettings.LoggingFile;
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

        private static async Task ImportLegacyData(IConnectionManager connectionManager, UserController userController)
        {
            using (var database = connectionManager.OpenConnection())
            {
                await DataImporter.ImportLegacyData(database, userController);
            }
        }

        private static void HandleSubNotifications(IEnumerable<IrcMessage> notifications, UserController userController)
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

        private static async Task HandleTriggersAndCommands(IEnumerable<IrcMessage> messages, UserController userController, ICommandManager commandManager, TriggerManager triggerManager, ITwitchIrcClient ircClient, ITwitchClient twitchClient)
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
                            await commandManager.HandleResult(message.Message, result, ircClient, twitchClient);
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

        private static async Task RunBot(ILifetimeScope scope)
        {
            var twitchClient = scope.Resolve<ITwitchClient>();
            var ircClient = scope.Resolve<ITwitchIrcClient>();
            var controllerManager = scope.Resolve<IControllerManager>();
            var commandManager = scope.Resolve<ICommandManager>();
            var connectionManager = scope.Resolve<IConnectionManager>();
            var triggerManager = scope.Resolve<TriggerManager>();

            var userController = scope.Resolve<UserController>();
            var playerController = scope.Resolve<PlayerController>();

            doRecover = false;
            await ImportLegacyData(connectionManager, userController);
            doRecover = true;
            playerController.ExperienceToggled += (bool enabled) => { isLive = enabled; };
            if (isLive)
            {
                playerController.EnableAwards(new User("Auto Recovery", ""));
                CrashAlert();
            }

            using (connectionManager.OpenConnection())
            {
                controllerManager.Initialize();
                twitchClient.Initialize();
            }

            commandManager.InitializeViews();
            commandManager.PushNotifications +=
                (User user, CommandResult commandResult) =>
                {
                    string message = "Push Notification";
                    commandResult.Sender = user;
                    commandManager.HandleResult(message, commandResult, ircClient, twitchClient);
                };

            var lastTime = DateTime.Now;
            await ircClient.Connect();

            while (true)
            {
                using (connectionManager.OpenConnection())
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
        }

        private static async Task Initialize(ClientData clientData, TokenData tokenData)
        {
            bool twitchPlays = false;
            using (var scope = CreateApplicationScope(clientData, tokenData))
            {
                var connectionManager = scope.Resolve<IConnectionManager>();
                var userController = scope.Resolve<UserController>();
                SeedDatabase(connectionManager, userController, tokenData);
                ConfigureLogging(connectionManager);
                using (connectionManager.OpenConnection())
                {
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

        private static async Task ConfigureDatabase(ClientData clientData, TokenData tokenData)
        {
            RestLogger.SetSensitiveData(clientData, tokenData);
            await UpdateDatabase(clientData, tokenData);
        }
    }
}