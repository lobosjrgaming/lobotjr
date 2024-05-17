using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Experience;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.System.Twitch;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Data.Import
{
    public static class PlayerDataImport
    {
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

        public static Dictionary<string, CharacterClass> SeedClassData(IRepository<CharacterClass> classRepository)
        {
            var output = new Dictionary<string, CharacterClass>()
            {
                { "-1", new CharacterClass("Deprived", 0, 0, 0, 0, 0) { CanPlay = false } },
                { "1", new CharacterClass("Warrior", 0.1f, 3, 5, 0, 0) },
                { "2", new CharacterClass("Mage", 0.03f, 10, 0, 5, 0) },
                { "3", new CharacterClass("Rogue", 0, 5, 10, 3, 0) },
                { "4", new CharacterClass("Ranger", 0.05f, 0, 3, 10, 0) },
                { "5", new CharacterClass("Cleric", 0.03f, 3, 3, 3, 0.1f) }
            };

            foreach (var entry in output.OrderBy(x => x.Key))
            {
                classRepository.Create(entry.Value);
            }
            classRepository.Commit();
            return output;
        }

        public static async Task ImportPlayerDataIntoSql(
            string coinDataPath,
            string xpDataPath,
            string classDataPath,
            IRepository<PlayerCharacter> playerRepository,
            IRepository<CharacterClass> classRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<Stable> stableRepository,
            UserSystem userSystem,
            Dictionary<int, Item> itemMap,
            Dictionary<int, Pet> petMap)
        {
            var classMap = SeedClassData(classRepository);
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

            var userMap = await userSystem.GetUsersByNames(classList.Keys.ToArray());
            foreach (var userClass in classList)
            {
                var user = userMap.FirstOrDefault(x => x.Username.Equals(userClass.Key, StringComparison.OrdinalIgnoreCase));
                if (user != null)
                {
                    var player = new PlayerCharacter()
                    {
                        UserId = user.TwitchId,
                        CharacterClassId = classMap[userClass.Value.className].Id,
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
    }

    public class LegacyCharacterClass
    {
        public string name { get; set; }
        public string className { get; set; }
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
