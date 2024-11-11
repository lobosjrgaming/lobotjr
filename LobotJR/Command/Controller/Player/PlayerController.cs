using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Data.Import;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LobotJR.Command.Controller.Player
{
    /// <summary>
    /// Controller for managing player experience and currency.
    /// </summary>
    public class PlayerController : IProcessor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static readonly int MaxLevel = 20;
        public static readonly int MinLevel = 3;

        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly UserController UserController;
        private readonly EquipmentController EquipmentController;

        private readonly List<string> PendingRespec = new List<string>();

        /// <summary>
        /// Event handler for events related to leveling up.
        /// </summary>
        /// <param name="user">The user that leveled up.</param>
        /// <param name="player">A player character object for the user.</param>
        public delegate void LevelUpEventHandler(User user, PlayerCharacter player);
        /// <summary>
        /// Event fired when a player levels up.
        /// </summary>
        public event LevelUpEventHandler LevelUp;
        /// <summary>
        /// Event handler for events related to periodic awards.
        /// </summary>
        /// <param name="experience">The amount of experience given.</param>
        /// <param name="currency">The amount of currency given.</param>
        /// <param name="multiplier">The subscriber multiplier applied.</param>
        public delegate void ExperienceAwardHandler(int experience, int currency, int multiplier);
        /// <summary>
        /// Event fired when periodic experience and currency are awarded.
        /// </summary>
        public event ExperienceAwardHandler ExperienceAwarded;
        /// <summary>
        /// Event handler for events related to periodic awards.
        /// </summary>
        /// <param name="enabled">True if experience was enabled, false if it
        /// was disabled.</param>
        public delegate void ExperienceToggleHandler(bool enabled);
        /// <summary>
        /// Event fired when periodic experience and currency are awarded.
        /// </summary>
        public event ExperienceToggleHandler ExperienceToggled;
        /// <summary>
        /// Event handler for events related to periodic awards.
        /// </summary>
        /// <param name="enabled">True if experience was enabled, false if it
        /// was disabled.</param>
        public delegate void MultiplierModifiedHandler(int value);
        /// <summary>
        /// Event fired when periodic experience and currency are awarded.
        /// </summary>
        public event MultiplierModifiedHandler MultiplierModified;

        /// <summary>
        /// The last time experience was awarded to viewers.
        /// </summary>
        public DateTime LastAward { get; set; }
        /// <summary>
        /// Multiplier applied to experience and currency awards.
        /// </summary>
        public int CurrentMultiplier { get; private set; } = 1;
        /// <summary>
        /// Wether or not the experience is currently being awarded.
        /// </summary>
        public bool AwardsEnabled { get; set; } = false;
        /// <summary>
        /// The user that last turned on experience.
        /// </summary>
        public User AwardSetter { get; set; }

        public PlayerController(IConnectionManager connectionManager, SettingsManager settingsManager, UserController userController, EquipmentController equipmentController)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            UserController = userController;
            EquipmentController = equipmentController;
        }

        public static int ExperienceForLevel(int level)
        {
            return (int)(4 * Math.Pow(level, 3) + 50);
        }

        public static int LevelFromExperience(int experience)
        {
            experience = Math.Max(experience, 81);
            var level = Math.Pow((experience - 50.0f) / 4.0f, (1.0f / 3.0f));
            return (int)Math.Floor(level);
        }

        /// <summary>
        /// Changes the current multiplier applied to xp and wolfcoin awards
        /// given to viewers.
        /// </summary>
        /// <param name="multiplier">The number to multiply awards by.</param>
        public void SetMultiplier(int multiplier)
        {
            CurrentMultiplier = multiplier;
            MultiplierModified?.Invoke(multiplier);
        }

        /// <summary>
        /// Clears all pending respecs.
        /// </summary>
        public void ClearRespecs()
        {
            PendingRespec.Clear();
        }

        /// <summary>
        /// Gives experience to a player. Raises the LevelUp event if the
        /// player levels up or prestiges as a result of this experience gain.
        /// </summary>
        /// <param name="user">The user object for the player.</param>
        /// <param name="player">The player character object.</param>
        /// <param name="experience">The amount of experience to add.</param>
        public void GainExperience(User user, PlayerCharacter player, int experience)
        {
            var oldLevel = LevelFromExperience(player.Experience);
            var newLevel = LevelFromExperience(player.Experience + experience);
            if (newLevel < oldLevel)
            {
                experience += GetExperienceToNextLevel(player.Experience + experience);
                newLevel = LevelFromExperience(player.Experience + experience);
            }
            player.Experience += experience;
            if (newLevel != oldLevel)
            {
                if (newLevel > MaxLevel)
                {
                    player.Level = MinLevel;
                    player.Experience = 200;
                    player.Prestige++;
                }
                else
                {
                    player.Level = newLevel;
                }
                LevelUp?.Invoke(user, player);
            }
        }

        /// <summary>
        /// Gives experience to a player. Raises the LevelUp event if the
        /// player levels up or prestiges as a result of this experience gain.
        /// </summary>
        /// <param name="player">The player character object.</param>
        /// <param name="experience">The amount of experience to add.</param>
        public void GainExperience(PlayerCharacter player, int experience)
        {
            var user = UserController.GetUserById(player.UserId);
            GainExperience(user, player, experience);
        }

        /// <summary>
        /// Gets a player character object for a given user.
        /// </summary>
        /// <param name="userId">The user id of the player character.</param>
        /// <returns>A player character object tied to the user.</returns>
        public PlayerCharacter GetPlayerByUserId(string userId)
        {
            var player = ConnectionManager.CurrentConnection.PlayerCharacters.FirstOrDefault(x => x.UserId.Equals(userId));
            if (player == null)
            {
                player = new PlayerCharacter()
                {
                    UserId = userId,
                    CharacterClassId = ConnectionManager.CurrentConnection.CharacterClassData.First(x => !x.CanPlay).Id,
                };
                ConnectionManager.CurrentConnection.PlayerCharacters.Create(player);
                ConnectionManager.CurrentConnection.PlayerCharacters.Commit();
            }
            return player;
        }

        /// <summary>
        /// Gets a player character object for a given user.
        /// </summary>
        /// <param name="user">The user to get the player character for.</param>
        /// <returns>A player character object tied to the user.</returns>
        public PlayerCharacter GetPlayerByUser(User user)
        {
            return GetPlayerByUserId(user.TwitchId);
        }

        /// <summary>
        /// Gets a collection of players from a collection of users, optimized
        /// for bulk processing.
        /// </summary>
        /// <param name="users">A collection of user objects.</param>
        /// <returns>A collection of player character objects for each user.</returns>
        public IEnumerable<PlayerCharacter> GetPlayersByUsers(IEnumerable<User> users)
        {
            var newClass = ConnectionManager.CurrentConnection.CharacterClassData.First(x => !x.CanPlay);
            var userIds = users.Select(x => x.TwitchId).ToList();
            var existingPlayers = ConnectionManager.CurrentConnection.PlayerCharacters.Read(x => userIds.Contains(x.UserId)).ToList();
            var existingIds = existingPlayers.Select(x => x.UserId);
            var missingUsers = userIds.Except(existingIds);
            if (missingUsers.Any())
            {
                var newPlayers = missingUsers.Select(x => new PlayerCharacter() { UserId = x, CharacterClassId = newClass.Id });
                newPlayers = ConnectionManager.CurrentConnection.PlayerCharacters.BatchCreate(newPlayers, newPlayers.Count(), Logger, "Players");
                existingPlayers = existingPlayers.Concat(newPlayers).ToList();
            }
            return existingPlayers;
        }

        /// <summary>
        /// Gets the user object for a given player.
        /// </summary>
        /// <param name="player">The player to get the user object for.</param>
        /// <returns>A user object tied to the player.</returns>
        public User GetUserByPlayer(PlayerCharacter player)
        {
            return ConnectionManager.CurrentConnection.Users.Read(x => x.TwitchId.Equals(player.UserId)).FirstOrDefault();
        }

        /// <summary>
        /// Returns the amount of experience needed to gain a level.
        /// </summary>
        /// <param name="experience">The current total experience.</param>
        /// <returns>The amount of experience needed to trigger a level up.</returns>
        public int GetExperienceToNextLevel(int experience)
        {
            return ExperienceForLevel(LevelFromExperience(experience) + 1) - experience;
        }

        /// <summary>
        /// Gets a collection of all playable classes. Non-playable classes
        /// such as the starting class will not be included.
        /// </summary>
        /// <returns>A collection of class data.</returns>
        public IEnumerable<CharacterClass> GetPlayableClasses()
        {
            return ConnectionManager.CurrentConnection.CharacterClassData.Read(x => x.CanPlay);
        }

        /// <summary>
        /// Gets metrics for the distribution of selected classes.
        /// </summary>
        /// <returns>A map of all playable classes and how many players have
        /// chosen each class.</returns>
        public IDictionary<CharacterClass, int> GetClassDistribution()
        {
            var classes = GetPlayableClasses();
            var allPlayers = ConnectionManager.CurrentConnection.PlayerCharacters.Read(x => x.CharacterClass.CanPlay);
            var output = new Dictionary<CharacterClass, int>();
            foreach (var characterClass in classes)
            {
                output.Add(characterClass, allPlayers.Count(x => x.CharacterClassId == characterClass.Id));
            }
            return output;
        }

        /// <summary>
        /// Gets the currency cost for a player to respec.
        /// </summary>
        /// <param name="level">The player's current level.</param>
        /// <returns>The currency required respec.</returns>
        public int GetRespecCost(int level)
        {
            var settings = SettingsManager.GetGameSettings();
            return Math.Max(settings.RespecCost, settings.RespecCost * (level - 4));
        }

        /// <summary>
        /// Gets the cost to pry on another player.
        /// </summary>
        /// <returns>The amount of currency required to pry.</returns>
        public int GetPryCost()
        {
            var settings = SettingsManager.GetGameSettings();
            return settings.PryCost;
        }

        /// <summary>
        /// Clears the class from a player. This should only be used for
        /// debugging purposes.
        /// </summary>
        /// <param name="player">The player to remove the class from.</param>
        public void ClearClass(PlayerCharacter player)
        {
            var baseClass = ConnectionManager.CurrentConnection.CharacterClassData.First(x => !x.CanPlay);
            SetClass(player, baseClass);
        }

        /// <summary>
        /// Sets the character class for a player.
        /// </summary>
        /// <param name="player">The player to change the class of.</param>
        /// <param name="characterClass">The class to change to.</param>
        public void SetClass(PlayerCharacter player, CharacterClass characterClass)
        {
            player.CharacterClass = characterClass;
        }

        /// <summary>
        /// Changes a player's class to a different one.
        /// </summary>
        /// <param name="player">The player to respec.</param>
        /// <param name="characterClass">The class to change to.</param>
        /// <param name="cost">The amount of currency to remove.</param>
        /// <returns>Whether or not the respec was successful.</returns>
        public bool Respec(PlayerCharacter player, CharacterClass characterClass, int cost)
        {
            if (player.Currency >= cost)
            {
                player.Currency -= cost;
                PendingRespec.Remove(player.UserId);
                EquipmentController.ClearInventory(player);
                SetClass(player, characterClass);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether a player has initiated a respec.
        /// </summary>
        /// <param name="player">The player to check for.</param>
        /// <returns>True if the player has initiated a respec.</returns>
        public bool IsFlaggedForRespec(PlayerCharacter player)
        {
            return PendingRespec.Contains(player.UserId);
        }

        /// <summary>
        /// Initiates a respec for a player.
        /// </summary>
        /// <param name="player">The player to flag for a respec.</param>
        /// <returns>True if the respec initiated successfully, false if the
        /// player was already flagged.</returns>
        public bool FlagForRespec(PlayerCharacter player)
        {
            if (!PendingRespec.Contains(player.UserId))
            {
                PendingRespec.Add(player.UserId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cancels a pending respec.
        /// </summary>
        /// <param name="player">The player to unflag for respec.</param>
        public void UnflagForRespec(PlayerCharacter player)
        {
            PendingRespec.Remove(player.UserId);
        }

        /// <summary>
        /// Checks whether a player is elligible to choose a class.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player can select a class.</returns>
        public bool CanSelectClass(PlayerCharacter player)
        {
            return IsFlaggedForRespec(player) || (!player.CharacterClass.CanPlay && player.Level >= MinLevel);
        }

        /// <summary>
        /// Checks if the player can afford to pry.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player has enough currency to pry.</returns>
        public bool CanPry(PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            return player.Currency >= settings.PryCost;
        }

        /// <summary>
        /// Performs a pry, returning the data for another player.
        /// </summary>
        /// <param name="player">The player executing the pry.</param>
        /// <param name="target">The player to pry on.</param>
        /// <returns>The player character object for the target.</returns>
        public PlayerCharacter Pry(PlayerCharacter player, string target)
        {
            var targetUser = UserController.GetUserByName(target);
            if (targetUser != null)
            {
                var targetPlayer = GetPlayerByUser(targetUser);
                if (targetPlayer != null)
                {
                    var settings = SettingsManager.GetGameSettings();
                    player.Currency -= settings.PryCost;
                    return targetPlayer;
                }
            }
            return null;
        }

        /// <summary>
        /// Enables periodic experience and currency awards.
        /// </summary>
        /// <param name="user">The user triggering the enable.</param>
        public void EnableAwards(User user)
        {
            LastAward = DateTime.Now;
            AwardsEnabled = true;
            AwardSetter = user;
            ExperienceToggled?.Invoke(true);
        }

        /// <summary>
        /// Disables periodic experience and currency awards.
        /// </summary>
        public void DisableAwards()
        {
            AwardsEnabled = false;
            AwardSetter = null;
            ExperienceToggled?.Invoke(false);
        }

        private async Task<int> ImportPlayerDataBatch(
            IEnumerable<string> users,
            Dictionary<string, LegacyCharacterClass> classList,
            IEnumerable<string> playerIds,
            IRepository<PlayerCharacter> playerRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<Stable> stableRepository,
            UserController userController,
            Dictionary<int, CharacterClass> classMap,
            Dictionary<int, Item> itemMap,
            Dictionary<int, Pet> petMap)
        {
            var maxLevel = 20;
            var maxExperience = ExperienceForLevel(maxLevel + 1) - 1;
            var userArray = users.ToArray();
            IEnumerable<User> userResponse = await userController.GetUsersByNames(userArray, false);
            var newIds = userResponse.Select(x => x.TwitchId).Except(playerIds);
            var idMap = userResponse.Distinct(new UserNameEqualityComparer()).ToDictionary(x => x.TwitchId, x => x);
            var userMap = newIds.Select(x => idMap[x]).ToDictionary(x => x.Username, x => x);
            try
            {
                var total = classList.Count();
                var matchedPlayers = userMap.Where(x => classList.ContainsKey(x.Key)).ToDictionary(x => x.Value.TwitchId, x => classList[x.Key]);
                var playersToAdd = matchedPlayers.Select(x => new PlayerCharacter()
                {
                    UserId = x.Key,
                    CharacterClassId = classMap[x.Value.classType].Id,
                    Currency = x.Value.coins,
                    Experience = Math.Min(Math.Max(x.Value.xp, ExperienceForLevel(x.Value.level)), maxExperience),
                    Level = Math.Min(Math.Max(x.Value.level, LevelFromExperience(x.Value.xp)), maxLevel),
                    Prestige = x.Value.prestige,
                }).ToList();
                playerRepository.BatchCreate(playersToAdd, userArray.Length, Logger, "player");
                var stablesToAdd = matchedPlayers.SelectMany(x => x.Value.myPets.Select(y => new Stable()
                {
                    UserId = x.Key,
                    PetId = petMap[y.ID].Id,
                    Name = y.name,
                    Level = y.level,
                    Experience = y.xp,
                    Affection = y.affection,
                    Hunger = y.hunger,
                    IsSparkly = y.isSparkly,
                    IsActive = y.isActive,
                })).ToList();
                stableRepository.BatchCreate(stablesToAdd, userArray.Length, Logger, "stable");
                var itemsToCreate = matchedPlayers.SelectMany(x => x.Value.myItems.Select(y => new Inventory()
                {
                    UserId = x.Key,
                    ItemId = itemMap[y.itemID].Id,
                    IsEquipped = y.isActive
                })).ToList();
                inventoryRepository.BatchCreate(itemsToCreate, userArray.Length, Logger, "inventory");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return 0;
            }
            return userMap.Count;
        }

        private Dictionary<string, string> LoadKvpFile(string path)
        {
            return File.ReadAllLines(path).Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
        }

        private Dictionary<int, CharacterClass> BuildClassMap()
        {
            try
            {
                var oldClasses = new Dictionary<int, string>() {
                    { -1, "Deprived" },
                    { 0, "Deprived" },
                    { 1, "Warrior" },
                    { 2, "Mage" },
                    { 3, "Rogue" },
                    { 4, "Ranger" },
                    { 5, "Cleric" }
                };
                return oldClasses.ToDictionary(x => x.Key, x => ConnectionManager.CurrentConnection.CharacterClassData.Read(y => y.Name.Equals(x.Value)).First());
            }
            catch
            {
                throw new Exception("Unable to map class data as names have changed. Aborting import fix.");
            }
        }

        private Dictionary<int, Item> BuildItemMap()
        {
            try
            {
                var itemBackupFile = Directory.GetFiles($"./{ItemDataImport.ContentFolderName}", $"{ItemDataImport.ItemListPath}.*.backup").OrderByDescending(x => x).FirstOrDefault();
                var itemMap = File.ReadAllLines(itemBackupFile).Select(x => x.Split(',')).ToDictionary(x => int.Parse(x[0]), x => LoadKvpFile($"./{ItemDataImport.ContentFolderName}/{ItemDataImport.ItemFolder}/{x[1]}")["Name"]);
                return itemMap.ToDictionary(x => x.Key, x => ConnectionManager.CurrentConnection.ItemData.Read(y => y.Name.Equals(x.Value)).First());
            }
            catch
            {
                throw new Exception("Unable to map item data as names have changed. Aborting import fix.");
            }
        }

        private Dictionary<int, Pet> BuildPetMap()
        {
            try
            {
                var petBackupFile = Directory.GetFiles($"./{PetDataImport.ContentFolderName}", $"{PetDataImport.PetListPath}.*.backup").OrderByDescending(x => x).FirstOrDefault();
                var petMap = File.ReadAllLines(petBackupFile).Select(x => x.Split(',')).ToDictionary(x => int.Parse(x[0]), x => LoadKvpFile($"./{PetDataImport.ContentFolderName}/{PetDataImport.PetFolder}/{x[1]}")["Name"]);
                return petMap.ToDictionary(x => x.Key, x => ConnectionManager.CurrentConnection.PetData.Read(y => y.Name.Equals(x.Value)).First());
            }
            catch
            {
                throw new Exception("Unable to map pet data as names have changed. Aborting import fix.");
            }
        }

        /// <summary>
        /// Special one-time-use method to fix a botched import.
        /// </summary>
        /// <returns>The number of users updated.</returns>
        public async Task<int> ImportFix()
        {
            Logger.Info("Loading backup data.");
            var xpBackupFile = Directory.GetFiles(".", $"{PlayerDataImport.ExperienceDataPath}.*.backup").OrderByDescending(x => x).FirstOrDefault();
            var coinBackupFile = Directory.GetFiles(".", $"{PlayerDataImport.CoinDataPath}.*.backup").OrderByDescending(x => x).FirstOrDefault();
            var classBackupFile = Directory.GetFiles(".", $"{PlayerDataImport.ClassDataPath}.*.backup").OrderByDescending(x => x).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(xpBackupFile) && !string.IsNullOrWhiteSpace(coinBackupFile) && !string.IsNullOrWhiteSpace(classBackupFile))
            {
                var classMap = BuildClassMap();
                var itemMap = BuildItemMap();
                var petMap = BuildPetMap();

                var fileData = PlayerDataImport.LoadLegacyData(coinBackupFile, xpBackupFile, classBackupFile);
                var fileNames = fileData.Keys.Distinct(StringComparer.OrdinalIgnoreCase);

                var allPlayers = ConnectionManager.CurrentConnection.PlayerCharacters.Read().ToList();
                var dupePlayers = allPlayers.GroupBy(x => x.UserId).Where(x => x.Count() > 1);
                var index = 0;
                var toDelete = new List<PlayerCharacter>();
                Logger.Info("Removing duplicate player records for {count} users.", dupePlayers.Count());
                foreach (var player in dupePlayers)
                {
                    var dupes = player.ToList();
                    var first = dupes.First();
                    var level = dupes.Max(x => x.Level);
                    var xp = dupes.Max(x => x.Experience);
                    first.Level = Math.Max(level, LevelFromExperience(xp));
                    first.Experience = Math.Max(xp, ExperienceForLevel(first.Level));
                    first.Currency = dupes.Max(x => x.Currency);
                    toDelete.AddRange(dupes.Skip(1));
                    index++;
                }
                ConnectionManager.CurrentConnection.PlayerCharacters.DeleteRange(toDelete);
                ConnectionManager.CurrentConnection.Commit();
                Logger.Info("{count} duplicate records deleted.", toDelete.Count);
                allPlayers = ConnectionManager.CurrentConnection.PlayerCharacters.Read().ToList();
                var allUsers = ConnectionManager.CurrentConnection.Users.Read().ToList();
                var allPlayerIds = allPlayers.Select(x => x.UserId);
                var mappedPlayers = allPlayers.Join(allUsers, player => player.UserId, user => user.TwitchId, (player, user) => new KeyValuePair<string, PlayerCharacter>(user.Username, player)).ToDictionary(x => x.Key, x => x.Value);
                var allNames = allUsers.Select(x => x.Username);

                fileNames = fileNames.Except(mappedPlayers.Keys, StringComparer.OrdinalIgnoreCase);

                var regex = new Regex("^[0-9a-zA-Z][0-9a-zA-Z_]*$");

                var mangledUsernames = fileNames.Where(x => !regex.IsMatch(x)).ToArray();
                var mangledRecords = mangledUsernames.Select(x => fileData[x]).Where(x => x != null).ToArray();
                var validUsernames = fileNames.Except(mangledUsernames).ToArray();
                var found = 0;
                var cursor = 0;
                var pageSize = 20000;
                var startTime = DateTime.Now;
                var database = ConnectionManager.CurrentConnection;
                Logger.Info("Checking for missed imports among {count} users.", validUsernames.Length);
                while (cursor < validUsernames.Length)
                {
                    var result = await ImportPlayerDataBatch(validUsernames.Skip(cursor).Take(pageSize), fileData, allPlayerIds, database.PlayerCharacters, database.Inventories, database.Stables, UserController, classMap, itemMap, petMap);
                    cursor += pageSize;
                    found += result;
                    var elapsed = DateTime.Now - startTime;
                    var estimate = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / cursor * validUsernames.Length) - elapsed;
                    if (result > 0)
                    {
                        Logger.Info("{count} skipped records imported.", result);
                    }
                    Logger.Info("{count} of {total} records processed. {elapsed} time elapsed, {estimate} estimated remaining.", Math.Min(cursor, validUsernames.Length), validUsernames.Length, elapsed.ToString("hh\\:mm\\:ss"), estimate.ToString("hh\\:mm\\:ss"));
                }
                Logger.Info("{count} skipped records imported.", found);
                return found;
            }
            return 0;
        }

        public async Task Process()
        {
            var settings = SettingsManager.GetGameSettings();
            if (AwardsEnabled)
            {
                if (LastAward + TimeSpan.FromMinutes(settings.ExperienceFrequency) <= DateTime.Now)
                {
                    LastAward = DateTime.Now;
                    var chatters = await UserController.GetViewerList();
                    var chatterDict = chatters.ToDictionary(x => x.TwitchId, x => x);
                    var xpToAward = settings.ExperienceValue * CurrentMultiplier;
                    var coinsToAward = settings.CoinValue * CurrentMultiplier;
                    var subMultiplier = settings.SubRewardMultiplier;
                    Logger.Info("{coins} wolfcoins and {xp} experience awarded to {count} viewers.", coinsToAward, xpToAward, chatters.Count());
                    var players = GetPlayersByUsers(chatters);
                    ConnectionManager.CurrentConnection.PlayerCharacters.BeginTransaction();
                    foreach (var player in players)
                    {
                        var chatter = chatterDict[player.UserId];
                        if (chatter.IsSub)
                        {
                            GainExperience(chatter, player, xpToAward * subMultiplier);
                            player.Currency += coinsToAward * subMultiplier;
                        }
                        else
                        {
                            GainExperience(chatter, player, xpToAward);
                            player.Currency += coinsToAward;
                        }
                    }
                    ConnectionManager.CurrentConnection.PlayerCharacters.Commit();
                    ExperienceAwarded?.Invoke(xpToAward, coinsToAward, subMultiplier);
                    Logger.Info("Experience awarded to {count} users in {time}.", chatters.Count(), DateTime.Now - LastAward);
                }
            }
        }
    }
}
