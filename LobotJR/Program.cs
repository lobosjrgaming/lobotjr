﻿using Adventures;
using Autofac;
using Classes;
using Companions;
using Equipment;
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

        static void UpdateDungeons(string dungeonListPath, ref Dictionary<int, string> dungeonList)
        {
            IEnumerable<string> fileText;
            if (File.Exists(dungeonListPath))
            {
                fileText = File.ReadLines(dungeonListPath, UTF8Encoding.Default);
            }
            else
            {
                fileText = new List<string>();
                Logger.Error("Failed to load dungeon list file, {file} not found.", dungeonListPath);
            }

            dungeonList = new Dictionary<int, string>();
            int dungeonIter = 1;
            foreach (var line in fileText)
            {
                string[] temp = line.Split(',');
                int id = -1;
                int.TryParse(temp[0], out id);
                if (id != -1)
                    dungeonList.Add(id, temp[1]);
                else
                    Logger.Error("Invalid dungeon read on line {line}", dungeonIter);
                dungeonIter++;
            }
        }

        static void UpdateItems(string itemListPath, ref Dictionary<int, string> itemList, ref Dictionary<int, Item> itemDatabase)
        {
            IEnumerable<string> fileText;
            if (File.Exists(itemListPath))
            {
                fileText = File.ReadLines(itemListPath, UTF8Encoding.Default);
            }
            else
            {
                fileText = new List<string>();
                Logger.Error("Failed to load item list file, {file} not found.", itemListPath);
            }
            itemDatabase = new Dictionary<int, Item>();
            itemList = new Dictionary<int, string>();
            int itemIter = 1;
            // ALERT: Was there a reason you were loading this from the file twice?
            // fileText = System.IO.File.ReadLines(itemListPath, UTF8Encoding.Default);
            foreach (var line in fileText)
            {
                string[] temp = line.Split(',');
                int id = -1;
                int.TryParse(temp[0], out id);
                if (id != -1)
                    itemList.Add(id, "content/items/" + temp[1]);
                else
                    Logger.Error("Invalid item read on line {line}", itemIter);
                itemIter++;
            }

            itemIter = 0;
            foreach (var item in itemList)
            {
                Item myItem = new Item();
                int parsedInt = -1;
                int line = 0;
                string[] temp = { "" };
                fileText = System.IO.File.ReadLines(itemList.ElementAt(itemIter).Value, UTF8Encoding.Default);
                // item ID
                myItem.itemID = itemList.ElementAt(itemIter).Key;
                // item name
                temp = fileText.ElementAt(line).Split('=');
                myItem.itemName = temp[1];
                line++;
                // item type (1=weapon, 2=armor, 3=other)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.itemType = parsedInt;
                line++;
                // Class designation (1=Warrior,2=Mage,3=Rogue,4=Ranger,5=Cleric)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.forClass = parsedInt;
                line++;
                // Item rarity (1=Uncommon,2=Rare,3=Epic,4=Artifact)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.itemRarity = parsedInt;
                line++;
                // success boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.successChance = parsedInt;
                line++;
                // item find (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.itemFind = parsedInt;
                line++;
                // coin boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.coinBonus = parsedInt;
                line++;
                // xp boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.xpBonus = parsedInt;
                line++;
                // prevent death boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                myItem.preventDeathBonus = parsedInt;
                line++;
                // item description
                temp = fileText.ElementAt(line).Split('=');
                myItem.description = temp[1];

                itemDatabase.Add(itemIter, myItem);

                itemIter++;
            }

        }

        static void UpdatePets(string petListPath, ref Dictionary<int, string> petList, ref Dictionary<int, Pet> petDatabase)
        {
            IEnumerable<string> fileText;
            if (File.Exists(petListPath))
            {
                fileText = File.ReadLines(petListPath, UTF8Encoding.Default);
            }
            else
            {
                fileText = new List<string>();
                Logger.Error("Failed to load item list file, {file} not found.", petListPath);
            }
            petDatabase = new Dictionary<int, Pet>();
            petList = new Dictionary<int, string>();
            int petIter = 1;
            foreach (var line in fileText)
            {
                string[] temp = line.Split(',');
                int id = -1;
                int.TryParse(temp[0], out id);
                if (id != -1)
                    petList.Add(id, "content/pets/" + temp[1]);
                else
                    Logger.Error("Invalid pet read on line {line}", petIter);
                petIter++;
            }

            petIter = 0;
            foreach (var pet in petList)
            {
                Pet mypet = new Pet();
                int parsedInt = -1;
                int line = 0;
                string[] temp = { "" };
                fileText = System.IO.File.ReadLines(petList.ElementAt(petIter).Value, UTF8Encoding.Default);
                // pet ID
                mypet.ID = petList.ElementAt(petIter).Key;
                // pet name
                temp = fileText.ElementAt(line).Split('=');
                mypet.name = temp[1];
                line++;
                // pet type (string ex: Fox, Cat, Dog)
                temp = fileText.ElementAt(line).Split('=');
                mypet.type = temp[1];
                line++;
                // pet size (string ex: tiny, small, large)
                temp = fileText.ElementAt(line).Split('=');
                mypet.size = temp[1];
                line++;
                // pet rarity (1=Common,2=Uncommon,3=Rare,4=Epic,5=Artifact)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                mypet.petRarity = parsedInt;
                line++;
                // pet description
                temp = fileText.ElementAt(line).Split('=');
                mypet.description = temp[1];
                line++;
                // pet emote
                temp = fileText.ElementAt(line).Split('=');
                mypet.emote = temp[1];
                line++;
                int numLines = fileText.Count();
                if (numLines <= line)
                {
                    petDatabase.Add(petIter, mypet);
                    petIter++;
                    continue;
                }
                // success boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                mypet.successChance = parsedInt;
                line++;
                // item find (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                mypet.itemFind = parsedInt;
                line++;
                // coin boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                mypet.coinBonus = parsedInt;
                line++;
                // xp boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                mypet.xpBonus = parsedInt;
                line++;
                // prevent death boost (%)
                temp = fileText.ElementAt(line).Split('=');
                int.TryParse(temp[1], out parsedInt);
                mypet.preventDeathBonus = parsedInt;
                line++;


                petDatabase.Add(petIter, mypet);

                petIter++;
            }

        }

        public static bool isLive = false;
        public static bool hasCrashed = false;

        public static void CrashAlert()
        {
            if (isLive)
            {
                if (hasCrashed)
                {
                    using (var player = new SoundPlayer("./Resources/alert.wav"))
                    {
                        player.PlaySync();
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
                    Directory.CreateDirectory(folder);
                    File.Copy("./output.log", $"{folder}/output.log");
                    ZipFile.CreateFromDirectory(folder, $"{folder}.zip");
                    File.Delete($"{folder}/output.log");
                    Directory.Delete(folder);
                    Logger.Error(ex);
                    Logger.Error("The application has encountered an unexpected error: {message}", ex.Message);
                    Logger.Error("The full details of the error can be found in {file}", $"{folder}.zip");
                    hasCrashed = true;
                    CrashAlert();
                }
            }
        }

        static void RunBot(string[] args)
        {
            bool twitchPlays = false;
            bool broadcasting = false;
            string broadcastSetter = "";

            // How often to award Wolfcoins in minutes
            const int DUNGEON_MAX = 3;
            const int PARTY_FORMING = 1;
            const int PARTY_FULL = 2;
            const int PARTY_STARTED = 3;
            const int PARTY_COMPLETE = 4;
            const int PARTY_READY = 2;

            const int SUCCEED = 0;
            const int FAIL = 1;

            const int LOW_DETAIL = 0;
            const int HIGH_DETAIL = 1;

            int subCounter = 0;
            int awardMultiplier = 1;
            int awardInterval = 30;
            int awardAmount = 1;
            int awardTotal = 0;
            int gloatCost = 25;
            int pryCost = 1;

            Dictionary<int, Item> itemDatabase = new Dictionary<int, Item>();
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
            //const int baseRaidCost = 150;
            const int baseRespecCost = 250;

            Dictionary<string, Better> betters = new Dictionary<string, Better>();
            //string betStatement = "";
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
                if (File.Exists(FishDataImport.FishDataPath))
                {
                    Logger.Info("Detected legacy fish data file, migrating to SQLite.");
                    FishDataImport.ImportFishDataIntoSql(FishDataImport.FishDataPath, contentManager.FishData);
                    File.Move(FishDataImport.FishDataPath, $"{FishDataImport.FishDataPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                    Logger.Info("Fish data migration complete!");
                }

                var hasFisherData = File.Exists(FisherDataImport.FisherDataPath);
                var hasLeaderboardData = File.Exists(FisherDataImport.FishingLeaderboardPath);
                if (hasFisherData || hasLeaderboardData)
                {
                    Logger.Info("Detected legacy fisher data file, migrating to SQLite. This could take a few minutes.");
                    IEnumerable<string> users = new List<string>();
                    Dictionary<string, LegacyFisher> legacyFisherData = FisherDataImport.LoadLegacyFisherData(FisherDataImport.FisherDataPath);
                    List<LegacyCatch> legacyLeaderboardData = FisherDataImport.LoadLegacyFishingLeaderboardData(FisherDataImport.FishingLeaderboardPath);
                    Logger.Info("Converting usernames to user ids...");
                    FisherDataImport.FetchUserIds(legacyFisherData.Keys.Union(legacyLeaderboardData.Select(x => x.caughtBy)), userSystem, tokenData.BroadcastToken, clientData);
                    if (hasFisherData)
                    {
                        Logger.Info("Importing user records...");
                        FisherDataImport.ImportFisherDataIntoSql(legacyFisherData, contentManager.FishData, repoManager.Catches, userSystem);
                        File.Move(FisherDataImport.FisherDataPath, $"{FisherDataImport.FisherDataPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                    }
                    if (hasLeaderboardData)
                    {
                        Logger.Info("Importing leaderboard...");
                        FisherDataImport.ImportLeaderboardDataIntoSql(legacyLeaderboardData, repoManager.FishingLeaderboard, contentManager.FishData, userSystem);
                        File.Move(FisherDataImport.FishingLeaderboardPath, $"{FisherDataImport.FishingLeaderboardPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                    }
                    Logger.Info("Fisher data migration complete!");
                }
                #endregion

                UpdateDungeons(dungeonListPath, ref dungeonList);

                UpdateItems(itemListPath, ref itemList, ref itemDatabase);
                UpdatePets(petListPath, ref petList, ref petDatabase);
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
                    systemManager.Process(broadcasting).GetAwaiter().GetResult();
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

                    if (((DateTime.Now - awardLast).TotalMinutes > awardInterval))
                    {
                        if (broadcasting)
                        {
                            awardTotal = awardAmount * awardMultiplier;

                            // Halloween Treats
                            //Random rnd = new Random();
                            //int numViewers = wolfcoins.viewers.chatters.viewers.Count;
                            //int winner = rnd.Next(0, (numViewers - 1));
                            //string winnerName = wolfcoins.viewers.chatters.viewers.ElementAt(winner);
                            //int coinsToAward = (rnd.Next(5, 10)) * 50;
                            //wolfcoins.AddCoins(winnerName, coinsToAward.ToString());

                            wolfcoins.AwardCoins(awardTotal * 3, userSystem.Viewers); // Give 3x as many coins as XP
                            wolfcoins.AwardXP(awardTotal, userSystem.Viewers, twitchClient);
                            //string path2 = "C:/Users/Lobos/AppData/Roaming/DarkSoulsII/01100001004801af/`s" + DateTime.Now.Ticks + ".sl2";
                            //File.Copy(@"C:\Users\Lobos\AppData\Roaming\DarkSoulsII\01100001004801af\DS2SOFS0000.sl2", @path2);
                            //string path = "C:/Users/Lobos/AppData/Roaming/DarkSoulsIII/01100001004801af/Backups/DS30000_" + DateTime.Now.Ticks + ".sl2";
                            //File.Copy(@"C:/Users/Lobos/AppData/Roaming/DarkSoulsIII/01100001004801af/DS30000.sl2", @path);
                            ircClient.QueueMessage($"Thanks for watching! Viewers awarded {awardTotal} XP & {awardTotal * 3} Wolfcoins. Subscribers earn double that amount!");
                            //ircClient.QueueMessage("Happy Halloween! Viewer " + winnerName + " just won a treat of " + coinsToAward + " wolfcoins!");
                        }

                        wolfcoins.SaveCoins();
                        wolfcoins.SaveXP();
                        wolfcoins.SaveClassData();
                        awardLast = DateTime.Now;
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
                            var whisperer = userSystem.GetOrCreateUser(whisper.UserId, whisper.UserName);
                            // TODO: Need to add user system here, with a get/add user based on the whisper sender

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

                            if (whisperMessage == "?" || whisperMessage == "help" || whisperMessage == "!help" || whisperMessage == "faq" || whisperMessage == "!faq")
                            {
                                Logger.Debug(">>{user}: Help message sent.", whisperSender);
                                twitchClient.QueueWhisper(whisperer, "Hi I'm LobotJR! I'm a chat bot written by LobosJR to help out with things.  To ask me about a certain topic, whisper me the number next to what you want to know about! (Ex: Whisper me 1 for information on Wolfcoins)");
                                twitchClient.QueueWhisper(whisperer, "Here's a list of things you can ask me about: Wolfcoins (1) - Leveling System (2)");

                            }
                            else if (whisperMessage == "!testcrash" && (whisperSender == tokenData.BroadcastUser || whisperSender == tokenData.ChatUser || whisperSender.Equals("celesteenfer", StringComparison.OrdinalIgnoreCase)))
                            {
                                throw new Exception($"Test crash initiated by {whisperSender} at {DateTime.Now.ToString("yyyyMMddTHHmmssfffZ")}");
                            }
                            else if (whisperMessage == "!cleartesters" && (whisperSender == tokenData.BroadcastUser || whisperSender == tokenData.ChatUser))
                            {
                                //string[] users = { "lobosjr", "spectrumknight", "floogoss", "shoumpaloumpa", "nemesis_of_green", "donotgogently", "twitchmage", "kidgreen4", "cuddling", "androsv", "jaranous94", "lambchop2559", "hockeyboy1257", "dumj00", "stennisberetheon", "bionicmeech", "blargh201", "arampizzatime"};
                                //for(int i = 0; i < users.Length; i++)
                                //{
                                //    if (wolfcoins.Exists(wolfcoins.classList, users[i]))
                                //    {
                                //        wolfcoins.classList.Remove(users[i]);
                                //        wolfcoins.SetXP(1, users[i], group);
                                //        wolfcoins.SetXP(600, users[i], group);
                                //    }
                                //    else
                                //    {
                                //        wolfcoins.SetXP(1, users[i], group);
                                //        wolfcoins.SetXP(600, users[i], group);
                                //    }
                                //}

                            }
                            else if (whisperMessage.StartsWith("!dungeon"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    string[] msgData = whisperMessage.Split(' ');
                                    int dungeonID = -1;
                                    if (msgData.Count() > 1)
                                    {
                                        int.TryParse(msgData[1], out dungeonID);
                                        if (dungeonID != -1 && dungeonList.ContainsKey(dungeonID))
                                        {
                                            string dungeonPath = "content/dungeons/" + dungeonList[dungeonID];
                                            Dungeon tempDungeon = new Dungeon(dungeonPath);
                                            Logger.Debug(">>{user}: Dungeon data requested for dungeon id {id}.", whisperSender, dungeonID);
                                            twitchClient.QueueWhisper(whisperer, tempDungeon.dungeonName + " (Levels " + tempDungeon.minLevel + " - " + tempDungeon.maxLevel + ") -- " + tempDungeon.description);
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Dungeon data request failed, dungeon id {id} not found.", whisperSender, msgData[1]);
                                            twitchClient.QueueWhisper(whisperer, "Invalid Dungeon ID provided.");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Dungeon data request failed, dungeon id not provided.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Failed to send dungeon data because user is not in wolfcoin collection.", whisperSender);
                                }
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
                            else if (whisperMessage.StartsWith("!item"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myItems.Count == 0)
                                    {
                                        Logger.Debug(">>{user}: Item description request failed, user has no items.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You have no items.");
                                        continue;
                                    }

                                    string[] msgData = whisperMessage.Split(' ');
                                    int invID = -1;
                                    if (msgData.Count() > 1)
                                    {
                                        int.TryParse(msgData[1], out invID);
                                        if (invID != -1)
                                        {
                                            bool itemFound = false;
                                            foreach (var item in wolfcoins.classList[whisperSender].myItems)
                                            {
                                                if (item.inventoryID == invID)
                                                {
                                                    string desc = itemDatabase[item.itemID - 1].description;
                                                    string name = itemDatabase[item.itemID - 1].itemName;
                                                    Logger.Debug(">>{user}: Item description for item {id} sent.", whisperSender, invID);
                                                    twitchClient.QueueWhisper(whisperer, name + " -- " + desc);
                                                    itemFound = true;
                                                    break;
                                                }
                                            }
                                            if (!itemFound)
                                            {
                                                Logger.Debug(">>{user}: Item description failed, invalid inventory id {id}.", whisperSender, invID);
                                                twitchClient.QueueWhisper(whisperer, "Invalid Inventory ID provided.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Item description failed, inventory id {id} failed to parse.", whisperSender, msgData[1]);
                                            twitchClient.QueueWhisper(whisperer, "Invalid Inventory ID provided.");
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Item description failed because user is not in wolfcoin collection.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!updateviewers")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Update viewers command not authorized.", whisperSender);
                                    continue;
                                }

                                Logger.Debug(">>{user}: Update viewers command executed.", whisperSender);
                                userSystem.LastUpdate = userSystem.LastUpdate - TimeSpan.FromMinutes(appSettings.UserDatabaseUpdateTime + 1);
                                userSystem.Process(true).GetAwaiter().GetResult();
                            }
                            else if (whisperMessage == "!updateitems")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Update items command not authorized.", whisperSender);
                                    continue;
                                }

                                UpdateItems(itemListPath, ref itemList, ref itemDatabase);


                                foreach (var player in wolfcoins.classList)
                                {
                                    if (player.Value.myItems.Count == 0)
                                        continue;

                                    foreach (var item in player.Value.myItems)
                                    {
                                        Item newItem = itemDatabase[item.itemID - 1];

                                        item.itemName = newItem.itemName;
                                        item.itemRarity = newItem.itemRarity;
                                        item.itemFind = newItem.itemFind;
                                        item.successChance = newItem.successChance;
                                        item.coinBonus = newItem.coinBonus;
                                        item.preventDeathBonus = newItem.preventDeathBonus;
                                        item.xpBonus = newItem.xpBonus;
                                    }
                                }
                                Logger.Debug(">>{user}: Update items command executed.", whisperSender);
                            }
                            else if (whisperMessage == "!godmode")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: God mode command not authorized.", whisperSender);
                                    continue;
                                }

                                Logger.Debug(">>{user}: God mode command executed.", whisperSender);
                                wolfcoins.classList[whisperSender].successChance = 1000;

                            }
                            else if (whisperMessage.StartsWith("!addplayer"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Add player command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] msgData = whisperMessage.Split(' ');
                                if (msgData.Count() == 2)
                                {
                                    string name = msgData[1];
                                    string toSend = "";
                                    if (!wolfcoins.classList.ContainsKey(name))
                                    {
                                        wolfcoins.classList.Add(name.ToLower(), new CharClass());
                                        toSend += "class, ";
                                    }

                                    if (!wolfcoins.coinList.ContainsKey(name))
                                    {
                                        wolfcoins.coinList.Add(name, 0);
                                        toSend += "coin, ";
                                    }

                                    if (!wolfcoins.xpList.ContainsKey(name))
                                    {
                                        wolfcoins.xpList.Add(name, 0);
                                        toSend += "xp";
                                    }

                                    Logger.Debug(">>{user}: Add player command executed. {playerData}", whisperSender, toSend);
                                    twitchClient.QueueWhisper(userSystem.BroadcastUser, name + " added to the following lists: " + toSend);
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Add player command failed due to invalid parameter count. Expected 2, actual {count}", whisperSender, msgData.Length);
                                }
                            }
                            else if (whisperMessage.StartsWith("!transfer"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Transfer command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] msgData = whisperMessage.Split(' ');
                                if (msgData.Count() > 2 && msgData.Count() < 4)
                                {
                                    string prevName = msgData[1];
                                    string newName = msgData[2];

                                    if (!wolfcoins.coinList.ContainsKey(prevName) || !wolfcoins.xpList.ContainsKey(prevName))
                                    {
                                        twitchClient.QueueWhisper(whisperer, prevName + " has no stats to transfer.");
                                        continue;
                                    }

                                    if (!wolfcoins.coinList.ContainsKey(newName))
                                    {
                                        wolfcoins.coinList.Add(newName, 0);
                                    }

                                    if (!wolfcoins.xpList.ContainsKey(newName))
                                    {
                                        wolfcoins.xpList.Add(newName, 0);
                                    }

                                    int prevCoins = wolfcoins.coinList[prevName];
                                    int prevXP = wolfcoins.xpList[prevName];

                                    wolfcoins.coinList[newName] += prevCoins;
                                    wolfcoins.xpList[newName] += prevXP;

                                    if (!wolfcoins.classList.ContainsKey(newName))
                                    {
                                        CharClass playerClass = new CharClass();
                                        wolfcoins.classList.Add(newName.ToLower(), new CharClass());
                                    }

                                    twitchClient.QueueWhisper(whisperer, "Transferred " + prevName + "'s xp/coins to " + newName + ".");
                                    userSystem.GetUserByNameAsync(newName, (user) => { twitchClient.QueueWhisper(user, "Your xp/coin total has been updated by Lobos! Thanks for playing the RPG lobosHi"); });

                                    wolfcoins.SaveCoins();
                                    wolfcoins.SaveXP();

                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Transfer command failed due to invalid parameter count. Expected 2, actual {count}.", whisperSender, msgData.Length - 1);
                                }
                            }
                            else if (whisperMessage.StartsWith("!checkpets"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Check pets command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] msgData = whisperMessage.Split(' ');
                                if (msgData.Count() > 1)
                                {
                                    string toCheck = msgData[1];
                                    foreach (var pet in wolfcoins.classList[toCheck].myPets)
                                    {
                                        WhisperPet(whisperSender, pet, userSystem, twitchClient, LOW_DETAIL);
                                    }
                                    Logger.Debug(">>{user}: Check pets command executed for {target}", whisperSender, toCheck);
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Check pets command failed due to invalid parameter count. Expected 1, actual {count}", whisperSender, msgData.Length - 1);
                                    twitchClient.QueueWhisper(whisperer, "!checkpets <username>");
                                }
                            }
                            else if (whisperMessage.StartsWith("!grantpet"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Grant pets command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] msgData = whisperMessage.Split(' ');
                                if (msgData.Count() > 1)
                                {
                                    int rarity = -1;
                                    if (int.TryParse(msgData[1], out rarity))
                                    {
                                        wolfcoins.classList[whisperSender].petEarned = rarity;
                                    }
                                }
                                else
                                {
                                    Random rng = new Random();
                                    wolfcoins.classList[whisperSender].petEarned = rng.Next(1, 6);
                                }
                                Dictionary<int, Pet> allPets = petDatabase;
                                GrantPet(whisperSender, wolfcoins, allPets, userSystem, ircClient, twitchClient);
                                Logger.Debug(">>{user}: Grant pets command executed with rarity {rarity}", whisperSender, wolfcoins.classList[whisperSender].petEarned);

                                //Random RNG = new Random();

                                //Pet newPet = new Pet();

                                //int petToAward = RNG.Next(1, petDatabase.Count);
                                //newPet = petDatabase[petToAward];
                                //int sparklyCheck = RNG.Next(1, 100);

                                //if (sparklyCheck == 1)
                                //    newPet.isSparkly = true;

                                //wolfcoins.classList[whisperSender].myPets.Count++;
                                //newPet.stableID = wolfcoins.classList[whisperSender].myPets.Count;

                                //wolfcoins.classList[whisperSender].myPets.Add(newPet);

                                //Whisper(whisperSender, "Added a random pet.", group);

                            }
                            else if (whisperMessage == "!clearpets")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Clear pets command not authorized.", whisperSender);
                                    continue;
                                }

                                wolfcoins.classList[whisperSender].myPets = new List<Pet>();
                                wolfcoins.classList[whisperSender].toRelease = new Pet();
                                wolfcoins.classList[whisperSender].pendingPetRelease = false;

                                Logger.Debug(">>{user}: Clear pets command executed.", whisperSender);
                                twitchClient.QueueWhisper(whisperer, "Pets cleared.");
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
                            else if (whisperMessage.StartsWith("/p") || whisperMessage.StartsWith("/party"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].groupID != -1)
                                    {
                                        string[] msgData = whisperMessage.Split(' ');
                                        if (msgData.Count() > 1)
                                        {
                                            string partyMessage = "";
                                            for (int i = 1; i < msgData.Count(); i++)
                                            {
                                                partyMessage += msgData[i] + " ";
                                            }
                                            int partyID = wolfcoins.classList[whisperSender].groupID;
                                            foreach (var member in parties[partyID].members)
                                            {
                                                Logger.Debug(">>{user}: Party command executed. Whispering target {target} message {message}.", whisperSender, member, partyMessage);
                                                userSystem.GetUserByNameAsync(member.name, (user) =>
                                                {
                                                    if (user.Username.Equals(whisperSender, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        twitchClient.QueueWhisper(user, "You whisper: \" " + partyMessage + "\" ");
                                                    }
                                                    else
                                                    {
                                                        twitchClient.QueueWhisper(user, whisperSender + " says: \" " + partyMessage + "\" ");
                                                    }
                                                });
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Party command failed. No message provided.", whisperSender);
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Party command failed. User is not in wolfcoin collection.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!respec")
                            {
                                if (wolfcoins.classList != null)
                                {
                                    if (wolfcoins.classList.Keys.Contains(whisperSender.ToLower()) && wolfcoins.determineClass(whisperSender) != "INVALID_CLASS")
                                    {
                                        if (wolfcoins.classList[whisperSender].groupID == -1)
                                        {
                                            if (wolfcoins.Exists(wolfcoins.coinList, whisperSender))
                                            {

                                                int respecCost = (baseRespecCost * (wolfcoins.classList[whisperSender].level - 4));
                                                if (respecCost < baseRespecCost)
                                                    respecCost = baseRespecCost;

                                                if (wolfcoins.coinList[whisperSender] <= respecCost)
                                                {
                                                    Logger.Debug(">>{user}: Respec command sent insufficient coins error to user. Needed {cost}, has {coins}.", whisperSender, respecCost, wolfcoins.coinList[whisperSender]);
                                                    twitchClient.QueueWhisper(whisperer, "It costs " + respecCost + " Wolfcoins to respec at your level. You have " + wolfcoins.coinList[whisperSender] + " coins.");
                                                }
                                                int classNumber = wolfcoins.classList[whisperSender].classType * 10;
                                                wolfcoins.classList[whisperSender].classType = classNumber;

                                                Logger.Debug(">>{user}: Respec command sent respec instructions.", whisperSender);
                                                twitchClient.QueueWhisper(whisperer, "You've chosen to respec your class! It will cost you " + respecCost + " coins to respec and you will lose all your items. Reply 'Nevermind' to cancel or one of the following codes to select your new class: ");
                                                twitchClient.QueueWhisper(whisperer, "'C1' (Warrior), 'C2' (Mage), 'C3' (Rogue), 'C4' (Ranger), or 'C5' (Cleric)");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Respec command failed. User has no coins.", whisperSender);
                                                twitchClient.QueueWhisper(whisperer, "You have no coins to respec with.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Respec command failed. User is currently in a party.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You can't respec while in a party!");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Respec command failed. User has no assigned class.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Respec command failed. Wolfcoin class list is null.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!inventory" || whisperMessage == "!inv" || whisperMessage == "inv" || whisperMessage == "inventory")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myItems.Count > 0)
                                    {
                                        Logger.Debug(">>{user}: Inventory command executed, sent user items {items}.", whisperSender, string.Join(", ", wolfcoins.classList[whisperSender].myItems.Select(x => x.itemName)));
                                        twitchClient.QueueWhisper(whisperer, "You have " + wolfcoins.classList[whisperSender].myItems.Count + " items: ");
                                        foreach (var item in wolfcoins.classList[whisperSender].myItems)
                                        {
                                            WhisperItem(whisperSender, item, userSystem, twitchClient, itemDatabase);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Inventory command executed. User has no items.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You have no items.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Inventory command failed. User is not in wolfcoin collection.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!pets" || whisperMessage == "!stable" || whisperMessage == "pets" || whisperMessage == "stable")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        Logger.Debug(">>{user}: Pets command executed. User sent pets {pets}.", whisperSender, string.Join(", ", wolfcoins.classList[whisperSender].myPets.Select(x => x.name)));
                                        twitchClient.QueueWhisper(whisperer, "You have " + wolfcoins.classList[whisperSender].myPets.Count + " pets: ");
                                        foreach (var pet in wolfcoins.classList[whisperSender].myPets)
                                        {
                                            WhisperPet(whisperSender, pet, userSystem, twitchClient, LOW_DETAIL);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Pets command executed. User has no pets.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You have no pets.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Pets command failed. User is not in wolfcoin collection.", whisperSender);
                                }
                            }

                            else if (whisperMessage == "!pethelp")
                            {
                                Logger.Debug(">>{user}: Pet help command executed.", whisperSender);
                                twitchClient.QueueWhisper(whisperer, "View all your pets by whispering me '!pets'. View individual pet stats using '!pet <stable id>' where the id is the number next to your pet's name in brackets [].");
                                twitchClient.QueueWhisper(whisperer, "A summoned/active pet will join you on dungeon runs and possibly even bring benefits! But this will drain its energy, which you can restore by feeding it.");
                                twitchClient.QueueWhisper(whisperer, "You can !dismiss, !summon, !release, !feed, and !hug* your pets using their stable id (ex: !summon 2)");
                                twitchClient.QueueWhisper(whisperer, "*: In development, available soon!");
                            }
                            else if (whisperMessage.StartsWith("!fixpets"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Fix pets command not authorized.", whisperSender);
                                    continue;
                                }

                                //string[] msgData = whisperMessage.Split(' ');
                                //if (msgData.Count() != 2)
                                //{
                                //    Whisper(whisperSender, "Invalid number of parameters. Syntax: !feed <stable ID>", group);
                                //    continue;
                                //}

                                //string playerToFix = msgData[1];
                                foreach (var player in wolfcoins.classList)
                                {
                                    if (player.Value.myPets.Count == 0)
                                    {
                                        continue;
                                    }
                                    int stableIDFix = 1;
                                    foreach (var pet in player.Value.myPets)
                                    {
                                        pet.stableID = stableIDFix;
                                        stableIDFix++;
                                    }
                                    Logger.Debug(">>{user}: Fix pets command executed. {target} pet indices updated.", whisperSender, player.Value.name);
                                    twitchClient.QueueWhisper(whisperer, "Fixed " + player.Value.name + "'s pet IDs.");
                                }
                            }
                            else if (whisperMessage.StartsWith("!feed"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender) && wolfcoins.Exists(wolfcoins.coinList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        string[] msgData = whisperMessage.Split(' ');
                                        if (msgData.Count() != 2)
                                        {
                                            Logger.Debug(">>{user}: Feed command failed due to invalid parameter count. Expected 1, actual {count}.", whisperSender, msgData.Length - 1);
                                            twitchClient.QueueWhisper(whisperer, "Invalid number of parameters. Syntax: !feed <stable ID>");
                                            continue;
                                        }
                                        int petToFeed = -1;
                                        if (int.TryParse(msgData[1], out petToFeed))
                                        {

                                            if (petToFeed > wolfcoins.classList[whisperSender].myPets.Count || petToFeed < 1)
                                            {
                                                Logger.Debug(">>{user}: Feed command failed due to invalid stable id {stable}.", whisperSender, petToFeed);
                                                twitchClient.QueueWhisper(whisperer, "Invalid Stable ID given. Check !pets for each pet's stable ID!");
                                                continue;
                                            }

                                            if (wolfcoins.coinList[whisperSender] < 5)
                                            {
                                                Logger.Debug(">>{user}: Feed command failed due to insufficient wolfcoins.", whisperSender);
                                                twitchClient.QueueWhisper(whisperer, "You lack the 5 wolfcoins to feed your pet! Hop in a Lobos stream soon!");
                                                continue;
                                            }

                                            // build a dummy pet to do calculations
                                            Pet tempPet = wolfcoins.classList[whisperSender].myPets.ElementAt(petToFeed - 1);

                                            if (tempPet.hunger >= Pet.HUNGER_MAX)
                                            {
                                                Logger.Debug(">>{user}: Feed command failed as pet is full.", whisperSender);
                                                twitchClient.QueueWhisper(whisperer, tempPet.name + " is full and doesn't need to eat!");
                                                continue;
                                            }

                                            int currentHunger = tempPet.hunger;
                                            int currentXP = tempPet.xp;
                                            int currentLevel = tempPet.level;
                                            int currentAffection = tempPet.affection + Pet.FEEDING_AFFECTION;

                                            // Charge the player for pet food
                                            wolfcoins.coinList[whisperSender] = wolfcoins.coinList[whisperSender] - Pet.FEEDING_COST;

                                            Logger.Debug(">>{user}: Feed command executed. Pet {pet} was feed for {cost} coins.", whisperSender, tempPet.name, Pet.FEEDING_COST);
                                            twitchClient.QueueWhisper(whisperer, "You were charged " + Pet.FEEDING_COST + " wolfcoins to feed " + tempPet.name + ". They feel refreshed!");
                                            // earn xp equal to amount of hunger 'fed'
                                            currentXP += (Pet.HUNGER_MAX - currentHunger);

                                            // check if pet leveled
                                            if (currentXP >= Pet.XP_TO_LEVEL && currentLevel < Pet.LEVEL_MAX)
                                            {
                                                currentLevel++;
                                                currentXP = currentXP - Pet.XP_TO_LEVEL;
                                                Logger.Debug(">>{user}: Pet level increased from feeding to level {level}.", whisperSender, currentLevel);
                                                twitchClient.QueueWhisper(whisperer, tempPet.name + " leveled up! They are now level " + currentLevel + ".");
                                            }
                                            // refill hunger value
                                            currentHunger = Pet.HUNGER_MAX;

                                            // update temp pet w/ new data
                                            tempPet.affection = currentAffection;
                                            tempPet.hunger = currentHunger;
                                            tempPet.xp = currentXP;
                                            tempPet.level = currentLevel;

                                            // update actual pet data
                                            wolfcoins.classList[whisperSender].myPets.ElementAt(petToFeed - 1).affection = currentAffection;
                                            wolfcoins.classList[whisperSender].myPets.ElementAt(petToFeed - 1).hunger = currentHunger;
                                            wolfcoins.classList[whisperSender].myPets.ElementAt(petToFeed - 1).xp = currentXP;
                                            wolfcoins.classList[whisperSender].myPets.ElementAt(petToFeed - 1).level = currentLevel;

                                            wolfcoins.SaveClassData();
                                            wolfcoins.SaveCoins();
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Feed command failed due to invalid stable id {stable}.", whisperSender, msgData[1]);
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Feed command failed. User is not in both class and coin lists.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!sethunger"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Set hunger command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] msgData = whisperMessage.Split(' ');
                                if (msgData.Count() > 3)
                                {
                                    Logger.Debug(">>{user}: Set hunger command failed due to invalid parameter count. Expected 2, actual {count}.", whisperSender, msgData.Length - 1);
                                    twitchClient.QueueWhisper(whisperer, "Too many parameters. Syntax: !sethunger <stable ID>");
                                    continue;
                                }
                                int petToSet = -1;
                                int amount = -1;
                                if (int.TryParse(msgData[1], out petToSet) && int.TryParse(msgData[2], out amount))
                                {
                                    Logger.Debug(">>{user}: Set hunger command executed. Hunger for {pet} set to {amount}", whisperSender, wolfcoins.classList[whisperSender].myPets.ElementAt(petToSet - 1).name, amount);
                                    wolfcoins.classList[whisperSender].myPets.ElementAt(petToSet - 1).hunger = amount;
                                    twitchClient.QueueWhisper(whisperer, wolfcoins.classList[whisperSender].myPets.ElementAt(petToSet - 1).name + "'s energy set to " + amount + ".");
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Set hunger command failed due to invalid parameters count. {param1} or {param2} failed to parse as int.", whisperSender, msgData[1], msgData[2]);
                                    twitchClient.QueueWhisper(whisperer, "Ya dun fucked somethin' up.");
                                }
                            }
                            else if (whisperMessage.StartsWith("!release"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        string[] msgData = whisperMessage.Split(' ');
                                        if (msgData.Count() != 2)
                                        {
                                            Logger.Debug(">>{user}: Release command failed due to invalid parameter count. Expected 1, actual {count}.", whisperSender, msgData.Length - 1);
                                            twitchClient.QueueWhisper(whisperer, "Invalid number of parameters. Syntax: !release <stable ID>");
                                            continue;
                                        }
                                        int petToRelease = -1;
                                        if (int.TryParse(msgData[1], out petToRelease))
                                        {

                                            if (petToRelease > wolfcoins.classList[whisperSender].myPets.Count || petToRelease < 1)
                                            {
                                                Logger.Debug(">>{user}: Release command failed due to invalid stable id {stable}.", whisperSender, petToRelease);
                                                twitchClient.QueueWhisper(whisperer, "Invalid Stable ID given. Check !pets for each pet's stable ID!");
                                                continue;
                                            }
                                            string petName = wolfcoins.classList[whisperSender].myPets.ElementAt(petToRelease - 1).name;
                                            //wolfcoins.classList[whisperSender].toRelease = petToRelease;
                                            wolfcoins.classList[whisperSender].pendingPetRelease = true;
                                            wolfcoins.classList[whisperSender].toRelease = new Pet();
                                            wolfcoins.classList[whisperSender].toRelease.stableID = wolfcoins.classList[whisperSender].myPets.ElementAt(petToRelease - 1).stableID;
                                            Logger.Debug(">>{user}: Release command executed. Pet {pet} pending release.", whisperSender, petName);
                                            twitchClient.QueueWhisper(whisperer, "If you release " + petName + ", they will be gone forever. Are you sure you want to release them? (y/n)");
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Release command failed due to parameter {parameter} failing to parse as int.", whisperSender, msgData[1]);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Release command failed as user has no pets.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You don't have a pet.");
                                    }
                                }
                            }
                            else if (whisperMessage.StartsWith("!dismiss"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        string[] msgData = whisperMessage.Split(' ');
                                        if (msgData.Count() != 2)
                                        {
                                            Logger.Debug(">>{user}: Dismiss command failed due to invalid parameter count. Expected 1, actual {count}.", whisperSender, msgData.Length - 1);
                                            twitchClient.QueueWhisper(whisperer, "Invalid number of parameters. Syntax: !dismiss <stable ID>");
                                            continue;
                                        }
                                        int petToDismiss = -1;
                                        if (int.TryParse(msgData[1], out petToDismiss))
                                        {
                                            if (petToDismiss > wolfcoins.classList[whisperSender].myPets.Count || petToDismiss < 1)
                                            {
                                                Logger.Debug(">>{user}: Dismiss command failed due to invalid stable id {stable}.", whisperSender, petToDismiss);
                                                twitchClient.QueueWhisper(whisperer, "Invalid Stable ID given. Check !pets for each pet's stable ID!");
                                                continue;
                                            }
                                            if (wolfcoins.classList[whisperSender].myPets.ElementAt(petToDismiss - 1).isActive)
                                            {
                                                wolfcoins.classList[whisperSender].myPets.ElementAt(petToDismiss - 1).isActive = false;
                                                Logger.Debug(">>{user}: Dismiss command executed, {pet} dismissed.", whisperSender, wolfcoins.classList[whisperSender].myPets.ElementAt(petToDismiss - 1).name);
                                                twitchClient.QueueWhisper(whisperer, "You dismissed " + wolfcoins.classList[whisperSender].myPets.ElementAt(petToDismiss - 1).name + ".");
                                                wolfcoins.SaveClassData();
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Dismiss command failed as pet {stable} not currently summoned.", whisperSender, petToDismiss);
                                                twitchClient.QueueWhisper(whisperer, "That pet is not currently summoned.");
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Dismiss command failed due to parameter {parameter} failing to parse as int.", whisperSender, msgData[1]);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Dismiss command failed as user has no pets.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You don't have a pet.");
                                    }
                                }
                            }
                            else if (whisperMessage.StartsWith("!summon"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        string[] msgData = whisperMessage.Split(' ');
                                        if (msgData.Count() != 2)
                                        {
                                            Logger.Debug(">>{user}: Summon command failed due to invalid parameter count. Expected 1, actual {count}.", whisperSender, msgData.Length - 1);
                                            twitchClient.QueueWhisper(whisperer, "Invalid number of parameters. Syntax: !summon <stable ID>");
                                            continue;
                                        }
                                        int petToSummon = -1;
                                        int currentlyActivePet = -1;
                                        if (int.TryParse(msgData[1], out petToSummon))
                                        {
                                            if (petToSummon > wolfcoins.classList[whisperSender].myPets.Count || petToSummon < 1)
                                            {
                                                Logger.Debug(">>{user}: Summon command failed due to invalid stable id {stable}.", whisperSender, petToSummon);
                                                twitchClient.QueueWhisper(whisperer, "Invalid Stable ID given. Check !pets for each pet's stable ID!");
                                                continue;
                                            }
                                            foreach (var pet in wolfcoins.classList[whisperSender].myPets)
                                            {
                                                if (pet.isActive)
                                                {
                                                    currentlyActivePet = pet.stableID;
                                                }
                                            }
                                            if (currentlyActivePet > wolfcoins.classList[whisperSender].myPets.Count)
                                            {
                                                Logger.Debug(">>{user}: Summon command failed due to corrupt stable id {stable}.", whisperSender, currentlyActivePet);
                                                twitchClient.QueueWhisper(whisperer, "Sorry, your stableID is corrupt. Lobos is working on this issue :(");
                                                continue;
                                            }
                                            if (!wolfcoins.classList[whisperSender].myPets.ElementAt(petToSummon - 1).isActive)
                                            {
                                                wolfcoins.classList[whisperSender].myPets.ElementAt(petToSummon - 1).isActive = true;
                                                Logger.Debug(">>{user}: Summon command executed, {pet} summoned.", whisperSender, wolfcoins.classList[whisperSender].myPets.ElementAt(petToSummon - 1).name);
                                                twitchClient.QueueWhisper(whisperer, "You summoned " + wolfcoins.classList[whisperSender].myPets.ElementAt(petToSummon - 1).name + ".");
                                                if (currentlyActivePet != -1)
                                                {
                                                    wolfcoins.classList[whisperSender].myPets.ElementAt(currentlyActivePet - 1).isActive = false;
                                                    Logger.Debug(">>{user}: Previously summoned pet {pet} dismissed by summon command.", whisperSender, wolfcoins.classList[whisperSender].myPets.ElementAt(currentlyActivePet - 1).name);
                                                    twitchClient.QueueWhisper(whisperer, wolfcoins.classList[whisperSender].myPets.ElementAt(currentlyActivePet - 1).name + " was dismissed.");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Summon command failed as pet {pet} was already summoned.", whisperSender, wolfcoins.classList[whisperSender].myPets.ElementAt(petToSummon - 1).name);
                                                twitchClient.QueueWhisper(whisperer, wolfcoins.classList[whisperSender].myPets.ElementAt(petToSummon - 1).name + " is already summoned!");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Summon command failed as user has no pets.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You don't have a pet.");
                                    }
                                }
                            }
                            else if (whisperMessage.StartsWith("!pet"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        string[] msgData = whisperMessage.Split(' ');
                                        if (msgData.Count() != 2)
                                        {
                                            Logger.Debug(">>{user}: Pet command failed due to invalid parameter count. Expected 1, actual {count}.", whisperSender, msgData.Length - 1);
                                            twitchClient.QueueWhisper(whisperer, "Invalid number of parameters. Syntax: !pet <stable ID>");
                                            continue;
                                        }
                                        int petToSend = -1;
                                        if (int.TryParse(msgData[1], out petToSend))
                                        {
                                            if (petToSend > wolfcoins.classList[whisperSender].myPets.Count || petToSend < 1)
                                            {
                                                Logger.Debug(">>{user}: Pet command failed due to invalid stable id {stable}.", whisperSender, petToSend);
                                                twitchClient.QueueWhisper(whisperer, "Invalid Stable ID given. Check !pets for each pet's stable ID!");
                                                continue;
                                            }
                                            Logger.Debug(">>{user}: Pet command executed. Details of pet {pet} sent.", whisperSender, wolfcoins.classList[whisperSender].myPets.ElementAt(petToSend - 1).name);
                                            WhisperPet(whisperSender, wolfcoins.classList[whisperSender].myPets.ElementAt(petToSend - 1), userSystem, twitchClient, HIGH_DETAIL);
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Pet command failed due to parameter {parameter} failing to parse as int.", whisperSender, msgData[1]);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Pet command failed as user has no pets.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You don't have any pets.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Pet command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!rename"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        string[] msgData = whisperMessage.Split(' ');
                                        if (msgData.Count() != 3)
                                        {
                                            Logger.Debug(">>{user}: Rename command failed due to invalid parameter count. Expected 2, actual {count}.", whisperSender, msgData.Length - 1);
                                            twitchClient.QueueWhisper(whisperer, "Invalid number of parameters. Note: names cannot contain spaces.");
                                            continue;
                                        }
                                        else if (msgData.Count() == 3)
                                        {
                                            int petToRename = -1;
                                            if (int.TryParse(msgData[1], out petToRename))
                                            {
                                                if (petToRename > (wolfcoins.classList[whisperSender].myPets.Count) || petToRename < 1)
                                                {
                                                    Logger.Debug(">>{user}: Rename command failed due to invalid stable id {stable}.", whisperSender, petToRename);
                                                    twitchClient.QueueWhisper(whisperer, "Sorry, the Stable ID given was invalid. Please try again.");
                                                    continue;
                                                }
                                                string newName = msgData[2];
                                                if (newName.Length > 16)
                                                {
                                                    Logger.Debug(">>{user}: Rename command failed due to name {name} exceeding max length.", whisperSender, newName);
                                                    twitchClient.QueueWhisper(whisperer, "Name can only be 16 characters max.");
                                                    continue;
                                                }
                                                string prevName = wolfcoins.classList[whisperSender].myPets.ElementAt(petToRename - 1).name;
                                                wolfcoins.classList[whisperSender].myPets.ElementAt(petToRename - 1).name = newName;
                                                Logger.Debug(">>{user}: Rename command executed, renaming pet {prevName} to {newName}.", whisperSender, prevName, newName);
                                                twitchClient.QueueWhisper(whisperer, prevName + " was renamed to " + newName + "!");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Rename command failed due to parameter {parameter} failing to parse as int.", whisperSender, msgData[1]);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Rename command failed due to invalid parameter count. Expected 2, actual {count}.", whisperSender, msgData.Length - 1);
                                            twitchClient.QueueWhisper(whisperer, "Sorry, the data you provided didn't work. Syntax: !rename <stable id> <new name>");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Rename command failed as user has no pets.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You don't have any pets to rename. :(");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Rename command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!start"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    int partyID = wolfcoins.classList[whisperSender].groupID;
                                    if (parties.Count() > 0 && partyID != -1 && parties.ContainsKey(partyID))
                                    {
                                        if (parties[partyID].status == PARTY_READY)
                                        {
                                            if (!(parties[partyID].partyLeader == whisperSender))
                                            {
                                                Logger.Debug(">>{user}: Start command failed because user is not leader ({leader}).", whisperSender, parties[partyID].partyLeader);
                                                twitchClient.QueueWhisper(whisperer, "You are not the party leader!");
                                                continue;
                                            }
                                            string[] msgData = whisperMessage.Split(' ');
                                            int dungeonID = -1;
                                            if (msgData.Count() > 1)
                                            {
                                                int.TryParse(msgData[1], out dungeonID);
                                            }
                                            else if (wolfcoins.classList[whisperSender].groupFinderDungeon != -1)
                                            {
                                                dungeonID = wolfcoins.classList[whisperSender].groupFinderDungeon;
                                                wolfcoins.classList[whisperSender].groupFinderDungeon = -1;
                                            }
                                            //else if (wolfcoins.classList[whisperSender].queueDungeons.Count > 0)
                                            //{
                                            //    dungeonID = wolfcoins.classList[whisperSender].queueDungeons.ElementAt(0);
                                            //}
                                            else
                                            {
                                                Logger.Debug(">>{user}: Start command failed due to invalid dungeon id {dungeonID}.", whisperSender, dungeonID);
                                                twitchClient.QueueWhisper(whisperer, "Invalid Dungeon ID provided.");
                                                continue;
                                            }
                                            if (dungeonList.Count() >= dungeonID && dungeonID > 0)
                                            {
                                                string dungeonPath = "content/dungeons/" + dungeonList[dungeonID];
                                                IEnumerable<string> fileText = System.IO.File.ReadLines(dungeonPath, UTF8Encoding.Default);
                                                string[] type = fileText.ElementAt(0).Split('=');
                                                if (type[1] == "Dungeon" && parties[partyID].NumMembers() > 3)
                                                {
                                                    Logger.Debug(">>{user}: Start command failed as party {partyID} contains too many members: {members}.", whisperSender, partyID, string.Join(", ", parties[partyID].members.Select(x => x.name)));
                                                    twitchClient.QueueWhisper(whisperer, "You can't have more than 3 party members for a Dungeon.");
                                                    continue;
                                                }

                                                if (wolfcoins.classList[whisperSender].queueDungeons.Count > 0)
                                                {
                                                    foreach (var member in parties[partyID].members)
                                                    {
                                                        wolfcoins.classList[member.name].queueDungeons = new List<int>();
                                                    }
                                                }

                                                Dungeon newDungeon = new Dungeon(dungeonPath, tokenData.BroadcastUser, itemDatabase);
                                                bool outOfLevelRange = false;
                                                foreach (var member in parties[partyID].members)
                                                {
                                                    member.level = wolfcoins.determineLevel(member.name);
                                                    //if (member.level < newDungeon.minLevel)
                                                    //{
                                                    //    Whisper(parties[partyID], member.name + " is not high enough level for the requested dungeon. (Min Level: " + newDungeon.minLevel + ")", group);
                                                    //    outOfLevelRange = true;
                                                    //}
                                                }
                                                int minLevel = 3;
                                                List<string> brokeBitches = new List<string>();
                                                bool enoughMoney = true;
                                                foreach (var member in parties[partyID].members)
                                                {

                                                    if (wolfcoins.Exists(wolfcoins.coinList, member.name))
                                                    {
                                                        if (wolfcoins.coinList[member.name] < (baseDungeonCost + ((member.level - minLevel) * 10)))
                                                        {
                                                            brokeBitches.Add(member.name);
                                                            enoughMoney = false;
                                                        }
                                                    }
                                                }

                                                if (!enoughMoney)
                                                {
                                                    var partyUsers = userSystem.GetUsersByNames(parties[partyID].members.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                                    string names = "";
                                                    foreach (var bitch in brokeBitches)
                                                    {
                                                        names += bitch + " ";
                                                    }
                                                    Logger.Debug(">>{user}: Start command failed as user(s) {users} have insufficient funds.", whisperSender, string.Join(", ", brokeBitches));
                                                    twitchClient.QueueWhisper(partyUsers, "The following party members do not have enough money to run " + newDungeon.dungeonName + ": " + names);
                                                }

                                                if (!outOfLevelRange && enoughMoney)
                                                {
                                                    var partyUsers = userSystem.GetUsersByNames(parties[partyID].members.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                                    Logger.Debug(">>{user}: Start command executed. Starting dungeon {dungeon} for {members}.", whisperSender, newDungeon.dungeonName, string.Join(", ", parties[partyID].members.Select(x => x.name)));
                                                    foreach (var member in parties[partyID].members)
                                                    {
                                                        wolfcoins.coinList[member.name] -= (baseDungeonCost + ((member.level - minLevel) * 10));
                                                    }
                                                    twitchClient.QueueWhisper(partyUsers, "Successfully initiated " + newDungeon.dungeonName + "! Wolfcoins deducted.");
                                                    string memberInfo = "";
                                                    foreach (var member in parties[partyID].members)
                                                    {
                                                        memberInfo += member.name + " (Level " + member.level + " " + member.className + ") ";
                                                    }

                                                    twitchClient.QueueWhisper(partyUsers, "Your party consists of: " + memberInfo);
                                                    parties[partyID].status = PARTY_STARTED;
                                                    parties[partyID].myDungeon = newDungeon;
                                                    parties[partyID] = parties[partyID].myDungeon.RunDungeon(parties[partyID], ref userSystem, ref twitchClient);

                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Start command failed as user is not in a party.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Start command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "y")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].pendingPetRelease)
                                    {
                                        int toRelease = wolfcoins.classList[whisperSender].toRelease.stableID;
                                        if (toRelease > wolfcoins.classList[whisperSender].myPets.Count)
                                        {
                                            Logger.Debug(">>{user}: \"Y\" command to release pet failed due to stable id mismatch.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "Stable ID mismatch. Try !release again.");
                                            continue;
                                        }
                                        string petName = wolfcoins.classList[whisperSender].myPets.ElementAt(toRelease - 1).name;
                                        if (wolfcoins.classList[whisperSender].releasePet(toRelease))
                                        {
                                            Logger.Debug(">>{user}: \"Y\" command to release pet executed. Pet {pet} released.", whisperSender, petName);
                                            twitchClient.QueueWhisper(whisperer, "You released " + petName + ". Goodbye, " + petName + "!");
                                            wolfcoins.SaveClassData();
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: \"Y\" command to release pet failed for unknown reason.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "Something went wrong. " + petName + " is still with you!");
                                        }
                                        wolfcoins.classList[whisperSender].pendingPetRelease = false;
                                        wolfcoins.classList[whisperSender].toRelease = new Pet();
                                    }

                                    if (wolfcoins.classList[whisperSender].pendingInvite)
                                    {
                                        int partyID = wolfcoins.classList[whisperSender].groupID;
                                        wolfcoins.classList[whisperSender].pendingInvite = false;
                                        string partyLeader = parties[partyID].partyLeader;
                                        int partySize = parties[partyID].NumMembers();
                                        string myClass = wolfcoins.classList[whisperSender].className;
                                        int myLevel = wolfcoins.determineLevel(wolfcoins.xpList[whisperSender]);
                                        string myMembers = "";
                                        foreach (var member in parties[partyID].members)
                                        {
                                            myMembers += member.name + " ";
                                        }
                                        Logger.Debug(">>{user}: \"Y\" command to join party executed. User added to party with {members}.", whisperSender, myMembers);
                                        twitchClient.QueueWhisper(whisperer, "You successfully joined a party with the following members: " + myMembers);
                                        var notifyNames = parties[partyID].members.Where(x => !x.pendingInvite && !x.name.Equals(whisperSender, StringComparison.OrdinalIgnoreCase)).Select(x => x.name);
                                        var notifyUsers = userSystem.GetUsersByNames(notifyNames.ToArray()).GetAwaiter().GetResult();
                                        twitchClient.QueueWhisper(notifyUsers, whisperSender + ", Level " + myLevel + " " + myClass + " has joined your party! (" + partySize + "/" + DUNGEON_MAX + ")");

                                        var leaderUser = userSystem.GetUserByName(partyLeader);
                                        if (partySize == DUNGEON_MAX)
                                        {
                                            twitchClient.QueueWhisper(leaderUser, "Your party is now full.");
                                            parties[partyID].status = PARTY_FULL;
                                        }

                                        if (partySize == 3)
                                        {
                                            twitchClient.QueueWhisper(leaderUser, "You've reached 3 party members! You're ready to dungeon!");
                                            parties[partyID].status = PARTY_READY;
                                        }
                                        Logger.Info("{user} added to Group {id}", whisperSender, partyID);
                                        string temp = "Updated Member List: ";
                                        foreach (var member in parties[partyID].members)
                                        {
                                            temp += member.name + " ";
                                        }
                                        Logger.Info(temp);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: \"Y\" command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!unready")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    CharClass myClass = wolfcoins.classList[whisperSender];
                                    if (myClass.isPartyLeader)
                                    {
                                        if (parties[myClass.groupID].status == PARTY_READY && parties[myClass.groupID].members.Count <= DUNGEON_MAX)
                                        {
                                            parties[myClass.groupID].status = PARTY_FORMING;
                                            var usersToWhisper = userSystem.GetUsersByNames(parties[myClass.groupID].members.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                            twitchClient.QueueWhisper(usersToWhisper, "Party 'Ready' status has been revoked.");
                                        }
                                        Logger.Debug(">>{user}: Unready command executed for group.", whisperSender);
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Unready command failed as user is not party leader.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Unready command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!ready")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    CharClass myClass = wolfcoins.classList[whisperSender];
                                    if (myClass.isPartyLeader)
                                    {
                                        if (parties.ContainsKey(myClass.groupID) && parties[myClass.groupID].status == PARTY_FORMING)
                                        {
                                            if (parties[myClass.groupID].members.Any(x => x.pendingInvite))
                                            {
                                                Logger.Debug(">>{user}: Ready command failed as one or more members has not accepted invitation.", whisperSender);
                                                twitchClient.QueueWhisper(whisperer, "One or more members have not accepted their invitation.");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Ready command executed.", whisperSender);
                                                parties[myClass.groupID].status = PARTY_READY;
                                                var usersToWhisper = userSystem.GetUsersByNames(parties[myClass.groupID].members.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                                twitchClient.QueueWhisper(usersToWhisper, "Party set to 'Ready'. Be careful adventuring without a full party!");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ready command failed as party status is not forming.", whisperSender);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Ready command failed as user is not party leader.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Ready command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "n")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].pendingPetRelease)
                                    {
                                        string petName = wolfcoins.classList[whisperSender].toRelease.name;
                                        wolfcoins.classList[whisperSender].pendingPetRelease = false;
                                        wolfcoins.classList[whisperSender].toRelease = new Pet();

                                        twitchClient.QueueWhisper(whisperer, "You decided to keep " + petName + ".");
                                        Logger.Debug(">>{user}: \"N\" command to release pet executed.", whisperSender);
                                    }

                                    if (wolfcoins.classList[whisperSender].pendingInvite)
                                    {
                                        wolfcoins.classList[whisperSender].pendingInvite = false;
                                        string partyLeader = parties[wolfcoins.classList[whisperSender].groupID].partyLeader;
                                        parties[wolfcoins.classList[whisperSender].groupID].RemoveMember(whisperSender);
                                        wolfcoins.classList[whisperSender].groupID = -1;
                                        twitchClient.QueueWhisper(whisperer, "You declined " + partyLeader + "'s invite.");
                                        userSystem.GetUserByNameAsync(partyLeader, (user) => { twitchClient.QueueWhisper(user, whisperSender + " has declined your party invite."); });
                                        wolfcoins.classList[partyLeader].numInvitesSent--;
                                        Logger.Debug(">>{user}: \"N\" command to join party executed.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: \"N\" command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!kick"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender) && wolfcoins.classList[whisperSender].isPartyLeader)
                                {
                                    if (wolfcoins.classList[whisperSender].groupID != -1)
                                    {
                                        if (parties[wolfcoins.classList[whisperSender].groupID].status == PARTY_STARTED)
                                        {
                                            Logger.Debug(">>{user}: Kick command failed as user is in a dungeon.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You can't kick a party member in the middle of a dungeon!");
                                            continue;
                                        }
                                        if (!string.IsNullOrWhiteSpace(whisperMessage))
                                        {
                                            string[] msgData = whisperMessage.Split(' ');

                                            if (msgData.Count() > 1)
                                            {
                                                string toKick = msgData[1];
                                                if (whisperSender == toKick)
                                                {
                                                    Logger.Debug(">>{user}: Kick command failed. User tried to kick themself.", whisperSender, toKick);
                                                    twitchClient.QueueWhisper(whisperer, "You can't kick yourself from a group! Do !leaveparty instead.");
                                                    continue;
                                                }
                                                toKick = toKick.ToLower();
                                                if (wolfcoins.classList.Keys.Contains(toKick.ToLower()))
                                                {
                                                    if (wolfcoins.classList[whisperSender].isPartyLeader)
                                                    {
                                                        int myID = wolfcoins.classList[whisperSender].groupID;
                                                        for (int i = 0; i < parties[myID].members.Count(); i++)
                                                        {
                                                            if (parties[myID].members.ElementAt(i).name == toKick)
                                                            {
                                                                Logger.Debug(">>{user}: User {target} kicked from party.", whisperSender, toKick);
                                                                parties[myID].RemoveMember(toKick);
                                                                wolfcoins.classList[toKick].groupID = -1;
                                                                wolfcoins.classList[toKick].pendingInvite = false;
                                                                wolfcoins.classList[toKick].numInvitesSent = 0;
                                                                wolfcoins.classList[whisperSender].numInvitesSent--;
                                                                userSystem.GetUserByNameAsync(toKick, (user) => { twitchClient.QueueWhisper(user, "You were removed from " + whisperSender + "'s party."); });
                                                                var partyMembers = userSystem.GetUsersByNames(parties[myID].members.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                                                twitchClient.QueueWhisper(partyMembers, toKick + " was removed from the party.");
                                                            }
                                                        }
                                                        Logger.Debug(">>{user}: Kick command executed.", whisperSender);
                                                    }
                                                    else
                                                    {
                                                        Logger.Debug(">>{user}: Kick command failed as user is not party leader.", whisperSender);
                                                        twitchClient.QueueWhisper(whisperer, "You are not the party leader.");
                                                    }
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Kick command failed. User {target} not in party.", whisperSender, toKick);
                                                    twitchClient.QueueWhisper(whisperer, "Couldn't find that party member to remove.");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Kick command failed due to invalid parameter count. Expected 1, actual {count}", whisperSender, msgData.Count() - 1);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Kick command failed as parameter is null.", whisperSender);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Kick command failed as user is not in a group", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Kick command failed as user is not in class list or is not party leader.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!add"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender) && wolfcoins.classList[whisperSender].isPartyLeader)
                                {
                                    if (wolfcoins.classList[whisperSender].groupID != -1)
                                    {
                                        if (parties[wolfcoins.classList[whisperSender].groupID].status == PARTY_STARTED)
                                            continue;

                                        if (wolfcoins.classList[whisperSender].usedGroupFinder && parties[wolfcoins.classList[whisperSender].groupID].NumMembers() == 3)
                                        {
                                            twitchClient.QueueWhisper(whisperer, "You can't have more than 3 party members for a Group Finder dungeon.");
                                            continue;
                                        }

                                        if (!string.IsNullOrWhiteSpace(whisperMessage))
                                        {
                                            string[] msgData = whisperMessage.Split(' ');

                                            if (msgData.Count() > 1)
                                            {
                                                string invitee = msgData[1];
                                                if (whisperSender == invitee)
                                                {
                                                    Logger.Debug(">>{user}: Add command failed as target and user are the same.", whisperSender);
                                                    twitchClient.QueueWhisper(whisperer, "You can't invite yourself to a group!");
                                                    continue;
                                                }
                                                if (wolfcoins.Exists(wolfcoins.classList, invitee) && wolfcoins.classList[invitee].queueDungeons.Count > 0)
                                                {
                                                    Logger.Debug(">>{user}: Add command failed as target {target} is queued in group finder.", whisperSender, invitee);
                                                    twitchClient.QueueWhisper(whisperer, invitee + " is currently queued for Group Finder and cannot be added to the group.");
                                                    userSystem.GetUserByNameAsync(invitee, (user) => { twitchClient.QueueWhisper(user, whisperSender + " tried to invite you to a group, but you are queued in the Group Finder. Type '!leavequeue' to leave the queue."); });
                                                    continue;
                                                }

                                                invitee = invitee.ToLower();
                                                if (wolfcoins.classList.Keys.Contains(invitee.ToLower()))
                                                {
                                                    int myID = wolfcoins.classList[whisperSender].groupID;
                                                    if (wolfcoins.classList[invitee].classType != -1 && wolfcoins.classList[invitee].groupID == -1
                                                        && !wolfcoins.classList[invitee].pendingInvite && wolfcoins.classList[whisperSender].numInvitesSent < DUNGEON_MAX
                                                        && parties.ContainsKey(myID))
                                                    {
                                                        if (parties[myID].status != PARTY_FORMING && parties[myID].status != PARTY_READY)
                                                        {
                                                            Logger.Debug(">>{user}: Add command failed as party not in valid state.", whisperSender);
                                                            continue;
                                                        }

                                                        wolfcoins.classList[whisperSender].numInvitesSent++;
                                                        wolfcoins.classList[invitee].pendingInvite = true;
                                                        wolfcoins.classList[invitee].groupID = myID;
                                                        wolfcoins.classList[invitee].ClearQueue();
                                                        string myClass = wolfcoins.classList[whisperSender].className;
                                                        int myLevel = wolfcoins.classList[whisperSender].level;
                                                        parties[myID].AddMember(wolfcoins.classList[invitee]);
                                                        string msg = whisperSender + ", Level " + myLevel + " " + myClass + ", has invited you to join a party. Accept? (y/n)";
                                                        Logger.Debug(">>{user}: Add command executed. User {target} invited to group.", whisperSender, invitee);
                                                        twitchClient.QueueWhisper(whisperer, "You invited " + invitee + " to a group.");
                                                        userSystem.GetUserByNameAsync(invitee, (user) => { twitchClient.QueueWhisper(user, msg); });
                                                    }
                                                    else if (wolfcoins.classList[whisperSender].numInvitesSent >= DUNGEON_MAX)
                                                    {
                                                        Logger.Debug(">>{user}: Add command failed as user has sent too many invites.", whisperSender);
                                                        twitchClient.QueueWhisper(whisperer, "You have the max number of invites already pending.");
                                                    }
                                                    else if (wolfcoins.classList[invitee].groupID != -1)
                                                    {
                                                        Logger.Debug(">>{user}: Add command failed as target {target} is already in a group.", whisperSender, invitee);
                                                        twitchClient.QueueWhisper(whisperer, invitee + " is already in a group.");
                                                        userSystem.GetUserByNameAsync(invitee, (user) => { twitchClient.QueueWhisper(user, whisperSender + " tried to invite you to a group, but you are already in one! Type '!leaveparty' to abandon your current group."); });
                                                    }
                                                }
                                                else
                                                {
                                                    if (wolfcoins.Exists(wolfcoins.xpList, invitee))
                                                    {
                                                        int level = wolfcoins.determineLevel(invitee);
                                                        if (level < 3)
                                                        {
                                                            Logger.Debug(">>{user}: Add command failed as target {target} is not high enough level.", whisperSender, invitee);
                                                            //Whisper(whisperSender, invitee + " is not high enough level. (" + level + ")", group);
                                                        }
                                                        else
                                                        {
                                                            Logger.Debug(">>{user}: Add command failed as target {target} has not picked a class.", whisperSender, invitee);
                                                            twitchClient.QueueWhisper(whisperer, invitee + " is high enough level, but has not picked a class!");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Logger.Debug(">>{user}: Add command failed as target {target} not in xp list.", whisperSender, invitee);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Add command failed due to invalid parameter count. Expected 1, actual {param}.", whisperSender, msgData.Length - 1);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Add command failed as parameter is null.", whisperSender);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Add command failed as user is not in a party.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Add command failed as user is not in class list or is not party leader.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!promote"))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    int partyID = wolfcoins.classList[whisperSender].groupID;
                                    if (partyID == -1)
                                    {
                                        Logger.Debug(">>{user}: Promote command failed as user is not in a party.", whisperSender);
                                        continue;
                                    }

                                    if (!wolfcoins.classList[whisperSender].isPartyLeader)
                                    {
                                        Logger.Debug(">>{user}: Promote command failed as user is not party leader.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You must be the party leader to promote.");
                                        continue;
                                    }

                                    if (!string.IsNullOrWhiteSpace(whisperMessage))
                                    {
                                        string[] msgData = whisperMessage.Split(' ');

                                        if (msgData.Count() > 1 && msgData.Count() <= 3)
                                        {
                                            string newLeader = msgData[1].ToLower();
                                            bool newLeaderCreated = false;

                                            foreach (var member in parties[partyID].members)
                                            {
                                                if (newLeaderCreated)
                                                    continue;

                                                if (member.name == whisperSender)
                                                    member.isPartyLeader = false;

                                                if (member.name == newLeader)
                                                {

                                                    wolfcoins.classList[whisperSender].isPartyLeader = false;
                                                    parties[partyID].partyLeader = newLeader;
                                                    wolfcoins.classList[newLeader].isPartyLeader = true;
                                                    member.isPartyLeader = true;

                                                    newLeaderCreated = true;
                                                }
                                            }

                                            if (newLeaderCreated)
                                            {
                                                foreach (var member in parties[partyID].members)
                                                {
                                                    if (member.name != newLeader && member.name != whisperSender)
                                                    {
                                                        userSystem.GetUserByNameAsync(member.name, (user) => { twitchClient.QueueWhisper(user, whisperSender + " has promoted " + newLeader + " to Party Leader."); });
                                                    }
                                                }
                                                Logger.Debug(">>{user}: Promote command executed. Player {target} is now party leader.", whisperSender, newLeader);
                                                userSystem.GetUserByNameAsync(newLeader, (user) => { twitchClient.QueueWhisper(user, whisperSender + " has promoted you to Party Leader."); });
                                                twitchClient.QueueWhisper(whisperer, "You have promoted " + newLeader + " to Party Leader.");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Promote command failed as target {target} not found.", whisperSender, newLeader);
                                                twitchClient.QueueWhisper(whisperer, "Party member '" + newLeader + "' not found. You are still party leader.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Promote command failed due to invalid parameter count. Expected 1, actual {param}.", whisperSender, msgData.Length - 1);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Promote command failed as parameter is null.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Promote command failed as user is not in class list.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!leaveparty")
                            {
                                if (parties.Count() > 0 && wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    int myID = wolfcoins.classList[whisperSender].groupID;
                                    if (myID != -1 && parties[myID].status == PARTY_STARTED)
                                    {
                                        Logger.Debug(">>{user}: Leave party command failed as user is in a dungeon.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You can't leave your party while a dungeon is in progress!");
                                        continue;
                                    }
                                    if (myID != -1 && !wolfcoins.classList[whisperSender].pendingInvite)
                                    {
                                        if (parties.Count() > 0 && parties.ContainsKey(myID))
                                        {
                                            if (wolfcoins.classList[whisperSender].isPartyLeader)
                                            {
                                                wolfcoins.classList[whisperSender].groupID = -1;
                                                wolfcoins.classList[whisperSender].numInvitesSent = 0;
                                                wolfcoins.classList[whisperSender].isPartyLeader = false;
                                                wolfcoins.classList[whisperSender].ClearQueue();

                                                parties[myID].RemoveMember(whisperSender);
                                                Logger.Info("Party Leader {user} left group #{id}", whisperSender, myID);
                                                string myMembers = "";
                                                foreach (var member in parties[myID].members)
                                                {
                                                    myMembers += member.name + " ";
                                                }
                                                Logger.Info("Remaining members: {members}", myMembers);
                                                var partyMembers = userSystem.GetUsersByNames(parties[myID].members.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                                twitchClient.QueueWhisper(partyMembers, "The party leader (" + whisperSender + ") has left. Your party has been disbanded.");
                                                for (int i = 0; i < parties[myID].members.Count(); i++)
                                                {
                                                    string dude = parties[myID].members.ElementAt(i).name;
                                                    wolfcoins.classList[dude].groupID = -1;
                                                    wolfcoins.classList[dude].pendingInvite = false;
                                                    wolfcoins.classList[dude].numInvitesSent = 0;
                                                    wolfcoins.classList[dude].ClearQueue();

                                                }
                                                parties.Remove(myID);
                                                twitchClient.QueueWhisper(whisperer, "Your party has been disbanded.");

                                            }
                                            else if (parties.ContainsKey(myID) && (parties[myID].RemoveMember(whisperSender)))
                                            {
                                                if (parties[myID].status == PARTY_FORMING)
                                                {
                                                    string partyleader = parties[myID].partyLeader;
                                                    wolfcoins.classList[partyleader].numInvitesSent--;
                                                }
                                                else if (parties[myID].status == PARTY_FULL)
                                                {
                                                    string partyleader = parties[myID].partyLeader;
                                                    parties[myID].status = PARTY_FORMING;
                                                    wolfcoins.classList[partyleader].numInvitesSent--;
                                                }

                                                Logger.Debug(">>{user}: Leave party command executed.", whisperSender);
                                                var partyMembers = userSystem.GetUsersByNames(parties[myID].members.Select(x => x.name).ToArray()).GetAwaiter().GetResult();
                                                twitchClient.QueueWhisper(partyMembers, whisperSender + " has left the party.");
                                                twitchClient.QueueWhisper(whisperer, "You left the party.");
                                                wolfcoins.classList[whisperSender].groupID = -1;
                                                wolfcoins.classList[whisperSender].ClearQueue();
                                                Logger.Info("{user} left group with ID {id}", whisperSender, myID);
                                                string myMembers = "";
                                                foreach (var member in parties[myID].members)
                                                {
                                                    myMembers += member.name + " ";
                                                }
                                                Logger.Info("Remaining Members: {members}", myMembers);
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Leave party command failed for unknown reason.", whisperSender);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Leave party command failed as there are no groups formed or user's group is not in group list.", whisperSender);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Leave party command failed as user not in a group, or is pending an invite.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Leave party command failed as user not found in class list, or no parties exist.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!daily")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    double minutes = (DateTime.Now - wolfcoins.classList[whisperSender].lastDailyGroupFinder).TotalMinutes;
                                    double totalHours = (DateTime.Now - wolfcoins.classList[whisperSender].lastDailyGroupFinder).TotalHours;
                                    double totalDays = (DateTime.Now - wolfcoins.classList[whisperSender].lastDailyGroupFinder).TotalDays;
                                    Logger.Debug(">>{user}: Daily command executed.", whisperSender);
                                    if (totalDays >= 1)
                                    {
                                        twitchClient.QueueWhisper(whisperer, "You are eligible for daily Group Finder rewards! Go queue up!");
                                        continue;
                                    }
                                    else
                                    {
                                        double minutesLeft = Math.Truncate(60 - (minutes % 60));
                                        double hoursLeft = Math.Truncate(24 - (totalHours));
                                        twitchClient.QueueWhisper(whisperer, "Your daily Group Finder reward resets in " + hoursLeft + " hours and " + minutesLeft + " minutes.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Daily command failed as user not found in class list, or no parties exist.", whisperSender);
                                }
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
                            else if (whisperMessage == "!classes")
                            {
                                if (wolfcoins.classList != null)
                                {
                                    double numClasses = 0;
                                    double numWarriors = 0;
                                    double numMages = 0;
                                    double numRogues = 0;
                                    double numRangers = 0;
                                    double numClerics = 0;
                                    foreach (var member in wolfcoins.classList)
                                    {
                                        numClasses++;
                                        switch (member.Value.classType)
                                        {
                                            case CharClass.WARRIOR:
                                                {
                                                    numWarriors++;
                                                }
                                                break;

                                            case CharClass.MAGE:
                                                {
                                                    numMages++;
                                                }
                                                break;

                                            case CharClass.ROGUE:
                                                {
                                                    numRogues++;
                                                }
                                                break;

                                            case CharClass.RANGER:
                                                {
                                                    numRangers++;
                                                }
                                                break;

                                            case CharClass.CLERIC:
                                                {
                                                    numClerics++;
                                                }
                                                break;

                                            default: break;
                                        }


                                    }

                                    double percentWarriors = (numWarriors / numClasses) * 100;
                                    percentWarriors = Math.Round(percentWarriors, 1);
                                    double percentMages = (numMages / numClasses) * 100;
                                    percentMages = Math.Round(percentMages, 1);
                                    double percentRogues = (numRogues / numClasses) * 100;
                                    percentRogues = Math.Round(percentRogues, 1);
                                    double percentRangers = (numRangers / numClasses) * 100;
                                    percentRangers = Math.Round(percentRangers, 1);
                                    double percentClerics = (numClerics / numClasses) * 100;
                                    percentClerics = Math.Round(percentClerics, 1);

                                    Logger.Debug(">>{user}: Class command executed.", whisperSender);
                                    twitchClient.QueueWhisper(whisperer, "Class distribution for the Wolfpack RPG: ");
                                    twitchClient.QueueWhisper(whisperer, "Warriors: " + percentWarriors + "%");
                                    twitchClient.QueueWhisper(whisperer, "Mages: " + percentMages + "%");
                                    twitchClient.QueueWhisper(whisperer, "Rogues: " + percentRogues + "%");
                                    twitchClient.QueueWhisper(whisperer, "Rangers: " + percentRangers + "%");
                                    twitchClient.QueueWhisper(whisperer, "Clerics " + percentClerics + "%");
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Class command failed as class list is null.", whisperSender);
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
                            else if (whisperMessage == "!createparty")
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (!wolfcoins.classList[whisperSender].pendingInvite && wolfcoins.classList[whisperSender].groupID == -1)
                                    {
                                        if (wolfcoins.classList[whisperSender].queueDungeons.Count > 0)
                                        {
                                            twitchClient.QueueWhisper(whisperer, "Can't create a party while queued with the Group Finder. Message me '!leavequeue' to exit.");
                                            continue;
                                        }
                                        wolfcoins.classList[whisperSender].isPartyLeader = true;
                                        wolfcoins.classList[whisperSender].numInvitesSent = 1;
                                        Wolfcoins.Party myParty = new Wolfcoins.Party();
                                        myParty.status = PARTY_FORMING;
                                        myParty.partyLeader = whisperSender;
                                        int myLevel = wolfcoins.determineLevel(whisperSender);
                                        wolfcoins.classList[whisperSender].groupID = maxPartyID;
                                        myParty.AddMember(wolfcoins.classList[whisperSender]);
                                        myParty.myID = maxPartyID;
                                        parties.Add(maxPartyID, myParty);

                                        twitchClient.QueueWhisper(whisperer, "Party created! Use '!add <username>' to invite party members.");
                                        Logger.Info("Party created: ");
                                        Logger.Info("ID: {id}", maxPartyID);
                                        Logger.Info("Total number of parties: {count}", parties.Count());
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
                                        Logger.Debug(">>{user}: Create party command failed as user already has a party. {reason}", whisperSender, reason);
                                        twitchClient.QueueWhisper(whisperer, "You already have a party created! " + reason);
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Create party command failed as user has an outstanding invite.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You currently have an outstanding invite to another party. Couldn't create new party!");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Create party command failed as user is not in class list.", whisperSender);
                                }
                            }

                            else if (whisperMessage.Equals("nevermind", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                {
                                    if (wolfcoins.classList[whisperSender].classType >= 10)
                                    {

                                        int oldClass = wolfcoins.classList[whisperSender].classType / 10;
                                        wolfcoins.classList[whisperSender].classType = oldClass;
                                        Logger.Debug(">>{user}: Nevermind command executed, respec cancelled.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "Respec cancelled. No Wolfcoins deducted from your balance.");
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Nevermind command failed as class type <= 10.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Nevermind command failed as user is not in class list.", whisperSender);
                                }
                            }

                            else if (whisperMessage == "!partydata")
                            {
                                if (parties.Count() > 0)
                                {
                                    if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                    {
                                        int myID = wolfcoins.classList[whisperSender].groupID;
                                        if (myID != -1)
                                        {
                                            string partyMembers = "";
                                            if (parties.ContainsKey(myID))
                                            {
                                                for (int i = 0; i < parties[myID].members.Count(); i++)
                                                {
                                                    partyMembers += parties[myID].members.ElementAt(i).name + " ";
                                                }
                                                string status = "";
                                                switch (parties[myID].status)
                                                {
                                                    case PARTY_FORMING:
                                                        {
                                                            status = "PARTY_FORMING";
                                                        }
                                                        break;

                                                    case PARTY_READY:
                                                        {
                                                            status = "PARTY_READY";
                                                        }
                                                        break;

                                                    case PARTY_STARTED:
                                                        {
                                                            status = "PARTY_STARTED";
                                                        }
                                                        break;

                                                    case PARTY_COMPLETE:
                                                        {
                                                            status = "PARTY_COMPLETE";
                                                        }
                                                        break;

                                                    default:
                                                        {
                                                            status = "UNKNOWN_STATUS";
                                                        }
                                                        break;
                                                }
                                                Logger.Debug(">>{user}: Party data command executed.", whisperSender);
                                                ircClient.QueueMessage(whisperSender + " requested his Party Data. Group ID: " + wolfcoins.classList[whisperSender].groupID + "; Members: " + partyMembers + "; Status: " + status);
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Party data command failed as user's party id is not is group list.", whisperSender);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Party data command failed as user is not in a party.", whisperSender);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Party data command failed as user is not in class list.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Party data command failed as there are no parties.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!clearitems"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Clear items command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length > 1)
                                {
                                    string target = whisperMSG[1];
                                    if (wolfcoins.Exists(wolfcoins.classList, target))
                                    {
                                        wolfcoins.classList[target].totalItemCount = 0;
                                        wolfcoins.classList[target].myItems = new List<Item>();
                                        wolfcoins.SaveClassData();
                                        Logger.Debug(">>{user}: Clear items executed against {target}.", whisperSender, target);
                                        twitchClient.QueueWhisper(whisperer, "Cleared " + target + "'s item list.");
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Clear items command failed as target {target} is not in class list.", whisperSender, target);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Clear items command failed as no parameter was provided.", whisperSender);
                                }
                            }
                            else if (whisperMessage == "!fixstats")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Fix stats command not authorized.", whisperSender);
                                    continue;
                                }

                                if (wolfcoins.classList != null)
                                {
                                    foreach (var user in wolfcoins.classList)
                                    {
                                        if (user.Value.name == "NAMELESS ONE")
                                        {
                                            Logger.Debug(">>{user}: Fix stats command failed as user has not selected a class.", whisperSender);
                                            continue;
                                        }

                                        int classType = wolfcoins.classList[user.Value.name].classType;
                                        CharClass defaultClass;
                                        switch (classType)
                                        {
                                            case CharClass.WARRIOR:
                                                {
                                                    defaultClass = new Warrior();
                                                }
                                                break;

                                            case CharClass.MAGE:
                                                {
                                                    defaultClass = new Mage();
                                                }
                                                break;

                                            case CharClass.ROGUE:
                                                {
                                                    defaultClass = new Rogue();
                                                }
                                                break;

                                            case CharClass.RANGER:
                                                {
                                                    defaultClass = new Ranger();
                                                }
                                                break;

                                            case CharClass.CLERIC:
                                                {
                                                    defaultClass = new Cleric();
                                                }
                                                break;

                                            default:
                                                {
                                                    defaultClass = new CharClass();
                                                }
                                                break;
                                        }

                                        wolfcoins.classList[user.Value.name].coinBonus = defaultClass.coinBonus;
                                        wolfcoins.classList[user.Value.name].xpBonus = defaultClass.xpBonus;
                                        wolfcoins.classList[user.Value.name].itemFind = defaultClass.itemFind;
                                        wolfcoins.classList[user.Value.name].successChance = defaultClass.successChance;
                                        wolfcoins.classList[user.Value.name].preventDeathBonus = defaultClass.preventDeathBonus;
                                        wolfcoins.classList[user.Value.name].itemEarned = -1;
                                        wolfcoins.classList[user.Value.name].ClearQueue();
                                        Logger.Debug(">>{user}: Fix stats command executed on {target}.", whisperSender, user.Value.name);
                                    }
                                    wolfcoins.SaveClassData();
                                    twitchClient.QueueWhisper(whisperer, "Reset all user's stats to default.");
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Fix stats command failed as class list is null.", whisperSender);
                                }

                            }
                            else if (whisperMessage.StartsWith("!giveitem"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Give item command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length > 2)
                                {
                                    string temp = whisperMSG[2];
                                    int id = -1;
                                    int.TryParse(temp, out id);
                                    if (id < 1 || id > itemDatabase.Count())
                                    {
                                        Logger.Debug(">>{user}: Give item command failed due to invalid item id {id}.", whisperSender, id);
                                        twitchClient.QueueWhisper(whisperer, "Invalid ID was attempted to be given.");
                                    }

                                    string user = whisperMSG[1];
                                    bool hasItem = false;
                                    if (wolfcoins.Exists(wolfcoins.classList, user))
                                    {
                                        if (wolfcoins.classList[user].myItems.Count > 0)
                                        {

                                            foreach (var item in wolfcoins.classList[user].myItems)
                                            {
                                                if (item.itemID == id)
                                                {
                                                    hasItem = true;
                                                    Logger.Debug(">>{user}: Give item command failed as user already has the given item {item}.", whisperSender, itemDatabase[id - 1].itemName);
                                                    twitchClient.QueueWhisper(whisperer, user + " already has " + itemDatabase[id - 1].itemName + ".");
                                                }
                                            }

                                        }
                                        if (!hasItem && itemDatabase.ContainsKey(id))
                                        {
                                            GrantItem(id, wolfcoins, user, itemDatabase);
                                            wolfcoins.SaveClassData();
                                            //if(wolfcoins.classList[user].totalItemCount != -1)
                                            //{
                                            //    wolfcoins.classList[user].totalItemCount++;
                                            //}
                                            //else
                                            //{
                                            //    wolfcoins.classList[user].totalItemCount = 1;
                                            //}
                                            //wolfcoins.classList[user].myItems.Add(itemDatabase[id]);
                                            Logger.Debug(">>{user}: Give item command executed. User {user} given item {item}.", whisperSender, user, itemDatabase[id - 1].itemName);
                                            twitchClient.QueueWhisper(whisperer, "Gave " + user + " a " + itemDatabase[id - 1].itemName + ".");
                                        }

                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Give item command failed due to invalid parameter count. Expected 2, actual {count}.", whisperSender, whisperMSG.Length - 1);
                                }
                            }
                            // player requests to equip an item. make sure they actually *have* items
                            // check if their item is equippable ('other' type items are not), and that it isn't already active
                            // set it to true, then iterate through item list. if inventoryID does *not* match and it *IS* active, deactivate it
                            else if (whisperMessage.StartsWith("!activate") || whisperMessage.StartsWith("activate") || whisperMessage.StartsWith("!equip")
                                || whisperMessage.StartsWith("equip"))
                            {
                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length > 1)
                                {

                                    string temp = whisperMSG[1];
                                    int id = -1;
                                    int.TryParse(temp, out id);
                                    if (wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                    {
                                        if (wolfcoins.classList[whisperSender].myItems.Count > 0)
                                        {
                                            Item toActivate = wolfcoins.classList[whisperSender].GetItem(id);
                                            int itemPos = wolfcoins.classList[whisperSender].GetItemPos(id);

                                            if (toActivate.itemType == Item.TYPE_ARMOR || toActivate.itemType == Item.TYPE_WEAPON)
                                            {
                                                if (toActivate.isActive)
                                                {
                                                    Logger.Debug(">>{user}: Equip command failed as item is already equipped.", whisperSender, toActivate.itemType, toActivate.itemName);
                                                    twitchClient.QueueWhisper(whisperer, toActivate.itemName + " is already equipped.");
                                                    continue;
                                                }
                                                wolfcoins.classList[whisperSender].myItems.ElementAt(itemPos).isActive = true;
                                                foreach (var itm in wolfcoins.classList[whisperSender].myItems)
                                                {
                                                    if (itm.inventoryID == id)
                                                        continue;

                                                    if (itm.itemType != toActivate.itemType)
                                                        continue;

                                                    if (itm.isActive)
                                                    {
                                                        itm.isActive = false;
                                                        Logger.Debug(">>{user}: Item {item} unequipped by equip command.", whisperSender, toActivate.itemType, toActivate.itemName);
                                                        twitchClient.QueueWhisper(whisperer, "Unequipped " + itm.itemName + ".");
                                                    }
                                                }
                                                Logger.Debug(">>{user}: Equip command executed. User equipped {item}.", whisperSender, toActivate.itemType, toActivate.itemName);
                                                twitchClient.QueueWhisper(whisperer, "Equipped " + toActivate.itemName + ".");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Equip command failed as item type {type} cannot be equipped.", whisperSender, toActivate.itemType);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Equip command failed as user has no items.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You have no items.");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Equip command failed as user is not in class list.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Equip command failed as no parameter was provided.", whisperSender);
                                }
                            }
                            // player requests to unequip an item. make sure they actually *have* items
                            // check if their item is equippable ('other' type items are not), and that it isn't already inactive
                            else if (whisperMessage.StartsWith("!deactivate") || whisperMessage.StartsWith("deactivate") || whisperMessage.StartsWith("!unequip")
                            || whisperMessage.StartsWith("unequip"))
                            {
                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length > 1)
                                {

                                    string temp = whisperMSG[1];
                                    int id = -1;
                                    int.TryParse(temp, out id);
                                    if (wolfcoins.Exists(wolfcoins.classList, whisperSender) && id != -1)
                                    {
                                        if (wolfcoins.classList[whisperSender].myItems.Count > 0)
                                        {
                                            Item toDeactivate = wolfcoins.classList[whisperSender].GetItem(id);
                                            int itemPos = wolfcoins.classList[whisperSender].GetItemPos(id);

                                            if (toDeactivate.itemType == Item.TYPE_ARMOR || toDeactivate.itemType == Item.TYPE_WEAPON)
                                            {
                                                if (toDeactivate.isActive)
                                                {
                                                    wolfcoins.classList[whisperSender].myItems.ElementAt(itemPos).isActive = false;
                                                    Logger.Debug(">>{user}: Unequip command executed, item {item} unequipped.", whisperSender, toDeactivate.itemName);
                                                    twitchClient.QueueWhisper(whisperer, "Unequipped " + toDeactivate.itemName + ".");
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Unequip command failed as item {item} was not equipped.", whisperSender, toDeactivate.itemName);
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Unequip command failed as item type {type} cannot be equipped.", whisperSender, toDeactivate.itemType);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Unequip command failed as user has no items.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You have no items.");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Unequip command failed as user is not in class list or parameter {parameter} failed to parse.", whisperSender, temp);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Unequip command failed as no parameter was provided.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!printinfo"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Print info command not authorized.", whisperSender);
                                    continue;
                                }
                                // first[1] is the user to print info for
                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length >= 2)
                                {
                                    string player = whisperMSG[1].ToString();
                                    if (wolfcoins.Exists(wolfcoins.classList, player))
                                    {

                                        // print out all the user's info

                                        int numItems = wolfcoins.classList[player].totalItemCount;

                                        Logger.Debug(">>{user}: Print info command executed.", whisperSender);
                                        Logger.Info("Name: {user}", player);
                                        Logger.Info("Level: {level}", wolfcoins.classList[player].level);
                                        Logger.Info("Prestige Level: {prestige}", wolfcoins.classList[player].prestige);
                                        Logger.Info("Class: {className}", wolfcoins.classList[player].className);
                                        Logger.Info("Dungeon success chance: {chance}", wolfcoins.classList[player].GetTotalSuccessChance());
                                        Logger.Info("Number of Items: {count}", numItems);
                                        Logger.Info(wolfcoins.classList[player].PrintItems());

                                    }
                                    else
                                    {
                                        Logger.Info(">>{user}: Print info command failed, player name {user} not found.", whisperSender, player);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Print info command failed due to missing parameter.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!setxp"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Set xp command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length > 2)
                                {
                                    if (int.TryParse(whisperMSG[2], out int value))
                                    {
                                        userSystem.GetUserByNameAsync(whisperMSG[1], (user) =>
                                        {
                                            int newXp = wolfcoins.SetXP(value, user, twitchClient);
                                            if (newXp != -1)
                                            {
                                                Logger.Debug(">>{user}: Set xp command executed. User {user} xp set to {xp}.", whisperSender, whisperMSG[1], newXp);
                                                twitchClient.QueueWhisper(whisperer, "Set " + whisperMSG[1] + "'s XP to " + newXp + ".");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Set xp command failed.", whisperSender);
                                                twitchClient.QueueWhisper(whisperer, "Error updating XP amount.");
                                            }
                                        });
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Set xp command failed as parameter {parameter} failed to parse as int.", whisperSender, whisperMSG[2]);
                                        twitchClient.QueueWhisper(whisperer, "Invalid data provided for !setxp command.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Set xp command failed due to missing parameters.", whisperSender);
                                    twitchClient.QueueWhisper(whisperer, "Not enough data provided for !setxp command.");
                                }
                            }
                            else if (whisperMessage.StartsWith("!setprestige"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Set prestige command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length > 2)
                                {
                                    int value = -1;
                                    if (int.TryParse(whisperMSG[2], out value))
                                    {

                                        if (value != -1 && wolfcoins.classList.ContainsKey(whisperMSG[1]))
                                        {
                                            wolfcoins.classList[whisperMSG[1].ToString()].prestige = value;
                                            Logger.Debug(">>{user}: Set prestige command executed. User {user} prestige set to {xp}.", whisperSender, whisperMSG[1], value);
                                            twitchClient.QueueWhisper(whisperer, "Set " + whisperMSG[1] + "'s Prestige to " + value + ".");
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Set prestige command failed.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "Error updating Prestige Level.");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Set prestige command failed as parameter {parameter} failed to parse as int.", whisperSender, whisperMSG[2]);
                                        twitchClient.QueueWhisper(whisperer, "Invalid data provided for !setprestige command.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Set prestige command failed due to missing parameters.", whisperSender);
                                    twitchClient.QueueWhisper(whisperer, "Not enough data provided for !setprestige command.");
                                }
                            }
                            else if (whisperMessage.StartsWith("C") || whisperMessage.StartsWith("c"))
                            {
                                if (wolfcoins.classList != null)
                                {
                                    if (wolfcoins.classList.Keys.Contains(whisperSender.ToLower()))
                                    {
                                        if (wolfcoins.classList[whisperSender].classType == -1)
                                        {
                                            Logger.Debug(">>{user}: {command} command executed, user's class set.", whisperSender, whisperMessage);
                                            wolfcoins.SetClass(whisperer, whisperMessage, twitchClient);
                                        }

                                        if (wolfcoins.classList[whisperSender].classType.ToString().EndsWith("0"))
                                        {
                                            char c = whisperMessage.Last();
                                            int newClass = -1;
                                            int.TryParse(c.ToString(), out newClass);
                                            Logger.Debug(">>{user}: {command} command executed, user's class changed.", whisperSender, whisperMessage);
                                            wolfcoins.ChangeClass(whisperSender, newClass, userSystem, twitchClient);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: {command} command failed as user is not in class list.", whisperSender, whisperMessage);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: {command} command failed as class list is null.", whisperSender, whisperMessage);
                                }
                            }
                            // 
                            //
                            //               COMMANDS TO FIX STUFF
                            //
                            //
                            else if (whisperMessage == "!patch1")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Patch 1 command not authorized.", whisperSender);
                                    continue;
                                }

                                if (wolfcoins.xpList != null)
                                {
                                    CharClass emptyClass = new CharClass();
                                    emptyClass.classType = -1;
                                    emptyClass.totalItemCount = -1;

                                    Logger.Debug(">>{user}: Patch 1 command executed.", whisperSender);
                                    foreach (var viewer in wolfcoins.xpList)
                                    {
                                        int myLevel = wolfcoins.determineLevel(viewer.Key);
                                        if (myLevel >= 3 && !wolfcoins.classList.ContainsKey(viewer.Key))
                                        {
                                            emptyClass.name = viewer.Key;
                                            emptyClass.level = myLevel;
                                            wolfcoins.classList.Add(viewer.Key, emptyClass);
                                            Logger.Info("Added {user} to the Class List.", viewer.Key);
                                        }
                                    }
                                    wolfcoins.SaveClassData();
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Patch 1 command failed as xp list is null.", whisperSender);
                                }
                            }
                            // command to fix multiple inventory ids and active states
                            else if (whisperMessage == "!fixinventory")
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Fix inventory command not authorized.", whisperSender);
                                    continue;
                                }

                                Logger.Debug(">>{user}: Fix inventory command executed.", whisperSender);
                                foreach (var player in wolfcoins.classList)
                                {
                                    player.Value.FixItems();
                                }

                                //Console.WriteLine(wolfcoins.classList["kraad_"].FixItems());

                                wolfcoins.SaveClassData();
                            }

                            else if (whisperMessage == "1")
                            {
                                Logger.Debug(">>{user}: \"1\" command executed.", whisperSender);
                                twitchClient.QueueWhisper(whisperer, "Wolfcoins are a currency you earn by watching the stream! You can check your coins by whispering me '!coins' or '!stats'. To find out what you can spend coins on, message me '!shop'.");
                            }

                            else if (whisperMessage == "2")
                            {
                                Logger.Debug(">>{user}: \"2\" command executed.", whisperSender);
                                twitchClient.QueueWhisper(whisperer, "Did you know you gain experience by watching the stream? You can level up as you get more XP! Max level is 20. To check your level & xp, message me '!xp' '!level' or '!stats'. Only Level 2+ viewers can post links. This helps prevent bot spam!");
                            }

                            else if (whisperMessage == "!shop")
                            {
                                Logger.Debug(">>{user}: shop command executed.", whisperSender);
                                twitchClient.QueueWhisper(whisperer, "Whisper me '!stats <username>' to check another users stats! (Cost: 1 coin)   Whisper me '!gloat' to spend 10 coins and show off your level! (Cost: 10 coins)");
                            }

                            else if (whisperMessage == "!dungeonlist")
                            {
                                Logger.Debug(">>{user}: Dungeon list command executed.", whisperSender);
                                twitchClient.QueueWhisper(whisperer, "List of Wolfpack RPG Adventures: http://tinyurl.com/WolfpackAdventureList");
                            }
                            else if (whisperMessage.StartsWith("!debuglevel5"))
                            {
                                if (whisperSender == tokenData.BroadcastUser || whisperSender == tokenData.ChatUser)
                                {
                                    string[] whisperMSG = whisperMessage.Split(' ');
                                    if (whisperMSG.Length > 1)
                                    {
                                        string userName = whisperMSG[1];
                                        if (wolfcoins.Exists(wolfcoins.classList, userName))
                                        {
                                            Logger.Debug(">>{user}: Debug level 5 command executed on existing user {target}.", whisperSender, userName);
                                            wolfcoins.classList.Remove(userName);
                                            userSystem.GetUserByNameAsync(userName, (user) =>
                                            {
                                                wolfcoins.SetXP(1, user, twitchClient);
                                                wolfcoins.SetXP(600, user, twitchClient);
                                            });
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Debug level 5 command executed on non-existing user {target}.", whisperSender, userName);
                                            userSystem.GetUserByNameAsync(userName, (user) =>
                                            {
                                                wolfcoins.SetXP(1, user, twitchClient);
                                                wolfcoins.SetXP(600, user, twitchClient);
                                            });
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Debug level 5 command failed due to missing parameter.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Debug level 5 command not authorized.", whisperSender);
                                }
                            }

                            else if (whisperMessage.StartsWith("!clearclass"))
                            {
                                if (whisperSender == tokenData.BroadcastUser || whisperSender == tokenData.ChatUser)
                                {
                                    if (wolfcoins.classList != null)
                                    {
                                        string[] whisperMSG = whisperMessage.Split(' ');
                                        if (whisperMSG.Length > 1)
                                        {
                                            string user = whisperMSG[1];
                                            if (wolfcoins.classList.Keys.Contains(user.ToLower()))
                                            {
                                                wolfcoins.classList.Remove(user);
                                                wolfcoins.SaveClassData();
                                                Logger.Debug(">>{user}: Clear class command executed on {target}.", whisperSender, user);
                                                twitchClient.QueueWhisper(whisperer, "Cleared " + user + "'s class.");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Clear class command failed as {target} was not in class list.", whisperSender, user);
                                                twitchClient.QueueWhisper(whisperer, "Couldn't find you in the class table.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Clear class command failed due to missing parameter.", whisperSender);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Clear class command failed as class list is null.", whisperSender);
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Clear class command not authorized.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!setcoins"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Set coins command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] whisperMSG = whisperMessage.Split(' ');
                                if (whisperMSG.Length > 2)
                                {
                                    if (int.TryParse(whisperMSG[2], out int value))
                                    {
                                        if (!wolfcoins.Exists(wolfcoins.coinList, whisperMSG[1]))
                                        {
                                            wolfcoins.coinList.Add(whisperMSG[1], 0);
                                        }
                                        int newCoins = wolfcoins.SetCoins(value, whisperMSG[1]);
                                        if (newCoins != -1)
                                        {
                                            Logger.Debug(">>{user}: Set coins command executed. User {target} coins set to {coins}.", whisperSender, whisperMSG[1], newCoins);
                                            twitchClient.QueueWhisper(whisperer, "Set " + whisperMSG[1] + "'s coins to " + newCoins + ".");
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Set coins command failed.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "Error updating Coin amount.");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Set coins command failed due to parameter {parameter} failing to parse as int.", whisperSender, whisperMSG[2]);
                                        twitchClient.QueueWhisper(whisperer, "Invalid data provided for !setcoins command.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Set coins command failed due to invalid parameter count. Expected 2, actual {count}.", whisperSender, whisperMSG.Length - 1);
                                    twitchClient.QueueWhisper(whisperer, "Not enough data provided for !setcoins command.");
                                }
                            }

                            if (whisperMessage == "!coins" || whisperMessage == "coins")
                            {
                                if (wolfcoins.coinList != null)
                                {
                                    Logger.Debug(">>{user}: Coins command executed.", whisperSender);
                                    if (wolfcoins.coinList.ContainsKey(whisperSender))
                                    {
                                        twitchClient.QueueWhisper(whisperer, "You have: " + wolfcoins.coinList[whisperSender] + " coins.");
                                    }
                                    else
                                    {
                                        twitchClient.QueueWhisper(whisperer, "You don't have any coins yet! Stick around during the livestream to earn coins.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Coins command failed as coin list is null.", whisperSender);
                                }
                            }
                            else if (whisperMessage.StartsWith("!gloatpet") || whisperMessage.StartsWith("!petgloat"))
                            {
                                if (wolfcoins.classList.ContainsKey(whisperSender))
                                {
                                    if (wolfcoins.coinList[whisperSender] < gloatCost)
                                    {
                                        Logger.Debug(">>{user}: Gloat pet command failed as user has insufficient coins. Need {cost}, has {coins}.", whisperSender, gloatCost, wolfcoins.coinList[whisperSender]);
                                        twitchClient.QueueWhisper(whisperer, "You don't have enough coins to gloat!");
                                        continue;
                                    }

                                    if (wolfcoins.classList[whisperSender].myPets.Count > 0)
                                    {
                                        bool hasActive = false;
                                        Pet toGloat = new Pet();
                                        foreach (var pet in wolfcoins.classList[whisperSender].myPets)
                                        {
                                            if (pet.isActive)
                                            {
                                                hasActive = true;
                                                toGloat = pet;
                                                break;
                                            }
                                        }

                                        if (!hasActive)
                                        {
                                            Logger.Debug(">>{user}: Gloat pet command failed as user has no active pet.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You don't have an active pet to show off! Activate one with !summon <id>");
                                            continue;
                                        }

                                        wolfcoins.RemoveCoins(whisperSender, gloatCost);


                                        string petType = "";
                                        if (toGloat.isSparkly)
                                            petType += "SPARKLY " + toGloat.type;
                                        else
                                            petType = toGloat.type;

                                        Logger.Debug(">>{user}: Gloat pet command executed for pet {pet}.", whisperSender, toGloat.name);
                                        ircClient.QueueMessage($"{whisperSender} watches proudly as their level {toGloat.level} {petType} named {toGloat.name} struts around!");
                                        twitchClient.QueueWhisper(whisperer, $"You spent {gloatCost} wolfcoins to brag about {toGloat.name}.");
                                    }
                                }
                            }
                            else if (whisperMessage.StartsWith("!gloat") || whisperMessage.StartsWith("gloat"))
                            {
                                if (wolfcoins.coinList != null && wolfcoins.xpList != null)
                                {
                                    if (wolfcoins.coinList.ContainsKey(whisperSender) && wolfcoins.xpList.ContainsKey(whisperSender))
                                    {

                                        if (wolfcoins.coinList[whisperSender] >= gloatCost)
                                        {
                                            string gloatMessage = "";
                                            int level = wolfcoins.determineLevel(whisperSender);
                                            string levelWithPrestige = wolfcoins.gloatWithPrestige(whisperSender);
                                            wolfcoins.RemoveCoins(whisperSender, gloatCost);
                                            #region gloatMessages
                                            switch (level)
                                            {
                                                case 1:
                                                    {
                                                        gloatMessage = "Just a baby! lobosMindBlank";
                                                    }
                                                    break;

                                                case 2:
                                                    {
                                                        gloatMessage = "Scrubtastic!";
                                                    }
                                                    break;

                                                case 3:
                                                    {
                                                        gloatMessage = "Pretty weak!";
                                                    }
                                                    break;

                                                case 4:
                                                    {
                                                        gloatMessage = "Not too shabby.";
                                                    }
                                                    break;

                                                case 5:
                                                    {
                                                        gloatMessage = "They can hold their own!";
                                                    }
                                                    break;

                                                case 6:
                                                    {
                                                        gloatMessage = "Getting pretty strong Kreygasm";
                                                    }
                                                    break;

                                                case 7:
                                                    {
                                                        gloatMessage = "A formidable opponent!";
                                                    }
                                                    break;

                                                case 8:
                                                    {
                                                        gloatMessage = "A worthy adversary!";
                                                    }
                                                    break;

                                                case 9:
                                                    {
                                                        gloatMessage = "A most powerful combatant!";
                                                    }
                                                    break;

                                                case 10:
                                                    {
                                                        gloatMessage = "A seasoned war veteran!";
                                                    }
                                                    break;

                                                case 11:
                                                    {
                                                        gloatMessage = "A fearsome champion of the Wolfpack!";
                                                    }
                                                    break;

                                                case 12:
                                                    {
                                                        gloatMessage = "A vicious pack leader!";
                                                    }
                                                    break;

                                                case 13:
                                                    {
                                                        gloatMessage = "A famed Wolfpack Captain!";
                                                    }
                                                    break;

                                                case 14:
                                                    {
                                                        gloatMessage = "A brutal commander of the Wolfpack!";
                                                    }
                                                    break;

                                                case 15:
                                                    {
                                                        gloatMessage = "Decorated Chieftain of the Wolfpack!";
                                                    }
                                                    break;

                                                case 16:
                                                    {
                                                        gloatMessage = "A War Chieftain of the Wolfpack!";
                                                    }
                                                    break;

                                                case 17:
                                                    {
                                                        gloatMessage = "A sacred Wolfpack Justicar!";
                                                    }
                                                    break;

                                                case 18:
                                                    {
                                                        gloatMessage = "Demigod of the Wolfpack!";
                                                    }
                                                    break;

                                                case 19:
                                                    {
                                                        gloatMessage = "A legendary Wolfpack demigod veteran!";
                                                    }
                                                    break;

                                                case 20:
                                                    {
                                                        gloatMessage = "The Ultimate Wolfpack God Rank. A truly dedicated individual.";
                                                    }
                                                    break;

                                                default: break;
                                            }
                                            #endregion

                                            Logger.Debug(">>{user}: Gloat command executed.", whisperSender, gloatCost, wolfcoins.coinList[whisperSender]);
                                            ircClient.QueueMessage($"{whisperSender} has spent {gloatCost} Wolfcoins to show off that they are {levelWithPrestige}! {gloatMessage}");
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Gloat command failed as user has insufficient coins. Need {cost}, has {coins}.", whisperSender, gloatCost, wolfcoins.coinList[whisperSender]);
                                            twitchClient.QueueWhisper(whisperer, $"You don't have enough coins to gloat (Cost: {gloatCost} Wolfcoins)");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Gloat command failed as user has no coins or xp.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You don't have coins and/or xp yet!");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Gloat command failed as coin list or xp list are null.", whisperSender);
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
                            else if (whisperMessage.StartsWith("!givexp"))
                            {
                                if (whisperSender != tokenData.BroadcastUser && whisperSender != tokenData.ChatUser)
                                {
                                    Logger.Debug(">>{user}: Give xp command not authorized.", whisperSender);
                                    continue;
                                }

                                string[] whisperMSG = whisperMessage.Split(' ');

                                if (whisperMSG[0] != null && whisperMSG[1] != null && whisperMSG[2] != null)
                                {
                                    if (!(int.TryParse(whisperMSG[2].ToString(), out int value)))
                                    {
                                        Logger.Debug(">>{user}: Give xp command failed due to parameter {parameter} failing to parse as int.", whisperSender, whisperMSG[2].ToString());
                                        break;
                                    }
                                    string user = whisperMSG[1].ToString();
                                    userSystem.GetUserByNameAsync(user, (userObj) => { wolfcoins.AwardXP(value, userObj, twitchClient); });
                                    wolfcoins.SaveClassData();
                                    wolfcoins.SaveXP();
                                    Logger.Debug(">>{user}: Give xp command executed. User {target} given {xp} xp.", whisperSender, user, value);
                                    twitchClient.QueueWhisper(whisperer, "Gave " + user + " " + value + " XP.");
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Give xp command failed due to missing parameters.", whisperSender);
                                    ircClient.QueueMessage("Not enough data provided for !givexp command.");
                                }
                            }
                            else if (whisperMessage.StartsWith("!xp") || whisperMessage.StartsWith("xp") || whisperMessage.StartsWith("level") || whisperMessage.StartsWith("!level") ||
                                whisperMessage.StartsWith("!lvl") || whisperMessage.StartsWith("lvl"))
                            {
                                if (wolfcoins.xpList != null)
                                {
                                    if (wolfcoins.xpList.ContainsKey(whisperSender))
                                    {

                                        int myLevel = wolfcoins.determineLevel(whisperSender);
                                        int xpToNextLevel = wolfcoins.XpToNextLevel(whisperSender);
                                        int myPrestige = -1;
                                        if (wolfcoins.classList.ContainsKey(whisperSender))
                                        {
                                            myPrestige = wolfcoins.classList[whisperSender].prestige;
                                        }
                                        //if(!wolfcoins.Exists(wolfcoins.classList, whisperSender))
                                        //{
                                        //    Whisper(whisperSender, "You are Level " + myLevel + " (Total XP: " + wolfcoins.xpList[whisperSender] + ")", group);
                                        //}
                                        //else
                                        //{
                                        //    string myClass = wolfcoins.determineClass(whisperSender);
                                        //    Whisper(whisperSender, "You are a Level " + myLevel + " " + myClass + " (Total XP: " + wolfcoins.xpList[whisperSender] + ")", group);
                                        //}

                                        if (wolfcoins.classList.Keys.Contains(whisperSender.ToLower()))
                                        {
                                            string myClass = wolfcoins.determineClass(whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You are a Level " + myLevel + " " + myClass + ", and you are Prestige Level " + myPrestige + ". (Total XP: " + wolfcoins.xpList[whisperSender] + " | XP To Next Level: " + xpToNextLevel + ")");
                                        }
                                        else
                                        {
                                            twitchClient.QueueWhisper(whisperer, "You are Level " + myLevel + " (Total XP: " + wolfcoins.xpList[whisperSender] + " | XP To Next Level: " + xpToNextLevel + ")");
                                        }
                                        Logger.Debug(">>{user}: Level command executed.", whisperSender);
                                    }
                                    else
                                    {
                                        Logger.Debug(">>{user}: Level command failed as user has no xp.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You don't have any XP yet! Hang out in chat during the livestream to earn XP & coins.");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Level command failed as xp list is null.", whisperSender);
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
                            else if (whisperMessage.StartsWith("!stats") || whisperMessage.StartsWith("stats"))
                            {
                                if (wolfcoins.coinList != null && wolfcoins.xpList != null)
                                {
                                    string[] temp = whisperMessage.Split(' ');
                                    if (temp.Count() > 1)
                                    {
                                        string desiredUser = temp[1].ToLower();
                                        if (wolfcoins.xpList.ContainsKey(desiredUser) && wolfcoins.coinList.ContainsKey(desiredUser))
                                        {

                                            wolfcoins.RemoveCoins(whisperSender, pryCost);
                                            if (wolfcoins.Exists(wolfcoins.classList, desiredUser))
                                            {
                                                twitchClient.QueueWhisper(whisperer, "" + desiredUser + " is a Level " + wolfcoins.determineLevel(desiredUser) + " " + wolfcoins.determineClass(desiredUser) + " (" + wolfcoins.xpList[desiredUser] + " XP), Prestige Level " + wolfcoins.classList[desiredUser].prestige + ", and has " +
                                                    wolfcoins.coinList[desiredUser] + " Wolfcoins.");
                                            }
                                            else
                                            {
                                                twitchClient.QueueWhisper(whisperer, "" + desiredUser + " is Level " + " " + wolfcoins.determineLevel(desiredUser) + " (" + wolfcoins.xpList[desiredUser] + " XP) and has " +
                                                    wolfcoins.coinList[desiredUser] + " Wolfcoins.");
                                            }
                                            Logger.Debug(">>{user}: Stats command executed on {target}.", whisperSender, desiredUser);
                                            twitchClient.QueueWhisper(whisperer, "It cost you " + pryCost + " Wolfcoins to discover this information.");
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Stats command failed as user is not in xp or coin list.", whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "User does not exist in database. You were charged no coins.");
                                        }
                                    }
                                    else if (wolfcoins.coinList.ContainsKey(whisperSender) && wolfcoins.xpList.ContainsKey(whisperSender))
                                    {

                                        int myLevel = wolfcoins.determineLevel(whisperSender);
                                        int myPrestige = -1;

                                        if (wolfcoins.classList.ContainsKey(whisperSender))
                                        {
                                            myPrestige = wolfcoins.classList[whisperSender].prestige;
                                        }

                                        int xpToNextLevel = wolfcoins.XpToNextLevel(whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You have: " + wolfcoins.coinList[whisperSender] + " coins.");
                                        if (wolfcoins.classList.Keys.Contains(whisperSender.ToLower()))
                                        {
                                            string myClass = wolfcoins.determineClass(whisperSender);
                                            twitchClient.QueueWhisper(whisperer, "You are a Level " + myLevel + " " + myClass + ", and you are Prestige Level " + myPrestige + ". (Total XP: " + wolfcoins.xpList[whisperSender] + " | XP To Next Level: " + xpToNextLevel + ")");
                                        }
                                        else
                                        {
                                            twitchClient.QueueWhisper(whisperer, "You are Level " + myLevel + " (Total XP: " + wolfcoins.xpList[whisperSender] + " | XP To Next Level: " + xpToNextLevel + ")");
                                        }
                                        Logger.Debug(">>{user}: Stats command executed on {target}.", whisperSender, whisperSender);
                                    }
                                    if (!(wolfcoins.coinList.ContainsKey(whisperSender)) || !(wolfcoins.xpList.ContainsKey(whisperSender)))
                                    {
                                        Logger.Debug(">>{user}: Stats command failed as user is not in xp or coin list.", whisperSender);
                                        twitchClient.QueueWhisper(whisperer, "You either don't have coins or xp yet. Hang out in chat during the livestream to earn them!");
                                    }
                                }
                                else
                                {
                                    Logger.Debug(">>{user}: Stats command failed as xp or coin list are null.", whisperSender);
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
                            string[] first = message.Message.Split(' ');
                            string chatMessage = message.Message;
                            string sender = message.UserName;
                            string senderId = message.UserId;
                            var chatter = userSystem.GetOrCreateUser(message.UserId, message.UserName);

                            #region Trigger Processing
                            var triggerResult = triggerManager.ProcessTrigger(chatMessage, chatter);
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
                            #endregion

                            switch (first[0])
                            {
                                case "!nextaward":
                                    {
                                        if (sender.Equals(tokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase)
                                            || sender.Equals(tokenData.ChatUser, StringComparison.OrdinalIgnoreCase))
                                        {
                                            double totalSec = (DateTime.Now - awardLast).TotalSeconds;
                                            int timeRemaining = (awardInterval * 60) - (int)(DateTime.Now - awardLast).TotalSeconds;
                                            int secondsRemaining = timeRemaining % 60;
                                            int minutesRemaining = timeRemaining / 60;
                                            Logger.Debug(">>{user}: Returning {minutesRemaining} minutes, {secondsRemaining} seconds remaining.", sender, minutesRemaining, secondsRemaining);
                                            ircClient.QueueMessage(minutesRemaining + " minutes and " + secondsRemaining + " seconds until next coins/xp are awarded.");
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ignoring next award command.", sender);
                                        }
                                    }
                                    break;

                                case "!setinterval":
                                    {
                                        if (sender.Equals(tokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase)
                                            || sender.Equals(tokenData.ChatUser, StringComparison.OrdinalIgnoreCase))
                                        {
                                            int newAmount = 0;
                                            if (first.Length > 1)
                                            {
                                                if (int.TryParse(first[1], out newAmount))
                                                {
                                                    awardInterval = newAmount;
                                                    Logger.Debug(">>{user}: Set interval to {newAmount} for {user}.", sender, newAmount);
                                                    ircClient.QueueMessage("XP & Coins will now be awarded every " + newAmount + " minutes.");
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Failed to set interval, {param} failed to parse as integer.", sender, first[1]);
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Failed to set interval because parameter was missing.", sender, first[1]);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ignoring set interval command.", sender);
                                        }
                                    }
                                    break;

                                case "!setmultiplier":
                                    {
                                        if (sender.Equals(tokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase)
                                            || sender.Equals(tokenData.ChatUser, StringComparison.OrdinalIgnoreCase))
                                        {
                                            int newAmount = 0;
                                            if (first.Length > 1)
                                            {
                                                if (int.TryParse(first[1], out newAmount))
                                                {
                                                    awardMultiplier = newAmount;
                                                    ircClient.QueueMessage(newAmount + "x XP & Coins will now be awarded.");
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Failed to set multiplier, {param} failed to parse as integer.", sender, first[1]);
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Failed to set multiplier because parameter was missing.", sender, first[1]);
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ignoring set multiplier command.", sender);
                                        }
                                    }
                                    break;

                                //case "!endbet":
                                //    {
                                //        if (!betActive)
                                //            break;

                                //        if (wolfcoins.viewers.chatters.moderators.Contains(sender))
                                //        {
                                //            if (first.Count() > 1 && !betActive)
                                //            {
                                //                string result = first[1].ToLower();
                                //                if(result == "succeed")
                                //                {
                                //                    foreach(var element in betters)
                                //                    {
                                //                        //wolfcoins.AddCoins()
                                //                    }
                                //                }
                                //                else if(result == "fail")
                                //                {

                                //                }
                                //            }
                                //        }


                                //    } break;

                                //case "!closebet":
                                //    {
                                //        if(!betActive || !betsAllowed)
                                //            break;

                                //        if (wolfcoins.viewers.chatters.moderators.Contains(sender))
                                //        {
                                //            betsAllowed = false;
                                //            ircClient.QueueMessage("Bets are now closed! Good luck FrankerZ");
                                //        }
                                //    } break;

                                //case "!startbet":
                                //    {

                                //        betStatement = "";
                                //        if (wolfcoins.viewers.chatters.moderators.Contains(sender))
                                //        {
                                //            if (first.Count() > 1 && !betActive)
                                //            {
                                //                betActive = true;
                                //                betsAllowed = true;
                                //                for (int i = 0; i < first.Count() - 1; i++)
                                //                {
                                //                    betStatement += first[i + 1];
                                //                }
                                //                ircClient.QueueMessage("New bet started: " + betStatement + " Type '!bet succeed' or '!bet fail' to bet.");

                                //            }
                                //        }
                                //    } break;

                                case "!xpon":
                                    {
                                        if (chatter.IsAdmin || chatter.IsMod)
                                        {
                                            if (!broadcasting)
                                            {
                                                broadcasting = true;
                                                broadcastSetter = sender;
                                                awardLast = DateTime.Now;
                                                var tournamentSystem = systemManager.Get<TournamentSystem>();
                                                tournamentSystem.NextTournament = DateTime.Now.AddMinutes(15);
                                                isLive = true;
                                                CrashAlert();

                                                Logger.Debug(">>{user}: Xp turned on.", sender);
                                                ircClient.QueueMessage("Wolfcoins & XP will be awarded.");
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Xpon command ignored as xp already enabled by {broadcastSetting}.", sender, broadcastSetter);
                                                ircClient.QueueMessage($"XP has already been enabled by {broadcastSetter}");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Xpon command ignored from unauthorized sender. Only admins and mods can use this command.", sender);
                                        }
                                    }
                                    break;

                                case "!xpoff":
                                    {
                                        if (chatter.IsAdmin || chatter.IsMod)
                                        {
                                            if (broadcasting)
                                            {
                                                broadcasting = false;
                                                Logger.Debug(">>{user}: Xp turned off.", sender);
                                                ircClient.QueueMessage("Wolfcoins & XP will no longer be awarded.");
                                                wolfcoins.BackupData();
                                                isLive = false;
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Xpoff command ignored as xp already off.", sender);
                                                ircClient.QueueMessage("XP isn't on.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Xpoff command ignored from unauthorized sender. Only admins and mods can use this command.", sender);
                                        }
                                    }
                                    break;

                                case "!setxp":
                                    {
                                        if (sender.Equals(tokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase)
                                            || sender.Equals(tokenData.ChatUser, StringComparison.OrdinalIgnoreCase))
                                        {

                                            if (first.Length >= 3 && !string.IsNullOrWhiteSpace(first[1]) && !string.IsNullOrWhiteSpace(first[2]))
                                            {
                                                if (int.TryParse(first[2], out int value))
                                                {
                                                    userSystem.GetUserByNameAsync(first[1], (user) =>
                                                    {
                                                        int newXp = wolfcoins.SetXP(value, user, twitchClient);
                                                        if (newXp != -1)
                                                        {
                                                            Logger.Debug(">>{user}: Xp set to {xp} for {target}.", sender, value, user.Username);
                                                            ircClient.QueueMessage("Set " + user.Username + "'s XP to " + newXp + ".");
                                                        }
                                                        else
                                                        {
                                                            Logger.Debug(">>{user}: Unable to set xp to {xp} for {target}.", sender, value, user.Username);
                                                            ircClient.QueueMessage("Error updating XP amount.");
                                                        }
                                                    });
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Unable to parse xp amount {param} to set xp.", sender, first[2]);
                                                    ircClient.QueueMessage("Invalid data provided for !setxp command.");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Invalid number of parameters for set xp command. Expected 2, received {count}.", sender, first.Length - 1);
                                                ircClient.QueueMessage("Not enough data provided for !setxp command.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ignoring set xp command.", sender);
                                        }
                                    }
                                    break;

                                case "!grantxp":
                                    {
                                        if (sender.Equals(tokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase)
                                            || sender.Equals(tokenData.ChatUser, StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (first.Length >= 2 && !string.IsNullOrWhiteSpace(first[0]) && !string.IsNullOrWhiteSpace(first[1]))
                                            {
                                                if (int.TryParse(first[1], out int value))
                                                {
                                                    Logger.Debug(">>{user}: Granted {xp} xp to all viewers.", sender, value);
                                                    userSystem.GetUserByNameAsync(first[1], (user) => { wolfcoins.AwardXP(value, user, twitchClient); });
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Unable to parse xp amount {param} to grant xp.", sender, first[1]);
                                                    ircClient.QueueMessage("Invalid data provided for !givexp command.");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Invalid number of parameters for grant xp command. Expected 1, received {count}.", sender, first.Length - 1);
                                                ircClient.QueueMessage("Not enough data provided for !givexp command.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ignoring grant xp command.", sender);
                                        }
                                    }
                                    break;

                                case "!setcoins":
                                    {
                                        Logger.Debug(">>{user}: Set coins command not implemented.", sender);
                                    }
                                    break;

                                #region NormalBotStuff
                                //case "!hug":
                                //    {
                                //        ircClient.QueueMessage("/me gives " + sender + " a big hug!");
                                //    } break;

                                //case "!playlist":
                                //    {
                                //        ircClient.QueueMessage("Lobos' Spotify Playlist: http://open.spotify.com/user/1251282601/playlist/2j1FVSjJ4zdJiqGQgXgW3t");
                                //    } break;

                                //case "!opinion":
                                //    {
                                //        ircClient.QueueMessage("Opinions go here: http:////i.imgur.com/3jRQ2fa.jpg");
                                //    } break;

                                //case "!quote":
                                //    {
                                //        string path = @"C:\Users\Owner\Dropbox\Stream\quotes.txt";
                                //        string myFile = "";
                                //        if (File.Exists(path))
                                //        {
                                //            myFile = File.ReadAllText(path);
                                //            string[] quotes = myFile.Split('\n');
                                //            int numQuotes = quotes.Length;
                                //            Random random = new Random();
                                //            int randomNumber = random.Next(0, numQuotes);
                                //            ircClient.QueueMessage(quotes[randomNumber]);
                                //        }
                                //        else
                                //        {
                                //            ircClient.QueueMessage("Quotes file does not exist.");
                                //        }
                                //    } break;

                                //case "!pun":
                                //    {
                                //        string path = @"C:\Users\Owner\Dropbox\Stream\puns.txt";
                                //        string myFile = "";
                                //        if (File.Exists(path))
                                //        {
                                //            myFile = File.ReadAllText(path);
                                //            string[] puns = myFile.Split('\n');
                                //            int numPuns = puns.Length;
                                //            Random random = new Random();
                                //            int randomNumber = random.Next(0, numPuns);
                                //            ircClient.QueueMessage(puns[randomNumber]);
                                //        }
                                //        else
                                //        {
                                //            ircClient.QueueMessage("Puns file does not exist.");
                                //        }
                                //    } break;

                                //case "!whisper":
                                //    {
                                //        if (group.connected)
                                //        {
                                //            group.sendChatMessage(".w " + sender + " Psssssst!");
                                //        }
                                //    } break;
                                #endregion
                                // remove coins from a target. Ex: !removecoins lobosjr 200
                                case "!removecoins":
                                    {
                                        if (sender.Equals(tokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase)
                                            || sender.Equals(tokenData.ChatUser, StringComparison.OrdinalIgnoreCase))
                                        {

                                            if (first.Length >= 3 && first[1] != null && first[2] != null)
                                            {
                                                if (int.TryParse(first[2], out int value))
                                                {
                                                    if (wolfcoins.RemoveCoins(first[1], value))
                                                    {
                                                        Logger.Debug(">>{user}: Removed {coins} coins from {target}.", sender, value, first[1]);
                                                        ircClient.QueueMessage($"{sender} removed {value} coins from {first[1]}.");
                                                    }
                                                    else
                                                    {
                                                        Logger.Debug(">>{user}: Unable to remove {coins} coins from {target}.", sender, value, first[1]);
                                                    }
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Unable to parse coin amount {param} to remove coins.", sender, first[2]);
                                                    ircClient.QueueMessage("Invalid data provided for !removecoins command.");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Invalid number of parameters for remove coins command. Expected 2, received {count}.", sender, first.Length - 1);
                                                ircClient.QueueMessage("Not enough data provided for !removecoins command.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ignoring remove coins command.", sender);
                                        }
                                    }
                                    break;

                                case "!addcoins":
                                    {
                                        if (sender.Equals(tokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase)
                                            || sender.Equals(tokenData.ChatUser, StringComparison.OrdinalIgnoreCase))
                                        {

                                            if (first.Length >= 3 && first[1] != null && first[2] != null)
                                            {
                                                if (int.TryParse(first[2], out int value))
                                                {
                                                    userSystem.GetUserByNameAsync(first[1], (User user) =>
                                                    {
                                                        if (wolfcoins.AddCoins(user, value))
                                                        {
                                                            Logger.Debug(">>{user}: Granted {coins} coins to {target}.", sender, value, first[1]);
                                                            ircClient.QueueMessage($"{sender} granted {first[1]} {value} coins.");
                                                        }
                                                        else
                                                        {
                                                            Logger.Debug(">>{user}: Unable to add {coins} coins to {target}.", sender, value, first[1]);
                                                        }
                                                    });
                                                }
                                                else
                                                {
                                                    Logger.Debug(">>{user}: Unable to parse coin amount {param} to add coins.", sender, first[2]);
                                                    ircClient.QueueMessage("Invalid data provided for !addcoins command.");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Debug(">>{user}: Invalid number of parameters for add coins command. Expected 2, received {count}.", sender, first.Length - 1);
                                                ircClient.QueueMessage("Not enough data provided for !addcoins command.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug(">>{user}: Ignoring add coins command.", sender);
                                        }
                                    }
                                    break;

                                default: break;

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

        static void WhisperPet(string user, Pet pet, UserSystem userSystem, ITwitchClient twitchClient, int detail)
        {
            const int LOW_DETAIL = 0;
            const int HIGH_DETAIL = 1;

            string name = pet.name;
            int stableID = pet.stableID;
            string rarity = "";
            switch (pet.petRarity)
            {
                case (Pet.QUALITY_COMMON):
                    {
                        rarity = "Common";
                    }
                    break;

                case (Pet.QUALITY_UNCOMMON):
                    {
                        rarity = "Uncommon";
                    }
                    break;

                case (Pet.QUALITY_RARE):
                    {
                        rarity = "Rare";
                    }
                    break;

                case (Pet.QUALITY_EPIC):
                    {
                        rarity = "Epic";
                    }
                    break;

                case (Pet.QUALITY_ARTIFACT):
                    {
                        rarity = "Legendary";
                    }
                    break;

                default:
                    {
                        rarity = "Error";
                    }
                    break;
            }

            List<string> stats = new List<string>();
            if (detail == HIGH_DETAIL)
                stats.Add("Level: " + pet.level + " | Affection: " + pet.affection + " | Energy: " + pet.hunger);

            bool active = pet.isActive;
            string status = "";
            string myStableID = "";
            if (active)
            {
                status = "Active";
                myStableID = "<[" + pet.stableID + "]> ";
            }
            else
            {
                status = "In the Stable";
                myStableID = "[" + pet.stableID + "] ";
            }

            userSystem.GetUserByNameAsync(user, (userObj) =>
            {
                twitchClient.QueueWhisper(userObj, myStableID + name + " the " + pet.type + " (" + rarity + ") ");
                string sparkly = "";
                if (pet.isSparkly)
                    sparkly = "Yes!";
                else
                    sparkly = "No";
                if (detail == HIGH_DETAIL)
                    twitchClient.QueueWhisper(userObj, "Status: " + status + " | Sparkly? " + sparkly);

                foreach (var stat in stats)
                {
                    twitchClient.QueueWhisper(userObj, stat);
                }
            });
        }

        static void WhisperItem(string user, Item itm, UserSystem userSystem, ITwitchClient twitchClient, Dictionary<int, Item> itemDatabase)
        {
            string name = itm.itemName;
            string type = "";
            int inventoryID = itm.inventoryID;
            switch (itm.itemType)
            {
                case (Item.TYPE_ARMOR):
                    {
                        type = "Armor";
                    }
                    break;

                case (Item.TYPE_WEAPON):
                    {
                        type = "Weapon";
                    }
                    break;

                case (Item.TYPE_OTHER):
                    {
                        type = "Misc. Item";
                    }
                    break;
                default:
                    {
                        type = "Broken";
                    }
                    break;
            }
            string rarity = "";
            switch (itm.itemRarity)
            {
                case Item.QUALITY_UNCOMMON:
                    {
                        rarity = "Uncommon";
                    }
                    break;

                case Item.QUALITY_RARE:
                    {
                        rarity = "Rare";
                    }
                    break;

                case Item.QUALITY_EPIC:
                    {
                        rarity = "Epic";
                    }
                    break;

                case Item.QUALITY_ARTIFACT:
                    {
                        rarity = "Artifact";
                    }
                    break;

                default:
                    {
                        rarity = "Broken";
                    }
                    break;
            }
            List<string> stats = new List<string>();

            if (itm.successChance > 0)
            {
                stats.Add("+" + itm.successChance + "% Success Chance");
            }

            if (itm.xpBonus > 0)
            {
                stats.Add("+" + itm.xpBonus + "% XP Bonus");
            }

            if (itm.coinBonus > 0)
            {
                stats.Add("+" + itm.coinBonus + "% Wolfcoin Bonus");
            }

            if (itm.itemFind > 0)
            {
                stats.Add("+" + itm.itemFind + "% Item Find");
            }

            if (itm.preventDeathBonus > 0)
            {
                stats.Add("+" + itm.preventDeathBonus + "% to Prevent Death");
            }

            bool active = itm.isActive;
            string status = "";
            if (active)
            {
                status = "(Equipped)";
            }
            else
            {
                status = "(Unequipped)";
            }

            userSystem.GetUserByNameAsync(user, (userObj) =>
            {
                twitchClient.QueueWhisper(userObj, name + " (" + rarity + " " + type + ") " + status);
                twitchClient.QueueWhisper(userObj, "Inventory ID: " + inventoryID);
                foreach (var stat in stats)
                {
                    twitchClient.QueueWhisper(userObj, stat);
                }
            });
        }

        static int GrantItem(int id, Currency wolfcoins, string user, Dictionary<int, Item> itemDatabase)
        {
            string logPath = "dungeonlog.txt";

            if (id < 1)
                return -1;

            Item newItem = itemDatabase[id - 1];
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