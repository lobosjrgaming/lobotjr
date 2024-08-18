using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Twitch;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
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
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(FileSystem.ReadAllText(experienceDataPath));
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
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(FileSystem.ReadAllText(coinDataPath));
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
                return JsonConvert.DeserializeObject<Dictionary<string, LegacyCharacterClass>>(FileSystem.ReadAllText(classDataPath));
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
            UserSystem userSystem,
            Dictionary<int, Item> itemMap,
            Dictionary<int, Pet> petMap)
        {
            var classMap = SeedClassData(classRepository, typeRepository, equippableRepository);
            var coinList = LoadLegacyCoinData(coinDataPath);
            var xpList = LoadLegacyExperienceData(xpDataPath);
            var classList = LoadLegacyClassData(classDataPath);

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

            var keys = classList.Keys.ToArray();
            Logger.Info("Fetching user data for {userCount} users, this could take several minutes", keys.Count());
            //TODO: For some reason after this process happens once, running it again on the same data files produces weird user counts, when it should be 0 since the process already ran once.
            var userMap = await userSystem.GetUsersByNames(keys);
            Logger.Info("Importing data into database.");
            playerRepository.BeginTransaction();
            try
            {

                foreach (var userClass in classList)
                {
                    var user = userMap.FirstOrDefault(x => x.Username.Equals(userClass.Key, StringComparison.OrdinalIgnoreCase));
                    if (user != null)
                    {
                        var player = new PlayerCharacter()
                        {
                            UserId = user.TwitchId,
                            CharacterClassId = classMap[userClass.Value.classType].Id,
                            Currency = userClass.Value.coins,
                            Experience = userClass.Value.xp,
                            Level = userClass.Value.level,
                            Prestige = userClass.Value.prestige,
                        };
                        playerRepository.Create(player);
                        foreach (var pet in userClass.Value.myPets)
                        {
                            var stable = new Stable()
                            {
                                UserId = user.TwitchId,
                                PetId = petMap[pet.ID].Id,
                                Name = pet.name,
                                Level = pet.level,
                                Experience = pet.xp,
                                Affection = pet.affection,
                                Hunger = pet.hunger,
                                IsSparkly = pet.isSparkly,
                                IsActive = pet.isActive,
                            };
                            stableRepository.Create(stable);
                        }
                        foreach (var item in userClass.Value.myItems)
                        {
                            var itemObject = itemMap[item.itemID];
                            var inventory = new Inventory()
                            {
                                UserId = user.TwitchId,
                                ItemId = itemObject.Id,
                                IsEquipped = item.isActive
                            };
                            inventoryRepository.Create(inventory);
                        }
                    }
                }
                playerRepository.Commit();
                stableRepository.Commit();
                inventoryRepository.Commit();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
            return true;
        }
    }

    public class LegacyCharacterClass
    {
        public string name { get; set; }
        public int classType { get; set; }
        public int level { get; set; }
        public int prestige { get; set; }
        public int xp { get; set; }
        public int coins { get; set; }
        public List<LegacyItem> myItems { get; set; }
        public List<LegacyPet> myPets { get; set; }
    }

    public class LegacyItem
    {
        public int itemID { get; set; }
        public bool isActive { get; set; }
    }

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
