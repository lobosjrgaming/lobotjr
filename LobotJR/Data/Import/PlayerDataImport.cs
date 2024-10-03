using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LobotJR.Data.Import
{
    public static class PlayerDataImport
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static readonly string ExperienceDataPath = "XP.json";
        public static readonly string CoinDataPath = "wolfcoins.json";
        public static readonly string ClassDataPath = "classData.json";
        public static IFileSystem FileSystem = new FileSystem();

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

        public static Dictionary<string, int> LoadLegacyExperienceData(string experienceDataPath)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(FileSystem.ReadAllText(experienceDataPath)).ToDictionary(x => x.Key, x => x.Value);
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }

        public static Dictionary<string, int> LoadLegacyCoinData(string coinDataPath)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(FileSystem.ReadAllText(coinDataPath)).ToDictionary(x => x.Key, x => x.Value);
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }

        public static Dictionary<string, LegacyCharacterClass> LoadLegacyClassData(string classDataPath)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, LegacyCharacterClass>>(FileSystem.ReadAllText(classDataPath)).ToDictionary(x => x.Key, x => x.Value);
            }
            catch
            {
                return new Dictionary<string, LegacyCharacterClass>();
            }
        }

        public static Dictionary<int, int> SeedClassData(IRepository<CharacterClass> classRepository, IRepository<ItemType> typeRepository, IRepository<Equippables> equippableRepository)
        {
            var deprived = new CharacterClass("Deprived", false, 0f, 0f, 0f, 0f, 0f) { CanPlay = false };
            var output = new Dictionary<int, CharacterClass>()
            {
                { -1, deprived },
                { 0, deprived },
                { 1, new CharacterClass("Warrior", true, 0.1f, 0.03f, 0.05f, 0f, 0f) },
                { 2, new CharacterClass("Mage", true, 0.03f, 0.1f, 0f, 0.05f, 0f) },
                { 3, new CharacterClass("Rogue", true, 0f, 0.05f, 0.1f, 0.03f, 0f) },
                { 4, new CharacterClass("Ranger", true, 0.05f, 0f, 0.03f, 0.1f, 0f) },
                { 5, new CharacterClass("Cleric", true, 0.03f, 0.03f, 0.03f, 0.03f, 0.1f) }
            };

            foreach (var entry in output.OrderBy(x => x.Key))
            {
                classRepository.Create(entry.Value);
            }
            classRepository.Commit();

            var types = typeRepository.Read();
            foreach (var entry in output.OrderBy(x => x.Key))
            {
                var classType = typeRepository.Read(x => x.Name.Equals(entry.Value.Name)).FirstOrDefault();
                if (classType != null)
                {
                    equippableRepository.Create(new Equippables() { CharacterClassId = entry.Value.Id, ItemTypeId = classType.Id });
                }
            }
            equippableRepository.Commit();
            return output.ToDictionary(x => x.Key, x => x.Value.Id);
        }

        public static async Task<int> ImportPlayerDataBatch(
            IEnumerable<string> users,
            Dictionary<string, LegacyCharacterClass> classList,
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
            var userMap = userResponse.Distinct(new UserNameEqualityComparer()).ToDictionary(x => x.Username, x => x);
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
            return userArray.Length;
        }

        private static Dictionary<string, LegacyCharacterClass> LoadLegacyData(string coinDataPath, string xpDataPath, string classDataPath)
        {
            var coinList = LoadLegacyCoinData(coinDataPath);
            var xpList = LoadLegacyExperienceData(xpDataPath);
            var classList = LoadLegacyClassData(classDataPath);
            var uniqueKeys = classList.Keys.Distinct(StringComparer.OrdinalIgnoreCase);
            classList = uniqueKeys.ToDictionary(x => x, x => classList.ContainsKey(x.ToLower()) ? classList[x.ToLower()] : classList[x], StringComparer.OrdinalIgnoreCase);
            foreach (var coin in coinList)
            {
                if (classList.TryGetValue(coin.Key, out var classCoin))
                {
                    classCoin.coins = Math.Max(classCoin.coins, coin.Value);
                }
                else
                {
                    classList.Add(coin.Key, new LegacyCharacterClass()
                    {
                        name = coin.Key,
                        coins = coin.Value
                    });
                }
            }

            foreach (var xp in xpList)
            {
                if (classList.TryGetValue(xp.Key, out var classXp))
                {
                    classXp.xp = Math.Max(classXp.xp, xp.Value);
                }
                else
                {
                    classList.Add(xp.Key, new LegacyCharacterClass()
                    {
                        name = xp.Key,
                        xp = xp.Value
                    });
                }
            }
            return classList;
        }

        public static async Task<bool> ImportPlayerDataIntoSql(
            string coinDataPath,
            string xpDataPath,
            string classDataPath,
            IConnectionManager connectionManager,
            UserController userController,
            Dictionary<int, int> itemMap,
            Dictionary<int, int> petMap)
        {
            try
            {
                var classMap = new Dictionary<int, int>();
                using (var database = connectionManager.OpenConnection())
                {
                    classMap = SeedClassData(database.CharacterClassData, database.ItemTypeData, database.EquippableData);
                }
                var classList = LoadLegacyData(coinDataPath, xpDataPath, classDataPath);
                GC.Collect();
                var regex = new Regex("[^0-9a-zA-Z_]");

                var mangledUsernames = classList.Keys.Where(x => regex.IsMatch(x)).ToArray();
                Logger.Warn("Found {count} entries with invalid user names. These records are being skipped, and their coin, xp, and class data values were saved in mangled.json", mangledUsernames.Count());
                var mangledRecords = mangledUsernames.Select(x => classList[x]).Where(x => x != null).ToArray();
                File.WriteAllText("mangled.json", JsonConvert.SerializeObject(mangledRecords));
                var validUsernames = classList.Keys.Except(mangledUsernames).ToArray();
                var cursor = 0;
                var pageSize = 10000;
                var failed = false;
                var startTime = DateTime.Now;
                Logger.Info("Importing user and player data for {count} users.", validUsernames.Length);
                while (cursor < validUsernames.Length)
                {
                    var result = 0;
                    using (var database = connectionManager.OpenConnection())
                    {
                        var resolvedClassMap = classMap.ToDictionary(x => x.Key, x => database.CharacterClassData.ReadById(x.Value));
                        var resolvedItemMap = itemMap.ToDictionary(x => x.Key, x => database.ItemData.ReadById(x.Value));
                        var resolvedPetMap = petMap.ToDictionary(x => x.Key, x => database.PetData.ReadById(x.Value));
                        result = await ImportPlayerDataBatch(validUsernames.Skip(cursor).Take(pageSize), classList, database.PlayerCharacters, database.Inventories, database.Stables, userController, resolvedClassMap, resolvedItemMap, resolvedPetMap);
                    }
                    if (result == 0)
                    {
                        failed = true;
                        break;
                    }
                    cursor += result;
                    var elapsed = DateTime.Now - startTime;
                    var estimate = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / cursor * validUsernames.Length) - elapsed;
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                    Logger.Info("{count} of {total} records imported. {elapsed} time elapsed, {estimate} estimated remaining.", Math.Min(cursor, validUsernames.Length), validUsernames.Length, elapsed.ToString("hh\\:mm\\:ss"), estimate.ToString("hh\\:mm\\:ss"));
                }
                if (failed)
                {
                    Logger.Error("User import failed. Rolling back changes to preserve database integrity, please close the application and try again.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Logger.Error("Exception occurred while importing player data. Rolling back changes to preserve database integrity, please close the application and try again.");
                return false;
            }
            return true;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Needed to load in legacy data for conversion to modern standards")]
    public class LegacyCharacterClass
    {
        public string name { get; set; }
        public int classType { get; set; }
        public int level { get; set; }
        public int prestige { get; set; }
        public int xp { get; set; }
        public int coins { get; set; }
        public List<LegacyItem> myItems { get; set; } = new List<LegacyItem>();
        public List<LegacyPet> myPets { get; set; } = new List<LegacyPet>();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Needed to load in legacy data for conversion to modern standards")]
    public class LegacyItem
    {
        public int itemID { get; set; }
        public bool isActive { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Needed to load in legacy data for conversion to modern standards")]
    public class LegacyPet
    {
        public int ID { get; set; }
        public string name { get; set; }
        public bool isSparkly { get; set; }
        public bool isActive { get; set; }
        public int affection { get; set; }
        public int hunger { get; set; }
        public int xp { get; set; }
        public int level { get; set; }
    }
}
