using Adventures;
using Autofac;
using Companions;
using GroupFinder;
using LobotJR.Command;
using LobotJR.Command.System;
using LobotJR.Command.System.Fishing;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Data.Import;
using LobotJR.Data.Migration;
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
using System.Text;
using System.Threading;
using Wolfcoins;

namespace TwitchBot
{

    public class Better
    {
        public int betAmount;
        public int vote;

        public Better()
        {
            betAmount = -1;
            vote = -1;
        }
    }

    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static bool isLive = false;
        public static bool hasCrashed = false;

        public static void CrashAlert()
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

        static void Main(string[] args)
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
                    RunBot(args);
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

        static void RunBot(string[] args)
        {
            bool twitchPlays = false;

            const int DUNGEON_MAX = 3;
            const int PARTY_FORMING = 1;
            const int PARTY_FULL = 2;
            const int PARTY_STARTED = 3;
            const int PARTY_COMPLETE = 4;
            const int PARTY_READY = 2;

            const int SUCCEED = 0;
            const int FAIL = 1;

            int gloatCost = 25;

            Dictionary<int, Equipment.LegacyItem> itemDatabase = new Dictionary<int, Equipment.LegacyItem>();
            Dictionary<int, Pet> petDatabase = new Dictionary<int, Pet>();

            var subathonPath = "C:/Users/Lobos/Dropbox/Stream/subathon.txt";
            IEnumerable<string> subathonFile;
            if (File.Exists(subathonPath))
            {
                subathonFile = File.ReadLines(subathonPath, UTF8Encoding.Default);
            }
            else
            {
                subathonFile = new List<string>();
                Logger.Warn("Failed to load subathon file, {file} not found.", subathonPath);
            }

            Dictionary<int, string> dungeonList = new Dictionary<int, string>();
            string dungeonListPath = "content/dungeonlist.ini";

            Dictionary<int, string> itemList = new Dictionary<int, string>();
            string itemListPath = "content/itemlist.ini";

            Dictionary<int, string> petList = new Dictionary<int, string>();
            string petListPath = "content/petlist.ini";

            Dictionary<int, Party> parties = new Dictionary<int, Party>();
            int maxPartyID = 0;

            GroupFinderQueue groupFinder;

            const int baseDungeonCost = 25;

            Dictionary<string, Better> betters = new Dictionary<string, Better>();
            bool betActive = false;
            bool betsAllowed = false;

            var clientData = FileUtils.ReadClientData();
            var tokenData = FileUtils.ReadTokenData();
            RestLogger.SetSensitiveData(clientData, tokenData);

            #region Database Update
            var updaterContainer = AutofacSetup.SetupUpdater(clientData, tokenData);
            var updaterScope = updaterContainer.BeginLifetimeScope();
            var updater = updaterScope.Resolve<SqliteDatabaseUpdater>();
            updater.Initialize();
            if (updater.CurrentVersion < updater.LatestVersion)
            {
                Logger.Info("Database is out of date, updating to {version}. This could take a few minutes.", updater.LatestVersion);
                var updateResult = updater.UpdateDatabase();
                if (!updateResult.Success)
                {
                    throw new Exception($"Error occurred updating database from {updateResult.PreviousVersion} to {updateResult.NewVersion}. {updateResult.DebugOutput}");
                }
                updater.WriteUpdatedVersion();
                Logger.Info("Update complete!");
            }
            updaterScope.Dispose();
            #endregion

            #region Sql Data Setup
            var container = AutofacSetup.Setup(clientData, tokenData);
            var scope = container.BeginLifetimeScope();
            var context = scope.Resolve<SqliteContext>();
            context.Initialize();
            var repoManager = scope.Resolve<IRepositoryManager>();
            var metadata = repoManager.Metadata.Read().FirstOrDefault();
            if (metadata == null)
            {
                metadata = new Metadata();
                repoManager.Metadata.Create(metadata);
                repoManager.Metadata.Commit();
            }
            var appSettings = repoManager.AppSettings.Read().FirstOrDefault();
            if (appSettings == null)
            {
                appSettings = new AppSettings();
                repoManager.AppSettings.Create(appSettings);
                repoManager.AppSettings.Commit();
            }
            var wolfcoins = scope.Resolve<Currency>();
            var contentManager = scope.Resolve<IContentManager>();
            var userSystem = scope.Resolve<UserSystem>();
            userSystem.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(appSettings.UserDatabaseUpdateTime);
            userSystem.SetBotUsers(userSystem.GetOrCreateUser(tokenData.BroadcastId, tokenData.BroadcastUser), userSystem.GetOrCreateUser(tokenData.ChatId, tokenData.ChatUser));
            var connectionManager = scope.Resolve<IConnectionManager>();
            #endregion

            #region Logging Setup
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
            #endregion

            #region Twitch Setup
            var twitchClient = scope.Resolve<ITwitchClient>();
            var ircClient = scope.Resolve<ITwitchIrcClient>();
            var connected = ircClient.Connect().GetAwaiter().GetResult();
            var attempts = 0;
            while (!connected)
            {
                Logger.Error($"IRC connection failed. Retrying in {Math.Pow(2, attempts)} seconds...");
                Thread.Sleep((int)Math.Pow(2, attempts) * 1000);
                ircClient.Restart();
                connected = ircClient.Connect().GetAwaiter().GetResult();
                attempts++;
            }
            Logger.Info($"Logged in as {tokenData.ChatUser}");
            attempts = 0;
            #endregion

            if (!twitchPlays)
            {
                #region NormalBot
                DateTime awardLast = DateTime.Now;

                #region System Setup
                var systemManager = scope.Resolve<ISystemManager>();
                #endregion

                #region Command Manager Setup
                var commandManager = scope.Resolve<ICommandManager>();
                commandManager.InitializeModules();
                commandManager.PushNotifications +=
                    (User user, CommandResult commandResult) =>
                    {
                        string message = "Push Notification";
                        commandManager.HandleCommandResult(message, commandResult, ircClient, twitchClient);
                    };
                #endregion

                #region Trigger Manager Setup
                var triggerManager = scope.Resolve<TriggerManager>();
                #endregion

                #region Import Legacy Data Into Sql
                using (var database = new SqliteRepositoryManager())
                {
                    DataImporter.ImportLegacyData(database, userSystem).GetAwaiter().GetResult();
                }
                #endregion

                groupFinder = new GroupFinderQueue(dungeonList);

                foreach (var member in wolfcoins.classList)
                {
                    member.Value.ClearQueue();
                }
                wolfcoins.SaveClassData();

                #region Crash Recovery
                if (isLive)
                {
                    broadcasting = true;
                    broadcastSetter = "Auto Recovery";
                    awardLast = DateTime.Now;
                    var tournamentSystem = systemManager.Get<TournamentSystem>();
                    tournamentSystem.NextTournament = DateTime.Now.AddMinutes(15);
                    isLive = true;
                    CrashAlert();
                }
                #endregion

                while (true)
                {
                    #region System Processing
                    systemManager.Process().GetAwaiter().GetResult();
                    twitchClient.ProcessQueue().GetAwaiter().GetResult();
                    #endregion

                    var ircMessages = ircClient.Process().GetAwaiter().GetResult();
                    var whispers = ircMessages.Where(x => x.IsWhisper);
                    var messages = ircMessages.Where(x => x.IsChat);
                    var subs = ircMessages.Where(x => x.IsUserNotice);
                    string whisperSender;
                    string whisperMessage;

                    List<int> partiesToRemove = new List<int>();

                    foreach (var party in parties)
                    {
                        if (party.Value.status == PARTY_STARTED && party.Value.myDungeon.messenger.messageQueue.Count() > 0)
                        {

                            if (party.Value.myDungeon.messenger.processQueue() == 1)
                            {
                                // grant rewards here
                                foreach (var member in party.Value.members)
                                {
                                    var memberUser = userSystem.GetUserByName(member.name);
                                    // if player had an active pet, lower its hunger and affection
                                    if (member.myPets.Count > 0)
                                    {
                                        bool petUpdated = false;
                                        foreach (var pet in wolfcoins.classList[member.name].myPets)
                                        {
                                            // if we updated the active pet already (should only be one), we're done
                                            if (petUpdated)
                                                break;

                                            // check for active pet
                                            if (pet.isActive)
                                            {
                                                Random RNG = new Random();
                                                int hungerToLose = RNG.Next(Pet.DUNGEON_HUNGER, Pet.DUNGEON_HUNGER + 6);

                                                pet.affection -= Pet.DUNGEON_AFFECTION;

                                                if (pet.hunger <= 0)
                                                {
                                                    // PET DIES HERE
                                                    twitchClient.QueueWhisper(memberUser, pet.name + " starved to death.");
                                                    wolfcoins.classList[member.name].releasePet(pet.stableID);
                                                    wolfcoins.SaveClassData();
                                                    break;
                                                }
                                                else if (pet.hunger <= 10)
                                                {
                                                    twitchClient.QueueWhisper(memberUser, pet.name + " is very hungry and will die if you don't feed it soon!");
                                                }
                                                else if (pet.hunger <= 25)
                                                {
                                                    twitchClient.QueueWhisper(memberUser, pet.name + " is hungry! Be sure to !feed them!");
                                                }



                                                if (pet.affection < 0)
                                                    pet.affection = 0;

                                                petUpdated = true;
                                            }

                                        }
                                    }

                                    if (member.xpEarned == 0 || member.coinsEarned == 0)
                                        continue;

                                    if ((member.xpEarned + member.coinsEarned) > 0 && member.usedGroupFinder && (DateTime.Now - member.lastDailyGroupFinder).TotalDays >= 1)
                                    {
                                        member.lastDailyGroupFinder = DateTime.Now;
                                        member.xpEarned *= 2;
                                        member.coinsEarned *= 2;
                                        twitchClient.QueueWhisper(memberUser, "You earned double rewards for completing a daily Group Finder dungeon! Queue up again in 24 hours to receive the 2x bonus again! (You can whisper me '!daily' for a status.)");
                                    }

                                    wolfcoins.AwardXP(member.xpEarned, memberUser, twitchClient);
                                    wolfcoins.AwardCoins(member.coinsEarned, member.name);
                                    if (member.xpEarned > 0 && member.coinsEarned > 0)
                                        twitchClient.QueueWhisper(memberUser, member.name + ", you've earned " + member.xpEarned + " XP and " + member.coinsEarned + " Wolfcoins for completing the dungeon!");

                                    if (wolfcoins.classList[member.name].itemEarned != -1)
                                    {
                                        int itemID = GrantItem(wolfcoins.classList[member.name].itemEarned, wolfcoins, member.name, itemDatabase);
                                        twitchClient.QueueWhisper(memberUser, "You looted " + itemDatabase[(itemID - 1)].itemName + "!");
                                    }
                                    // if a pet is waiting to be awarded
                                    if (wolfcoins.classList[member.name].petEarned != -1)
                                    {

                                        Dictionary<int, Pet> allPets = new Dictionary<int, Pet>(petDatabase);
                                        Pet newPet = GrantPet(member.name, wolfcoins, allPets, userSystem, ircClient, twitchClient);
                                        if (newPet.stableID != -1)
                                        {
                                            string logPath = "petlog.txt";
                                            string timestamp = DateTime.Now.ToString();
                                            if (newPet.isSparkly)
                                            {
                                                System.IO.File.AppendAllText(logPath, timestamp + ": " + member.name + " found a SPARKLY pet " + newPet.name + "." + Environment.NewLine);
                                            }
                                            else
                                            {
                                                System.IO.File.AppendAllText(logPath, timestamp + ": " + member.name + " found a pet " + newPet.name + "." + Environment.NewLine);
                                            }
                                        }
                                        //if (wolfcoins.classList[member.name].petEarned != -1)
                                        //{
                                        //    List<Pet> toAward = new List<Pet>();
                                        //    bool hasActivePet = false;
                                        //    // figure out the rarity of pet to give and build a list of non-duplicate pets to award
                                        //    int rarity = wolfcoins.classList[member.name].petEarned;
                                        //    foreach (var basePet in petDatabase)
                                        //    {
                                        //        if (basePet.Value.petRarity != rarity)
                                        //            continue;

                                        //        bool alreadyOwned = false;

                                        //        foreach(var pet in wolfcoins.classList[member.name].myPets)
                                        //        {
                                        //            if (pet.isActive)
                                        //                hasActivePet = true;

                                        //            if (pet.ID == basePet.Value.ID)
                                        //                alreadyOwned = true;
                                        //        }

                                        //        if(!alreadyOwned)
                                        //        {
                                        //            toAward.Add(basePet.Value);
                                        //        }
                                        //    }
                                        //    // now that we have a list of eligible pets, randomly choose one from the list to award
                                        //    Pet newPet = new Pet();

                                        //    if(toAward.Count > 0)
                                        //    {
                                        //        string toSend = "";
                                        //        Random RNG = new Random();
                                        //        int petToAward = RNG.Next(1, toAward.Count);
                                        //        newPet = toAward[petToAward - 1];
                                        //        int sparklyCheck = RNG.Next(1, 100);

                                        //        if (sparklyCheck == 1)
                                        //            newPet.isSparkly = true;

                                        //        newPet.stableID = wolfcoins.classList[member.name].myPets.Count;
                                        //        wolfcoins.classList[member.name].myPets.Count = wolfcoins.classList[member.name].myPets.Count;

                                        //        if (!hasActivePet)
                                        //        {
                                        //            newPet.isActive = true;
                                        //            toSend = "You found your first pet! You now have a pet " + newPet.name + ". Whisper me !pethelp for more info.";
                                        //        }
                                        //        else
                                        //        {
                                        //            toSend = "You found a new pet buddy! You earned a " + newPet.name + " pet!";
                                        //        }

                                        //        if(newPet.isSparkly)
                                        //        {
                                        //            toSend += " WOW! And it's a sparkly version! Luck you!";
                                        //        }

                                        //        wolfcoins.classList[member.name].myPets.Add(newPet);

                                        //        wolfcoins.classList[member.name].petEarned = -1;
                                        //        Whisper(member.name, toSend, group);
                                        //        if (newPet.isSparkly)
                                        //        {
                                        //            Console.WriteLine(DateTime.Now.ToString() + "WOW! " + ": " + member.name + " just found a SPARKLY pet " + newPet.name + "!");
                                        //            ircClient.QueueMessage("WOW! " + member.name + " just found a SPARKLY pet " + newPet.name + "! What luck!");
                                        //        }
                                        //        else
                                        //        {
                                        //            Console.WriteLine(DateTime.Now.ToString() + ": " + member.name + " just found a pet " + newPet.name + "!");
                                        //            ircClient.QueueMessage(member.name + " just found a pet " + newPet.name + "!");
                                        //        }
                                        //    }
                                        //}
                                    }

                                    if (wolfcoins.classList[member.name].queueDungeons.Count > 0)
                                        wolfcoins.classList[member.name].ClearQueue();

                                }

                                party.Value.PostDungeon(wolfcoins);
                                wolfcoins.SaveClassData();
                                wolfcoins.SaveXP();
                                wolfcoins.SaveCoins();
                                party.Value.status = PARTY_READY;

                                if (party.Value.usedDungeonFinder)
                                {
                                    partiesToRemove.Add(party.Key);
                                }
                            }
                        }
                    }

                    for (int i = 0; i < partiesToRemove.Count; i++)
                    {
                        int Key = partiesToRemove[i];
                        foreach (var member in parties[Key].members)
                        {
                            var memberUser = userSystem.GetUserByName(member.name);
                            twitchClient.QueueWhisper(memberUser, "You completed a group finder dungeon. Type !queue to join another group!");
                            wolfcoins.classList[member.name].groupID = -1;
                            wolfcoins.classList[member.name].numInvitesSent = 0;
                            wolfcoins.classList[member.name].isPartyLeader = false;
                            wolfcoins.classList[member.name].ClearQueue();
                        }
                        parties.Remove(Key);
                    }

                    #region userNoticeRegion
                    // This code updates subcounter.txt for Subathon. Problem is having to recode the modifier update when the goal is met
                    //var text = File.ReadAllText(@"C:\Users\Lobos\Dropbox\Stream\subcounter.txt");
                    //int count = int.Parse(text);
                    //count--;
                    //File.WriteAllText(@"C:\Users\Lobos\Dropbox\Stream\subcounter.txt", count.ToString());
                    foreach (var sub in subs)
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
                    #endregion

                    #region whisperRegion
                    foreach (var whisper in whispers)
                    {
                        if (!string.IsNullOrWhiteSpace(whisper.Message))
                        {

                            whisperSender = whisper.UserName;
                            whisperMessage = whisper.Message;
                            User whisperer;
                            using (connectionManager.OpenConnection())
                            {
                                whisperer = userSystem.GetOrCreateUser(whisper.UserId, whisper.UserName);

                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.determineLevel(whisperSender) >= 3 && wolfcoins.determineClass(whisperSender) == "INVALID CLASS" && !whisperMessage.StartsWith("c") && !whisperMessage.StartsWith("C"))
                                    {
                                        Logger.Debug(">>{user}: User has not picked their class.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "ATTENTION! You are high enough level to pick a class, but have not picked one yet! Whisper me one of the following to choose your class: ");
                                        twitchClient.QueueWhisper(whisperer, "'C1' (Warrior), 'C2' (Mage), 'C3' (Rogue), 'C4' (Ranger), or 'C5' (Cleric)");
                                    }
                                }
                                #region Command Module Processing
                                if (whisperMessage[0] == CommandManager.Prefix)
                                {
                                    var result = commandManager.ProcessMessage(whisperMessage.Substring(1), whisperer, true);
                                    if (result != null && result.Processed)
                                    {
                                        commandManager.HandleCommandResult(whisperMessage, result, ircClient, twitchClient);
                                        continue;
                                    }
                                }
                                #endregion
                            }

                            if (whisperMessage == "!testcrash" && (whisperSender == tokenData.BroadcastUser || whisperSender == tokenData.ChatUser))
                            {
                                throw new Exception($"Test crash initiated by {whisperSender} at {DateTime.Now.ToString("yyyyMMddTHHmmssfffZ")}");
                            }
                            else if (whisperMessage.StartsWith("!bug"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    string[] msgData = whisperMessage.Split(' ');
                                    if (msgData.Count() > 1)
                                    {
                                        string bugMessage = "";
                                        for (int i = 1; i < msgData.Count(); i++)
                                        {
                                            bugMessage += msgData[i] + " ";
                                        }

                                        string logPath = "bugreports.log";
                                        System.IO.File.AppendAllText(logPath, whisperSender + ": " + bugMessage + Environment.NewLine);
                                        System.IO.File.AppendAllText(logPath, "------------------------------------------" + Environment.NewLine);

                                        twitchClient.QueueWhisper(whisperer, "Bug report submitted.");
                                        twitchClient.QueueWhisper(userSystem.BroadcastUser, DateTime.Now + ": " + whisperSender + " submitted a bug report.");
                                        Logger.Info(">>{user}: A bug has been reported. {message}", whisperSender, bugMessage);
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Bug report failed, no report provided.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Bug report failed because user is not in wolfcoin collection.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!updatedungeons")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Updated dungeons command not authorized.", whisperSender);
                                    continue;
                                }

                                Logger.Debug(">>{user}: Update dungeons command executed.", whisperSender);
                                UpdateDungeons(dungeonListPath, ref dungeonList);
                            }
                            else if (whisperMessage.StartsWith("!queue"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender) && wolfcoins.classList[whisperSender].name != "NAMELESS ONE")
                                {
                                    if (whisperMessage == "!queuetime")
                                    {
                                        if (wolfcoins.classList[whisperSender].queueDungeons.Count == 0)
                                        {
                                            Logger.Debug(">>{user}: Queuestatus command failed as there are no queued dungeons.", whisperSender);
                                            continue;
                                        }

                                        string myQueuedDungeons = "You are queued for the following dungeons: ";
                                        bool firstAdded = false;
                                        foreach (var dung in wolfcoins.classList[whisperSender].queueDungeons)
                                        {
                                            if (!firstAdded)
                                            {
                                                firstAdded = true;
                                            }
                                            else
                                            {
                                                myQueuedDungeons += ",";
                                            }
                                            myQueuedDungeons += dung;
                                        }

                                        double timeSpan = ((DateTime.Now - wolfcoins.classList[whisperSender].queueTime)).TotalSeconds;
                                        double seconds = timeSpan % 60;
                                        seconds = Math.Truncate(seconds);
                                        double minutes = ((DateTime.Now - wolfcoins.classList[whisperSender].queueTime)).TotalMinutes % 60;
                                        minutes = Math.Truncate(minutes);
                                        if (minutes >= 60)
                                            minutes = minutes % 60;

                                        double hours = ((DateTime.Now - wolfcoins.classList[whisperSender].queueTime)).TotalHours;
                                        string timeMessage = "You've been waiting in the Group Finder queue for ";
                                        string lastFormed = "The last group was formed ";
                                        hours = Math.Truncate(hours);
                                        if (hours > 0)
                                            timeMessage += hours + " hours, ";

                                        if (minutes > 0)
                                            timeMessage += minutes + " minutes, and ";

                                        timeMessage += seconds + " seconds.";

                                        timeSpan = ((DateTime.Now - groupFinder.lastFormed).TotalSeconds);
                                        seconds = timeSpan % 60;
                                        seconds = Math.Truncate(seconds);
                                        minutes = timeSpan / 60;
                                        minutes = Math.Truncate(minutes);
                                        hours = minutes / 60;
                                        hours = Math.Truncate(hours);

                                        if (minutes >= 60)
                                            minutes = minutes % 60;

                                        if (hours > 0)
                                            lastFormed += hours + " hours, ";

                                        if (minutes > 0)
                                            lastFormed += minutes + " minutes, and ";

                                        lastFormed += seconds + " seconds ago.";

                                        Logger.Debug(">>{user}: Queuetime command executed.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, myQueuedDungeons);
                                        twitchClient.QueueWhisper(whisperer, timeMessage);
                                        twitchClient.QueueWhisper(whisperer, lastFormed);
                                        continue;
                                    }
                                    if (whisperMessage == "!queuestatus" && (whisperSender == tokenData.BroadcastUser || whisperSender == tokenData.ChatUser))
                                    {
                                        if (groupFinder.queue.Count == 0)
                                        {
                                            twitchClient.QueueWhisper(whisperer, "No players in queue.");
                                        }

                                        twitchClient.QueueWhisper(whisperer, groupFinder.queue.Count + " players in queue.");
                                        Dictionary<int, int> queueData = new Dictionary<int, int>();
                                        foreach (var player in groupFinder.queue)
                                        {
                                            foreach (var dungeonID in player.queueDungeons)
                                            {
                                                if (!queueData.ContainsKey(dungeonID))
                                                {
                                                    queueData.Add(dungeonID, 1);
                                                }
                                                else
                                                {
                                                    queueData[dungeonID]++;
                                                }
                                            }
                                        }

                                        foreach (var dataPoint in queueData)
                                        {
                                            twitchClient.QueueWhisper(whisperer, "Dungeon ID <" + dataPoint.Key + ">: " + dataPoint.Value + " players");
                                        }

                                        Logger.Debug(">>{user}: Queuestatus command executed.", whisperSender);
                                        continue;
                                    }

                                    if (wolfcoins.classList[whisperSender].queueDungeons.Count > 0)
                                    {
                                        Logger.Debug(">>{user}: Queue command failed as user is already queued.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You are already queued in the Group Finder! Type !queuetime for more information.");
                                        continue;
                                    }

                                    if (!wolfcoins.classList[whisperSender].pendingInvite && wolfcoins.classList[whisperSender].groupID == -1)
                                    {

                                        string[] msgData = whisperMessage.Split(' ');
                                        string[] tempDungeonData;
                                        bool didRequest = false;
                                        if (msgData.Count() > 1)
                                        {
                                            tempDungeonData = msgData[1].Split(',');
                                            didRequest = true;
                                        }
                                        else
                                        {
                                            tempDungeonData = GetEligibleDungeons(whisperSender, wolfcoins, dungeonList).Split(',');
                                        }
                                        List<int> requestedDungeons = new List<int>();
                                        //string[] tempDungeonData = msgData[1].Split(',');
                                        string errorMessage = "Unable to join queue. Reason(s): ";
                                        bool eligible = true;
                                        for (int i = 0; i < tempDungeonData.Count(); i++)
                                        {
                                            int tempInt = -1;
                                            int.TryParse(tempDungeonData[i], out tempInt);
                                            int eligibility = DetermineEligibility(whisperSender, tempInt, dungeonList, baseDungeonCost, wolfcoins);
                                            switch (eligibility)
                                            {
                                                case 0: // player not high enough level
                                                    {
                                                        eligible = false;
                                                        errorMessage += "Not appropriate level. (ID: " + tempInt + ") ";
                                                    }
                                                    break;

                                                case -1: // invalid dungeon id
                                                    {
                                                        eligible = false;
                                                        errorMessage += "Invalid Dungeon ID provided. (ID: " + tempInt + ") ";
                                                    }
                                                    break;

                                                case -2: // not enough money
                                                    {
                                                        eligible = false;
                                                        errorMessage += "You don't have enough money!";
                                                    }
                                                    break;
                                                case 1:
                                                    {
                                                        requestedDungeons.Add(tempInt);
                                                    }
                                                    break;

                                                default: break;
                                            }

                                            if (eligibility == -2)
                                                break;

                                            //if (tempInt != -1)
                                            //    requestedDungeons.Add(tempInt);

                                        }

                                        if (!eligible)
                                        {
                                            Logger.Debug(">>{user}: Queue command failed. {errorMessage}", whisperSender, errorMessage);
                                            twitchClient.QueueWhisper(whisperer, errorMessage);
                                            continue;
                                        }

                                        wolfcoins.classList[whisperSender].queuePriority = groupFinder.priority;
                                        groupFinder.priority++;
                                        wolfcoins.classList[whisperSender].usedGroupFinder = true;
                                        wolfcoins.classList[whisperSender].queueDungeons = requestedDungeons;

                                        Party myParty = groupFinder.Add(wolfcoins.classList[whisperSender]);
                                        if (myParty.members.Count != 3)
                                        {
                                            wolfcoins.classList[whisperSender].queueTime = DateTime.Now;
                                            Logger.Debug(">>{user}: Queue command executed.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You have been placed in the Group Finder queue.");
                                            continue;
                                        }

                                        myParty.members.ElementAt(0).isPartyLeader = true;
                                        myParty.partyLeader = myParty.members.ElementAt(0).name;
                                        myParty.status = PARTY_FULL;
                                        myParty.members.ElementAt(0).numInvitesSent = 3;
                                        myParty.myID = maxPartyID;
                                        myParty.usedDungeonFinder = true;

                                        int lowestNumOfDungeons = myParty.members.ElementAt(0).queueDungeons.Count;
                                        int pickiestMember = 0;
                                        int count = 0;

                                        // Pick the party member with the least available dungeons (to narrow down options)
                                        foreach (var member in myParty.members)
                                        {

                                            if (member.name == myParty.partyLeader)
                                                continue;

                                            count++;

                                            if (member.queueDungeons.Count < lowestNumOfDungeons)
                                            {
                                                pickiestMember = count;
                                                lowestNumOfDungeons = member.queueDungeons.Count;
                                            }

                                        }

                                        Random RNG = new Random();
                                        int numAvailableDungeons = myParty.members.ElementAt(pickiestMember).queueDungeons.Count();

                                        //choose a random dungeon out of the available options
                                        int randDungeon = RNG.Next(0, (numAvailableDungeons - 1));
                                        // set the id based on that random dungeon
                                        int dungeonID = (myParty.members.ElementAt(pickiestMember).queueDungeons.ElementAt(randDungeon)) - 1;
                                        dungeonID++;
                                        myParty.members.ElementAt(0).groupFinderDungeon = dungeonID;
                                        string dungeonName = GetDungeonName(dungeonID, dungeonList);
                                        string members = "Group Finder group created for " + dungeonName + ": ";
                                        foreach (var member in myParty.members)
                                        {
                                            member.groupID = maxPartyID;
                                            member.usedGroupFinder = true;
                                            members += member.name + ", " + member.className + "; ";
                                            string otherMembers = "";
                                            foreach (var player in myParty.members)
                                            {
                                                if (player.name == member.name)
                                                    continue;

                                                otherMembers += player.name + " (" + player.className + ") ";
                                            }

                                            userSystem.GetUserByNameAsync(member.name, (user) =>
                                            {
                                                twitchClient.QueueWhisper(user, "You've been matched for " + dungeonName + " with: " + otherMembers + ".");
                                                if (member.isPartyLeader)
                                                    twitchClient.QueueWhisper(user, "You are the party leader. Whisper me '!start' to begin!");
                                            });
                                        }
                                        parties.Add(maxPartyID, myParty);
                                        Logger.Info(members);
                                        maxPartyID++;

                                    }
                                    else if (wolfcoins.classList[whisperSender].isPartyLeader)
                                    {
                                        string reason = "";
                                        if (parties.ContainsKey(wolfcoins.classList[whisperSender].groupID))
                                        {
                                            switch (parties[wolfcoins.classList[whisperSender].groupID].status)
                                            {
                                                case PARTY_FORMING:
                                                    {
                                                        reason = "Party is currently forming. Add members with '!add <username>'";
                                                    }
                                                    break;

                                                case PARTY_READY:
                                                    {
                                                        reason = "Party is filled and ready to adventure! Type '!start' to begin!";
                                                    }
                                                    break;

                                                case PARTY_STARTED:
                                                    {
                                                        reason = "Your party is currently on an adventure!";
                                                    }
                                                    break;

                                                case PARTY_COMPLETE:
                                                    {
                                                        reason = "Your party just finished an adventure!";
                                                    }
                                                    break;

                                                default:
                                                    {
                                                        reason = "I have no idea the status of your party.";
                                                    }
                                                    break;
                                            }
                                        }
                                        Logger.Debug(">>{user}: Queue command failed as user already in a party.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You already have a party created! " + reason);
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Queue command failed as user has an outstanding invite.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You currently have an outstanding invite to another party. Couldn't create new party!");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Queue command failed as user not found in class list or has not selected a class.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!leavequeue")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].queueDungeons.Count > 0)
                                    {
                                        groupFinder.RemoveMember(whisperSender);
                                        wolfcoins.classList[whisperSender].ClearQueue();

                                        Logger.Debug(">>{user}: Leave queue command executed.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You were removed from the Group Finder.");
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Leave queue command failed as no dungeons are queued.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Leave queue command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if ((whisperMessage.StartsWith("!bet") || whisperMessage.StartsWith("bet")) && betsAllowed && betActive && wolfcoins.Exists(wolfcoins.coinList, whisperSender))
                            {
                                string[] whisperMSG = whisperMessage.Split();
                                if (whisperMSG.Length > 1)
                                {
                                    Better betInfo = new Better();
                                    string user = whisperSender;
                                    string vote = whisperMSG[1].ToLower();
                                    int betAmount = -1;
                                    if (int.TryParse(whisperMSG[2], out betAmount))
                                    {
                                        if (!wolfcoins.CheckCoins(user, betAmount))
                                        {
                                            Logger.Debug(">>{user}: Bet command failed due to insufficient coins. Expected {amount}, has {coins}.", whisperSender, betAmount, wolfcoins.coinList[user]);
                                            userSystem.GetUserByNameAsync(user, (userObj) => { twitchClient.QueueWhisper(userObj, "There was an error placing your bet. (not enough coins?)"); });
                                            continue;
                                        }
                                        betInfo.betAmount = betAmount;
                                        if (!betters.ContainsKey(user))
                                        {
                                            wolfcoins.RemoveCoins(user, betAmount);
                                            if (vote == "succeed")
                                            {
                                                Logger.Debug(">>{user}: Bet placed on \"succeed\".", whisperSender);
                                                betInfo.vote = SUCCEED;
                                                betters.Add(user, betInfo);

                                            }
                                            else if (vote == "fail")
                                            {
                                                Logger.Debug(">>{user}: Bet placed on \"fail\".", whisperSender);
                                                betInfo.vote = FAIL;
                                                betters.Add(user, betInfo);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Bet command failed as user has already placed a bet.", whisperSender);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Bet command failed due to parameter {parameter} failed to parse as int.", whisperSender, whisperMSG[2]);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Bet command failed due to missing parameter.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!shutdown"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Shutdown command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] temp = whisperMessage.Split(' ');
                                if (temp.Count() > 1)
                                {
                                    int numMinutes = -1;
                                    if (int.TryParse(temp[1], out numMinutes))
                                    {
                                        var toNotify = userSystem.GetUsersByNames(groupFinder.queue.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                        twitchClient.QueueWhisper(toNotify, "Attention! Wolfpack RPG will be coming down for maintenance in about " + numMinutes + " minutes. If you are dungeoning while the bot shuts down, your progress may not be saved.");
                                        Logger.Debug(">>{user}: Shutdown command executed. Maintenance scheduled for {minutes} minutes.", whisperSender, numMinutes);
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Shutdown command failed as parameter {parameter} failed to parse as int.", whisperSender, temp[1]);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Shutdown command failed due to missing parameter.", whisperSender);
                                }
                            }
                        }
                    }
                    #endregion
                    #region messageRegion
                    foreach (var message in messages)
                    {
                        if (!string.IsNullOrWhiteSpace(message.Message))
                        {
                            using (connectionManager.OpenConnection())
                            {
                                var chatter = userSystem.GetOrCreateUser(message.UserId, message.UserName);
                                userSystem.UpdateUser(chatter, message);

                                var triggerResult = triggerManager.ProcessTrigger(message.Message, chatter);
                                if (triggerResult != null && triggerResult.Processed)
                                {
                                    if (triggerResult.Messages != null)
                                    {
                                        foreach (var responseMessage in triggerResult.Messages)
                                        {
                                            ircClient.QueueMessage(responseMessage);
                                        }
                                    }
                                    if (triggerResult.Whispers != null)
                                    {
                                        foreach (var triggerWhisper in triggerResult.Whispers)
                                        {
                                            twitchClient.QueueWhisper(chatter, triggerWhisper);
                                        }
                                    }
                                    if (triggerResult.TimeoutSender)
                                    {
                                        twitchClient.Timeout(chatter, 1, triggerResult.TimeoutMessage);
                                    }
                                    continue;
                                }
                            }
                        }
                    }
                    #endregion
                }

                Logger.Info("Connection terminated.");
                twitchClient.RefreshTokens().GetAwaiter().GetResult();
                connected = false;
                #endregion
            }
            else
            {
                new ChatController().Play(ircClient);
            }
        }

        static Pet GrantPet(string playerName, Currency wolfcoins, Dictionary<int, Pet> petDatabase, UserSystem userSystem, ITwitchIrcClient irc, ITwitchClient twitchClient)
        {
            List<Pet> toAward = new List<Pet>();
            // figure out the rarity of pet to give and build a list of non-duplicate pets to award
            int rarity = wolfcoins.classList[playerName].petEarned;
            foreach (var basePet in petDatabase)
            {
                if (basePet.Value.petRarity != rarity)
                    continue;

                bool alreadyOwned = false;


                foreach (var pet in wolfcoins.classList[playerName].myPets)
                {
                    if (pet.ID == basePet.Value.ID)
                    {
                        alreadyOwned = true;
                        break;
                    }

                }

                if (!alreadyOwned)
                {
                    toAward.Add(basePet.Value);
                }
            }
            // now that we have a list of eligible pets, randomly choose one from the list to award
            Pet newPet;

            if (toAward.Count > 0)
            {
                string toSend = "";
                Random RNG = new Random();
                int petToAward = RNG.Next(1, toAward.Count + 1);
                newPet = new Pet(toAward[petToAward - 1]);
                int sparklyCheck = RNG.Next(1, 101);
                bool firstPet = false;
                if (wolfcoins.classList[playerName].myPets.Count == 0)
                    firstPet = true;

                if (sparklyCheck == 1)
                    newPet.isSparkly = true;

                if (firstPet)
                {
                    newPet.isActive = true;
                    toSend = "You found your first pet! You now have a pet " + newPet.type + ". Whisper me !pethelp for more info.";
                }
                else
                {
                    toSend = "You found a new pet buddy! You earned a " + newPet.type + " pet!";
                }

                if (newPet.isSparkly)
                {
                    toSend += " WOW! And it's a sparkly version! Luck you!";
                }

                newPet.stableID = wolfcoins.classList[playerName].myPets.Count + 1;
                wolfcoins.classList[playerName].myPets.Add(newPet);



                userSystem.GetUserByNameAsync(playerName, (user) =>
                {
                    twitchClient.QueueWhisper(user, toSend);
                    if (newPet.isSparkly)
                    {
                        Logger.Info("WOW! {user} just found a SPARKLY pet {pet}", playerName, newPet.name);
                        irc.QueueMessage("WOW! " + playerName + " just found a SPARKLY pet " + newPet.name + "! What luck!");
                    }
                    else
                    {
                        Logger.Info("{user} just found a pet {pet}!", playerName, newPet.name);
                        irc.QueueMessage(playerName + " just found a pet " + newPet.name + "!");
                    }

                    if (wolfcoins.classList[playerName].myPets.Count == petDatabase.Count)
                    {
                        twitchClient.QueueWhisper(user, "You've collected all of the available pets! Congratulations!");
                    }
                });

                wolfcoins.classList[playerName].petEarned = -1;

                return newPet;
            }

            return new Pet();

        }

        static string GetDungeonName(int dungeonID, Dictionary<int, string> dungeonList)
        {
            if (!dungeonList.ContainsKey(dungeonID))
                return "Invalid DungeonID";

            Dungeon tempDungeon = new Dungeon("content/dungeons/" + dungeonList[dungeonID]);
            return tempDungeon.dungeonName;
        }

        static string GetEligibleDungeons(string user, Currency wolfcoins, Dictionary<int, string> dungeonList)
        {
            string eligibleDungeons = "";
            int playerLevel = wolfcoins.determineLevel(user);
            List<Dungeon> dungeons = new List<Dungeon>();
            Dungeon tempDungeon;
            foreach (var id in dungeonList)
            {
                tempDungeon = new Dungeon("content/dungeons/" + dungeonList[id.Key]);
                tempDungeon.dungeonID = id.Key;
                dungeons.Add(tempDungeon);
            }

            if (dungeons.Count == 0)
                return eligibleDungeons;

            bool firstAdded = false;
            foreach (var dungeon in dungeons)
            {
                //if(dungeon.minLevel <= playerLevel)
                //{
                if (!firstAdded)
                {
                    firstAdded = true;
                }
                else
                {
                    eligibleDungeons += ",";
                }
                eligibleDungeons += dungeon.dungeonID;


                //}
            }
            return eligibleDungeons;
        }

        static int DetermineEligibility(string user, int dungeonID, Dictionary<int, string> dungeonList, int baseDungeonCost, Currency wolfcoins)
        {
            if (!dungeonList.ContainsKey(dungeonID))
                return -1;

            int playerLevel = wolfcoins.determineLevel(wolfcoins.xpList[user]);
            Dungeon tempDungeon = new Dungeon("content/dungeons/" + dungeonList[dungeonID]);

            if (wolfcoins.Exists(wolfcoins.coinList, user))
            {
                if (wolfcoins.coinList[user] < (baseDungeonCost + ((playerLevel - 3) * 10)))
                {
                    //not enough money
                    return -2;
                }
            }
            // no longer gate dungeons by level
            //if (tempDungeon.minLevel <= playerLevel)
            //    return 1;

            return 1;
        }

        static int GrantItem(int id, Currency wolfcoins, string user, Dictionary<int, Equipment.LegacyItem> itemDatabase)
        {
            string logPath = "dungeonlog.txt";

            if (id < 1)
                return -1;

            Equipment.LegacyItem newItem = itemDatabase[id - 1];
            bool hasActiveItem = false;
            foreach (var item in wolfcoins.classList[user].myItems)
            {
                if (item.itemType == newItem.itemType && item.isActive)
                    hasActiveItem = true;
            }
            if (!hasActiveItem)
                newItem.isActive = true;

            wolfcoins.classList[user].totalItemCount++;
            newItem.inventoryID = wolfcoins.classList[user].totalItemCount;
            wolfcoins.classList[user].myItems.Add(newItem);
            wolfcoins.classList[user].itemEarned = -1;

            System.IO.File.AppendAllText(logPath, user + " looted a " + newItem.itemName + "!" + Environment.NewLine);
            Logger.Info("{user} just looted a {item}!", user, newItem.itemName);

            return newItem.itemID;
        }
    }
}