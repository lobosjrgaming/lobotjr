using LobotJR.Command.Controller.Twitch;
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

        private static async Task<bool> ImportFishData(IConnectionManager connectionManager)
        {
            if (FileSystem.Exists(FishDataImport.FishDataPath))
            {
                Logger.Info("Detected legacy fish data file, migrating to SQLite.");
                using (var database = await connectionManager.OpenConnection())
                {
                    FishDataImport.ImportFishDataIntoSql(FishDataImport.FishDataPath, database.FishData);
                }
                FileSystem.Move(FishDataImport.FishDataPath, $"{FishDataImport.FishDataPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                Logger.Info("Fish data migration complete!");
                return true;
            }
            return false;
        }

        private static async Task<bool> ImportFisherData(IConnectionManager connectionManager, UserController userController)
        {
            var hasFisherData = FileSystem.Exists(FisherDataImport.FisherDataPath);
            var hasLeaderboardData = FileSystem.Exists(FisherDataImport.FishingLeaderboardPath);
            if (hasFisherData || hasLeaderboardData)
            {
                Logger.Info("Detected legacy fisher data file, migrating to SQLite. This could take a few minutes.");
                IEnumerable<string> users = new List<string>();
                using (var database = await connectionManager.OpenConnection())
                {
                    Dictionary<string, LegacyFisher> legacyFisherData = FisherDataImport.LoadLegacyFisherData(FisherDataImport.FisherDataPath);
                    List<LegacyCatch> legacyLeaderboardData = FisherDataImport.LoadLegacyFishingLeaderboardData(FisherDataImport.FishingLeaderboardPath);
                    Logger.Info("Converting usernames to user ids...");
                    await userController.GetUsersByNames(legacyFisherData.Keys.Union(legacyLeaderboardData.Select(x => x.caughtBy)).ToArray());
                    if (hasFisherData)
                    {
                        Logger.Info("Importing user records...");
                        FisherDataImport.ImportFisherDataIntoSql(legacyFisherData, database.FishData, database.Catches, userController);
                        FileSystem.Move(FisherDataImport.FisherDataPath, $"{FisherDataImport.FisherDataPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                    }
                    if (hasLeaderboardData)
                    {
                        Logger.Info("Importing leaderboard...");
                        FisherDataImport.ImportLeaderboardDataIntoSql(legacyLeaderboardData, database.FishingLeaderboard, database.FishData, userController);
                        FileSystem.Move(FisherDataImport.FishingLeaderboardPath, $"{FisherDataImport.FishingLeaderboardPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
                    }
                }
                Logger.Info("Fisher data migration complete!");
                return true;
            }
            return false;
        }

        private static async Task<Dictionary<int, int>> ImportPetData(IConnectionManager connectionManager)
        {
            var content = PetDataImport.ContentFolderName;
            var listPath = PetDataImport.PetListPath;
            var hasPetData = FileSystem.Exists($"{content}/{listPath}");
            using (var database = await connectionManager.OpenConnection())
            {
                var hasPetDatabase = database.PetData.Read().Any() || database.PetRarityData.Read().Any();
                if (hasPetData)
                {
                    Logger.Info("Detected legacy pet data file, migrating to SQLite.");
                    if (hasPetDatabase)
                    {
                        Logger.Error("Pet database already contains data.");
                        throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                    }
                    return PetDataImport.ImportPetDataIntoSql(content, listPath, PetDataImport.PetFolder, database.PetData, database.PetRarityData);
                }
            }
            return new Dictionary<int, int>();
        }

        private static async Task RollbackPetData(IConnectionManager connectionManager)
        {
            using (var database = await connectionManager.OpenConnection())
            {
                database.PetData.DeleteAll();
                database.PetRarityData.DeleteAll();
            }
        }

        private static void FinalizePetData()
        {
            var content = PetDataImport.ContentFolderName;
            var listPath = PetDataImport.PetListPath;
            FileSystem.Move($"{content}/{listPath}", $"{content}/{listPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            Logger.Info("Pet data migration complete!");
        }

        private static async Task<Dictionary<int, int>> ImportItemData(IConnectionManager connectionManager)
        {
            var content = ItemDataImport.ContentFolderName;
            var listPath = ItemDataImport.ItemListPath;
            var hasItemData = FileSystem.Exists($"{content}/{listPath}");
            using (var database = await connectionManager.OpenConnection())
            {
                var hasItemDatabase = database.ItemData.Read().Any() || database.ItemTypeData.Read().Any() || database.ItemSlotData.Read().Any() || database.ItemQualityData.Read().Any();
                if (hasItemData)
                {
                    Logger.Info("Detected legacy item data files, migrating to SQLite.");
                    if (hasItemDatabase)
                    {
                        Logger.Error("Item database already contains data.");
                        throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                    }
                    return ItemDataImport.ImportItemDataIntoSql(content, listPath, ItemDataImport.ItemFolder, database.ItemData, database.ItemTypeData, database.ItemSlotData, database.ItemQualityData);
                }
            }
            return new Dictionary<int, int>();
        }

        private static async Task RollbackItemData(IConnectionManager connectionManager)
        {
            using (var database = await connectionManager.OpenConnection())
            {
                database.ItemData.DeleteAll();
                database.ItemTypeData.DeleteAll();
                database.ItemSlotData.DeleteAll();
                database.ItemQualityData.DeleteAll();
            }
        }

        private static void FinalizeItemData()
        {
            var content = ItemDataImport.ContentFolderName;
            var listPath = ItemDataImport.ItemListPath;
            FileSystem.Move($"{content}/{listPath}", $"{content}/{listPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            Logger.Info("Item data migration complete!");
        }

        private static async Task<bool> ImportDungeonData(IConnectionManager connectionManager, Dictionary<int, int> itemMap)
        {
            var content = DungeonDataImport.ContentFolderName;
            var listPath = DungeonDataImport.DungeonListPath;
            var hasDungeonData = FileSystem.Exists($"{content}/{listPath}");
            using (var database = await connectionManager.OpenConnection())
            {
                var hasDungeonDatabase = database.DungeonData.Read().Any() || database.DungeonTimerData.Read().Any() || database.DungeonModeData.Read().Any();
                if (hasDungeonData)
                {
                    Logger.Info("Detected legacy dungeon data files, migrating to SQLite.");
                    if (hasDungeonDatabase)
                    {
                        Logger.Error("Dungeon database already contains data.");
                        throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                    }
                    DungeonDataImport.ImportDungeonDataIntoSql(content, listPath, DungeonDataImport.DungeonFolder, database.DungeonData, database.DungeonTimerData, database.DungeonModeData, itemMap.ToDictionary(x => x.Key, x => database.ItemData.ReadById(x.Value)));
                }
            }
            return true;
        }

        private static async Task RollbackDungeonData(IConnectionManager connectionManager)
        {
            using (var database = await connectionManager.OpenConnection())
            {
                database.DungeonData.DeleteAll();
                database.DungeonTimerData.DeleteAll();
            }
        }

        private static async Task RollbackPlayerData(IConnectionManager connectionManager)
        {
            using (var database = await connectionManager.OpenConnection())
            {
                database.PlayerCharacters.DeleteAll();
                database.CharacterClassData.DeleteAll();
                database.Inventories.DeleteAll();
                database.Stables.DeleteAll();
            }
        }

        private static void FinalizeDungeonData()
        {
            var content = DungeonDataImport.ContentFolderName;
            var listPath = DungeonDataImport.DungeonListPath;
            FileSystem.Move($"{content}/{listPath}", $"{content}/{listPath}.{DateTime.Now.ToFileTimeUtc()}.backup");
            Logger.Info("Dungeon data migration complete!");
        }

        private static async Task<bool> ImportPlayerData(
            IConnectionManager connectionManager,
            UserController userController,
            Dictionary<int, int> itemMap,
            Dictionary<int, int> petMap)
        {
            var hasCoinData = FileSystem.Exists(PlayerDataImport.CoinDataPath);
            var hasXpData = FileSystem.Exists(PlayerDataImport.ExperienceDataPath);
            var hasClassData = FileSystem.Exists(PlayerDataImport.ClassDataPath);
            var hasPlayerDatabase = false;
            using (var database = await connectionManager.OpenConnection())
            {
                hasPlayerDatabase = database.PlayerCharacters.Read().Any() || database.CharacterClassData.Read().Any() || database.Inventories.Read().Any() || database.Stables.Read().Any();
            }
            if (hasCoinData && hasXpData && hasClassData)
            {
                Logger.Info("Detected legacy player data files, migrating to SQLite.");
                if (hasPlayerDatabase)
                {
                    Logger.Error("Player database already contains data, aborting import.");
                    throw new Exception("Legacy import error. Aborting import to avoid data loss.");
                }
                return await PlayerDataImport.ImportPlayerDataIntoSql(PlayerDataImport.CoinDataPath, PlayerDataImport.ExperienceDataPath, PlayerDataImport.ClassDataPath, connectionManager, userController, itemMap, petMap);
            }
            return false;
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

        private static async Task<bool> ImportPlayerData(IConnectionManager connectionManager, UserController userController)
        {
            var petMap = await ImportPetData(connectionManager);
            if (petMap.Any())
            {
                var itemMap = await ImportItemData(connectionManager);
                if (itemMap.Any())
                {
                    var dungeonImport = await ImportDungeonData(connectionManager, itemMap);
                    if (dungeonImport)
                    {
                        var playerImport = await ImportPlayerData(connectionManager, userController, itemMap, petMap);
                        if (playerImport)
                        {
                            FinalizePlayerData();
                            FinalizeDungeonData();
                            FinalizeItemData();
                            FinalizePetData();
                            return true;
                        }
                        await RollbackPlayerData(connectionManager);
                        await RollbackDungeonData(connectionManager);
                    }
                    await RollbackItemData(connectionManager);
                }
                await RollbackPetData(connectionManager);
            }
            return false;
        }

        public static async Task ImportLegacyData(IConnectionManager connectionManager, UserController userController)
        {
            await ImportFishData(connectionManager);
            await ImportFisherData(connectionManager, userController);
            var playerImport = await ImportPlayerData(connectionManager, userController);
            if (playerImport)
            {
                Logger.Info("All legacy data imported into database.");
            }
        }
    }
}
