using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Twitch;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Data.Import
{
    public static class DataImporter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static IFileSystem FileSystem = new FileSystem();

        private static bool ImportFishData(IRepository<Fish> fishRepository)
        {
            if (FileSystem.Exists(FishDataImport.FishDataPath))
            {
                Logger.Info("Detected legacy fish data file, migrating to SQLite.");
                FishDataImport.ImportFishDataIntoSql(FishDataImport.FishDataPath, fishRepository);
                FileSystem.Move(FishDataImport.FishDataPath, $"{FishDataImport.FishDataPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                Logger.Info("Fish data migration complete!");
                return true;
            }
            return false;
        }

        private static async Task<bool> ImportFisherData(IRepository<Fish> fishRepository, IRepository<Catch> catchRepository, IRepository<LeaderboardEntry> leaderboardRepository, UserSystem userSystem)
        {
            var hasFisherData = FileSystem.Exists(FisherDataImport.FisherDataPath);
            var hasLeaderboardData = FileSystem.Exists(FisherDataImport.FishingLeaderboardPath);
            if (hasFisherData || hasLeaderboardData)
            {
                Logger.Info("Detected legacy fisher data file, migrating to SQLite. This could take a few minutes.");
                IEnumerable<string> users = new List<string>();
                Dictionary<string, LegacyFisher> legacyFisherData = FisherDataImport.LoadLegacyFisherData(FisherDataImport.FisherDataPath);
                List<LegacyCatch> legacyLeaderboardData = FisherDataImport.LoadLegacyFishingLeaderboardData(FisherDataImport.FishingLeaderboardPath);
                Logger.Info("Converting usernames to user ids...");
                await userSystem.GetUsersByNames(legacyFisherData.Keys.Union(legacyLeaderboardData.Select(x => x.caughtBy)).ToArray());
                if (hasFisherData)
                {
                    Logger.Info("Importing user records...");
                    FisherDataImport.ImportFisherDataIntoSql(legacyFisherData, fishRepository, catchRepository, userSystem);
                    FileSystem.Move(FisherDataImport.FisherDataPath, $"{FisherDataImport.FisherDataPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                }
                if (hasLeaderboardData)
                {
                    Logger.Info("Importing leaderboard...");
                    FisherDataImport.ImportLeaderboardDataIntoSql(legacyLeaderboardData, leaderboardRepository, fishRepository, userSystem);
                    FileSystem.Move(FisherDataImport.FishingLeaderboardPath, $"{FisherDataImport.FishingLeaderboardPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                }
                Logger.Info("Fisher data migration complete!");
                return true;
            }
            return false;
        }

        private static Dictionary<int, Pet> ImportPetData(IRepository<Pet> petRepository, IRepository<PetRarity> rarityRepository)
        {
            var content = PetDataImport.ContentFolderName;
            var listPath = PetDataImport.PetListPath;
            var hasPetData = FileSystem.Exists($"{content}/{listPath}");
            var hasPetDatabase = petRepository.Read().Any() || rarityRepository.Read().Any();
            if (hasPetData)
            {
                Logger.Info("Detected legacy pet data file, migrating to SQLite.");
                if (hasPetDatabase)
                {
                    Logger.Error("Pet database already contains data.");
                    throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                }
                return PetDataImport.ImportPetDataIntoSql(content, listPath, PetDataImport.PetFolder, petRepository, rarityRepository);
            }
            return new Dictionary<int, Pet>();
        }

        private static void RollbackPetData(IRepository<Pet> petRepository, IRepository<PetRarity> rarityRepository)
        {
            var pets = petRepository.Read().ToList();
            foreach (var pet in pets)
            {
                petRepository.Delete(pet);
            }
            petRepository.Commit();
            var rarities = rarityRepository.Read().ToList();
            foreach (var rarity in rarities)
            {
                rarityRepository.Delete(rarity);
            }
            rarityRepository.Commit();
        }

        private static void FinalizePetData()
        {
            var content = PetDataImport.ContentFolderName;
            var listPath = PetDataImport.PetListPath;
            FileSystem.Move($"{content}/{listPath}", $"{content}/{listPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            Logger.Info("Pet data migration complete!");
        }

        private static Dictionary<int, Item> ImportItemData(IRepository<Item> itemRepository, IRepository<ItemType> typeRepository, IRepository<ItemSlot> slotRepository, IRepository<ItemQuality> qualityRepository)
        {
            var content = ItemDataImport.ContentFolderName;
            var listPath = ItemDataImport.ItemListPath;
            var hasItemData = FileSystem.Exists($"{content}/{listPath}");
            var hasItemDatabase = itemRepository.Read().Any() || typeRepository.Read().Any() || slotRepository.Read().Any() || qualityRepository.Read().Any();
            if (hasItemData)
            {
                Logger.Info("Detected legacy item data files, migrating to SQLite.");
                if (hasItemDatabase)
                {
                    Logger.Error("Item database already contains data.");
                    throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                }
                return ItemDataImport.ImportItemDataIntoSql(content, listPath, ItemDataImport.ItemFolder, itemRepository, typeRepository, slotRepository, qualityRepository);
            }
            return new Dictionary<int, Item>();
        }

        private static void RollbackItemData(IRepository<Item> itemRepository, IRepository<ItemType> typeRepository, IRepository<ItemSlot> slotRepository, IRepository<ItemQuality> qualityRepository)
        {
            var items = itemRepository.Read().ToList();
            foreach (var item in items)
            {
                itemRepository.Delete(item);
            }
            itemRepository.Commit();
            var types = typeRepository.Read().ToList();
            foreach (var type in types)
            {
                typeRepository.Delete(type);
            }
            typeRepository.Commit();
            var slots = slotRepository.Read().ToList();
            foreach (var slot in slots)
            {
                slotRepository.Delete(slot);
            }
            slotRepository.Commit();
            var qualities = qualityRepository.Read().ToList();
            foreach (var quality in qualities)
            {
                qualityRepository.Delete(quality);
            }
            qualityRepository.Commit();
        }

        private static void FinalizeItemData()
        {
            var content = ItemDataImport.ContentFolderName;
            var listPath = ItemDataImport.ItemListPath;
            FileSystem.Move($"{content}/{listPath}", $"{content}/{listPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            Logger.Info("Item data migration complete!");
        }

        private static bool ImportDungeonData(IRepository<Dungeon> dungeonRepository, IRepository<DungeonTimer> timerRepository, Dictionary<int, Item> itemMap)
        {
            var content = DungeonDataImport.ContentFolderName;
            var listPath = DungeonDataImport.DungeonListPath;
            var hasDungeonData = FileSystem.Exists($"{content}/{listPath}");
            var hasDungeonDatabase = dungeonRepository.Read().Any() || timerRepository.Read().Any();
            if (hasDungeonData)
            {
                Logger.Info("Detected legacy dungeon data files, migrating to SQLite.");
                if (hasDungeonDatabase)
                {
                    Logger.Error("Dungeon database already contains data.");
                    throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                }
                DungeonDataImport.ImportDungeonDataIntoSql(content, listPath, DungeonDataImport.DungeonFolder, dungeonRepository, timerRepository, itemMap);
            }
            return true;
        }

        private static void RollbackDungeonData(IRepository<Dungeon> dungeonRepository, IRepository<DungeonTimer> timerRepository)
        {
            var dungeons = dungeonRepository.Read().ToList();
            foreach (var dungeon in dungeons)
            {
                dungeonRepository.Delete(dungeon);
            }
            dungeonRepository.Commit();
            var timers = timerRepository.Read().ToList();
            foreach (var timer in timers)
            {
                timerRepository.Delete(timer);
            }
            timerRepository.Commit();
        }

        private static void FinalizeDungeonData()
        {
            var content = DungeonDataImport.ContentFolderName;
            var listPath = DungeonDataImport.DungeonListPath;
            FileSystem.Move($"{content}/{listPath}", $"{content}/{listPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            Logger.Info("Dungeon data migration complete!");
        }

        private static async Task<bool> ImportPlayerData(
            IRepository<PlayerCharacter> playerRepository,
            IRepository<CharacterClass> classRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<Stable> stableRepository,
            UserSystem userSystem,
            Dictionary<int, Item> itemMap,
            Dictionary<int, Pet> petMap)
        {
            var hasCoinData = FileSystem.Exists(PlayerDataImport.CoinDataPath);
            var hasXpData = FileSystem.Exists(PlayerDataImport.ExperienceDataPath);
            var hasClassData = FileSystem.Exists(PlayerDataImport.ClassDataPath);
            var hasPlayerDatabase = playerRepository.Read().Any() || classRepository.Read().Any() || inventoryRepository.Read().Any() || stableRepository.Read().Any();
            if (hasCoinData && hasXpData && hasClassData)
            {
                Logger.Info("Detected legacy player data files, migrating to SQLite.");
                if (hasPlayerDatabase)
                {
                    Logger.Error("Player database already contains data, aborting import.");
                    throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                }
                return await PlayerDataImport.ImportPlayerDataIntoSql(PlayerDataImport.CoinDataPath, PlayerDataImport.ExperienceDataPath, PlayerDataImport.ClassDataPath, playerRepository, classRepository, inventoryRepository, stableRepository, userSystem, itemMap, petMap);
            }
            return false;
        }

        private static void RollbackPlayerData(
            IRepository<PlayerCharacter> playerRepository,
            IRepository<CharacterClass> classRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<Stable> stableRepository)
        {
            var stables = stableRepository.Read().ToList();
            foreach (var stable in stables)
            {
                stableRepository.Delete(stable);
            }
            stableRepository.Commit();
            var inventories = inventoryRepository.Read().ToList();
            foreach (var inventory in inventories)
            {
                inventoryRepository.Delete(inventory);
            }
            inventoryRepository.Commit();
            var classes = classRepository.Read().ToList();
            foreach (var characterClass in classes)
            {
                classRepository.Delete(characterClass);
            }
            classRepository.Commit();
            var players = playerRepository.Read().ToList();
            foreach (var player in players)
            {
                playerRepository.Delete(player);
            }
            playerRepository.Commit();
        }

        private static void FinalizePlayerData()
        {
            var coinPath = PlayerDataImport.CoinDataPath;
            var xpPath = PlayerDataImport.ExperienceDataPath;
            var classPath = PlayerDataImport.ClassDataPath;
            FileSystem.Move($"{coinPath}", $"{coinPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            FileSystem.Move($"{xpPath}", $"{xpPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            FileSystem.Move($"{classPath}", $"{classPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            Logger.Info("Player data migration complete!");
        }

        private static async Task<bool> ImportPlayerData(IDatabase database, UserSystem userSystem)
        {
            var petMap = ImportPetData(database.PetData, database.PetRarityData);
            if (petMap.Any())
            {
                var itemMap = ImportItemData(database.ItemData, database.ItemTypeData, database.ItemSlotData, database.ItemQualityData);
                if (itemMap.Any())
                {
                    var dungeonImport = ImportDungeonData(database.DungeonData, database.DungeonTimerData, itemMap);
                    if (dungeonImport)
                    {
                        var playerImport = await ImportPlayerData(database.PlayerCharacters, database.CharacterClassData, database.Inventories, database.Stables, userSystem, itemMap, petMap);
                        if (playerImport)
                        {
                            FinalizePlayerData();
                            //TODO: classData.json file is persisting after this operation. Best guess is that the legacy wolfcoin code is importing the data file and then writing it back out to memory after this happens. Should resolve once the legacy code is removed.
                            FinalizeDungeonData();
                            FinalizeItemData();
                            FinalizePetData();
                            return true;
                        }
                        RollbackPlayerData(database.PlayerCharacters, database.CharacterClassData, database.Inventories, database.Stables);
                    }
                    RollbackDungeonData(database.DungeonData, database.DungeonTimerData);
                }
                RollbackItemData(database.ItemData, database.ItemTypeData, database.ItemSlotData, database.ItemQualityData);
            }
            RollbackPetData(database.PetData, database.PetRarityData);
            return false;
        }

        public static async Task ImportLegacyData(IDatabase database, UserSystem userSystem)
        {
            ImportFishData(database.FishData);
            await ImportFisherData(database.FishData, database.Catches, database.FishingLeaderboard, userSystem);
            var playerImport = await ImportPlayerData(database, userSystem);
            if (!playerImport)
            {
                Logger.Warn("Player data import failed, please verify your player, dungeon, item, and pet files and try again.");
            }
        }
    }
}
