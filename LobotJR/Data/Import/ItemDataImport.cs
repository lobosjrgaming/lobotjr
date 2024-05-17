using LobotJR.Command.Model.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Data.Import
{
    public static class ItemDataImport
    {
        public static readonly string ContentFolderName = "content";
        public static readonly string ItemListPath = "itemlist.ini";
        public static readonly string ItemFolder = "items";
        public static IFileSystem FileSystem = new FileSystem();

        public static Dictionary<string, int> SeedItemTypes(IRepository<ItemType> typeRepository)
        {
            List<string> names = new List<string>()
            {
                "Warrior", "Mage", "Rogue", "Ranger", "Cleric"
            };
            foreach (var name in names)
            {
                typeRepository.Create(new ItemType() { Name = name });
            }
            typeRepository.Commit();
            return typeRepository.Read().ToDictionary(x => (names.IndexOf(x.Name) + 1).ToString(), x => x.Id);
        }

        public static Dictionary<string, int> SeedItemSlots(IRepository<ItemSlot> slotRepository)
        {
            List<string> names = new List<string>()
            {
                "Weapon", "Armor", "Trinket", "Other"
            };
            foreach (var name in names)
            {
                slotRepository.Create(new ItemSlot() { Name = name });
            }
            slotRepository.Commit();
            return slotRepository.Read().ToDictionary(x => (names.IndexOf(x.Name) + 1).ToString(), x => x.Id);
        }

        public static Dictionary<string, int> SeedItemQuality(IRepository<ItemQuality> qualityRepository)
        {
            List<string> names = new List<string>()
            {
                "Uncommon", "Rare", "Epic", "Artifact"
            };
            for (var i = 0; i < names.Count; i++)
            {
                qualityRepository.Create(new ItemQuality() { Name = names[i], DropRatePenalty = (i + 1) * 5 });
            }
            qualityRepository.Commit();
            return qualityRepository.Read().ToDictionary(x => (names.IndexOf(x.Name) + 1).ToString(), x => x.Id);
        }

        public static IEnumerable<Tuple<int, Item>> LoadItemData(string contentFolder, string itemListPath, string itemFolder, Dictionary<string, int> typeMap, Dictionary<string, int> slotMap, Dictionary<string, int> qualityMap)
        {
            try
            {
                var entries = FileSystem.ReadAllLines($"{contentFolder}/{itemListPath}")
                    .Select(x => x.Split(','))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1]);

                var output = new List<Tuple<int, Item>>();
                foreach (var entry in entries)
                {
                    var path = $"{contentFolder}/{itemFolder}/{entry.Value}";
                    var itemData = FileSystem.ReadAllLines(path)
                        .Select(x => x.Split('='))
                        .Where(x => x.Length == 2)
                        .ToDictionary(x => x[0], x => x[1]);
                    output.Add(new Tuple<int, Item>(int.Parse(entry.Key), CreateItemFromFile(itemData, typeMap, slotMap, qualityMap)));
                }
                return output;
            }
            catch
            {
                return new List<Tuple<int, Item>>();
            }
        }

        private static Item CreateItemFromFile(Dictionary<string, string> fileData, Dictionary<string, int> types, Dictionary<string, int> slots, Dictionary<string, int> qualities)
        {
            return new Item()
            {
                Name = fileData["Name"],
                Description = fileData["Desc"],
                SlotId = slots[fileData["Type"]],
                TypeId = types[fileData["Class"]],
                QualityId = qualities[fileData["Rarity"]],
                SuccessChance = float.Parse(fileData["SuccessChance"]) / 100f,
                ItemFind = int.Parse(fileData["ItemFind"]),
                CoinBonus = int.Parse(fileData["CoinBonus"]),
                XpBonus = int.Parse(fileData["XpBonus"]),
                PreventDeathBonus = float.Parse(fileData["PreventDeathBonus"]) / 100f
            };
        }

        public static Dictionary<int, Item> ImportItemDataIntoSql(string contentFolder, string itemDataPath, string itemFolder, IRepository<Item> itemRepository, IRepository<ItemType> typeRepository, IRepository<ItemSlot> slotRepository, IRepository<ItemQuality> qualityRepository)
        {
            var typeMap = SeedItemTypes(typeRepository);
            var slotMap = SeedItemSlots(slotRepository);
            var qualityMap = SeedItemQuality(qualityRepository);

            var items = LoadItemData(contentFolder, itemDataPath, itemFolder, typeMap, slotMap, qualityMap);
            foreach (var item in items)
            {
                itemRepository.Create(item.Item2);
            }
            itemRepository.Commit();
            return items.ToDictionary(x => x.Item1, x => x.Item2);
        }
    }
}
