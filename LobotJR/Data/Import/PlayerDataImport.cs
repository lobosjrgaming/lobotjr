using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static Dictionary<int, CharacterClass> SeedClassData(IRepository<CharacterClass> classRepository, IRepository<ItemType> typeRepository, IRepository<Equippables> equippableRepository)
        {
            var deprived = new CharacterClass("Deprived", false, 0f, 0f, 0f, 0f, 0f) { CanPlay = false };
            var output = new Dictionary<int, CharacterClass>()
            {
                { -1, deprived },
                { 0, deprived },
                { 1, new CharacterClass("Warrior", true, 0.1f, 0.03f, 0.05f, 0f, 0f) },
                { 2, new CharacterClass("Mage", true, 0.03f, 0.1f, 0f, 0.05f, 0.05f) },
                { 3, new CharacterClass("Rogue", true, 0f, 0.05f, 0.1f, 0.03f, 0.03f) },
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
            return output;
        }

        public static async Task<bool> ImportPlayerDataIntoSql(
            string coinDataPath,
            string xpDataPath,
            string classDataPath,
            IRepository<PlayerCharacter> playerRepository,
            IRepository<CharacterClass> classRepository,
            IRepository<ItemType> typeRepository,
            IRepository<Equippables> equippableRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<Stable> stableRepository,
            UserController userController,
            Dictionary<int, Item> itemMap,
            Dictionary<int, Pet> petMap)
        {
            var classMap = SeedClassData(classRepository, typeRepository, equippableRepository);
            var coinList = LoadLegacyCoinData(coinDataPath);
            var xpList = LoadLegacyExperienceData(xpDataPath);
            var classList = LoadLegacyClassData(classDataPath);
            var regex = new Regex("[^0-9a-zA-Z_]");
            var uniqueKeys = classList.Keys.Distinct(StringComparer.OrdinalIgnoreCase);
            classList = uniqueKeys.ToDictionary(x => x, x => classList[x], StringComparer.OrdinalIgnoreCase);

            foreach (var coin in coinList)
            {
                if (classList.TryGetValue(coin.Key, out var classCoin))
                {
                    classCoin.coins = coin.Value;
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
                    classXp.xp = xp.Value;
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

            var mangledUsernames = classList.Keys.Where(x => regex.IsMatch(x)).ToArray();
            Logger.Warn("Found {count} entries with invalid user names. These records are being skipped, and their coin, xp, and class data values were saved in mangled.json", mangledUsernames.Count());
            var mangledRecords = mangledUsernames.Select(x => classList[x]).Where(x => x != null).ToArray();
            File.WriteAllText("mangled.json", JsonConvert.SerializeObject(mangledRecords));
            var validUsernames = classList.Keys.Except(mangledUsernames).ToArray();
            Logger.Info("Fetching user data for {userCount} users, this could take several minutes", validUsernames.Count());
            //TODO: For some reason after this process happens once, running it again on the same data files produces weird user counts, when it should be 0 since the process already ran once.
            var userMap = (await userController.GetUsersByNames(validUsernames.ToArray())).ToDictionary(x => x.Username, x => x, StringComparer.OrdinalIgnoreCase);
            Logger.Info("Importing data into database.");
            try
            {
                var total = classList.Count();
                // For some reason this is throwing a duplicate key exception, I'm guessing it's because there are two user entries that differ only by case
                var matchedPlayers = classList.Where(x => userMap.ContainsKey(x.Key)).ToDictionary(x => userMap[x.Key].TwitchId, x => x.Value);
                var playersToAdd = matchedPlayers.Select(x => new PlayerCharacter()
                {
                    UserId = x.Key,
                    CharacterClassId = classMap[x.Value.classType].Id,
                    Currency = x.Value.coins,
                    Experience = x.Value.xp,
                    Level = x.Value.level,
                    Prestige = x.Value.prestige,
                }).ToList();
                playerRepository.BatchCreate(playersToAdd, 1000, Logger, "player");
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
                stableRepository.BatchCreate(stablesToAdd, 1000, Logger, "stable");
                var itemsToCreate = matchedPlayers.SelectMany(x => x.Value.myItems.Select(y => new Inventory()
                {
                    UserId = x.Key,
                    ItemId = itemMap[y.itemID].Id,
                    IsEquipped = y.isActive
                })).ToList();
                inventoryRepository.BatchCreate(itemsToCreate, 1000, Logger, "inventory");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
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
