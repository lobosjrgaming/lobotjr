using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_7_1_1_0 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 7);
        public SemanticVersion ToVersion => new SemanticVersion(1, 1, 0);
        public bool UsesMetadata => true;

        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                //Add content tables
                "CREATE TABLE \"GameSettings\" ([Id] INTEGER PRIMARY KEY, [ExperienceFrequency] int NOT NULL, [ExperienceValue] int NOT NULL, [CoinValue] int NOT NULL, [SubRewardMultiplier] int NOT NULL, [RespecCost] int NOT NULL, [PryCost] int NOT NULL, [LevelGloatCost] int NOT NULL, [PetGloatCost] int NOT NULL, [PetExperienceToLevel] int NOT NULL, [PetLevelMax] int NOT NULL, [PetFeedingAffection] int NOT NULL, [PetFeedingCost] int NOT NULL, [PetHungerMax] int NOT NULL, "
                    + "[DungeonPartySize] int NOT NULL, [DungeonBaseCost] int NOT NULL, [DungeonLevelCost] int NOT NULL, [DungeonStepTime] int NOT NULL, [DungeonDeathChance] real NOT NULL, [DungeonCritChance] real NOT NULL, [DungeonCritBonus] real NOT NULL, [DungeonLevelRestrictions] bit NOT NULL, "
                    + "[FishingCastMinimum] int NOT NULL, [FishingCastMaximum] int NOT NULL, [FishingHookLength] int NOT NULL, [FishingUseNormalRarity] bit NOT NULL, [FishingUseNormalSizes] bit NOT NULL, [FishingGloatCost] int NOT NULL, [FishingTournamentDuration] int NOT NULL, [FishingTournamentInterval] int NOT NULL, [FishingTournamentCastMinimum] int NOT NULL, [FishingTournamentCastMaximum] int NOT NULL)",
                "CREATE TABLE \"ItemQualities\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [DropRatePenalty] int NOT NULL)",
                "CREATE TABLE \"ItemSlots\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [MaxEquipped] int NOT NULL)",
                "CREATE TABLE \"ItemTypes\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar)",
                "CREATE TABLE \"Items\" ([Id] INTEGER PRIMARY KEY, [QualityId] int NOT NULL, [SlotId] int NOT NULL, [TypeId] int NOT NULL, [Name] nvarchar, [Description] nvarchar, [Max] int NOT NULL, [SuccessChance] real NOT NULL, [ItemFind] real NOT NULL, [CoinBonus] real NOT NULL, [XpBonus] real NOT NULL, [PreventDeathBonus] real NOT NULL, FOREIGN KEY (QualityId) REFERENCES \"ItemQualities\"(Id), FOREIGN KEY (SlotId) REFERENCES \"ItemSlots\"(Id), FOREIGN KEY (TypeId) REFERENCES \"ItemTypes\"(Id))",
                "CREATE TABLE \"PetRarities\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [DropRate] real NOT NULL)",
                "CREATE TABLE \"Pets\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Description] nvarchar, [RarityId] int NOT NULL, FOREIGN KEY (RarityId) REFERENCES \"PetRarities\"(Id))",
                "CREATE TABLE \"DungeonTimers\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [BaseTime] datetime, [Length] int NOT NULL)",
                "CREATE TABLE \"Dungeons\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Description] nvarchar, [Introduction] nvarchar, [FailureText] nvarchar)",
                "CREATE TABLE \"DungeonModes\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Flag] nvarchar UNIQUE NOT NULL, [IsDefault] bit)",
                "CREATE TABLE \"LevelRanges\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [ModeId] int NOT NULL, [Minimum] int NOT NULL, [Maximum] int NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id), FOREIGN KEY (ModeId) REFERENCES \"DungeonModes\"(Id))",
                "CREATE TABLE \"Loot\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [ItemId] int NOT NULL, [DropChance] real NOT NULL, [ModeId] int NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id), FOREIGN KEY (ItemId) REFERENCES \"Items\"(Id), FOREIGN KEY (ModeId) REFERENCES \"DungeonModes\"(Id))",
                "CREATE TABLE \"Encounters\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [Enemy] nvarchar NOT NULL, [SetupText] nvarchar NOT NULL, [CompleteText] nvarchar NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id))",
                "CREATE TABLE \"EncounterLevels\" ([Id] INTEGER PRIMARY KEY, [EncounterId] int NOT NULL, [ModeId] int NOT NULL, [Difficulty] real NOT NULL, FOREIGN KEY (EncounterId) REFERENCES \"Encounters\"(Id), FOREIGN KEY (ModeId) REFERENCES \"DungeonModes\"(Id))",
                "CREATE TABLE \"CharacterClasses\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [CanPlay] bit NOT NULL, [SuccessChance] real NOT NULL, [ItemFind] real NOT NULL, [CoinBonus] real NOT NULL, [XpBonus] real NOT NULL, [PreventDeathBonus] real NOT NULL)",
                "CREATE TABLE \"Equippables\" ([Id] INTEGER PRIMARY KEY, [CharacterClassId] int NOT NULL, [ItemTypeId] int NOT NULL, FOREIGN KEY (CharacterClassId) REFERENCES \"CharacterClasses\"(Id), FOREIGN KEY (ItemTypeId) REFERENCES \"ItemTypes\"(Id))",
                //Add player tables
                "CREATE TABLE \"PlayerCharacters\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [CharacterClassId] int NOT NULL, [Experience] int NOT NULL, [Currency] int NOT NULL, [Level] int NOT NULL, [Prestige] int NOT NULL, FOREIGN KEY (CharacterClassId) REFERENCES \"CharacterClasses\"(Id))",
                "CREATE TABLE \"Inventories\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [ItemId] int NOT NULL, [Count] int NOT NULL, [IsEquipped] bit NOT NULL, [TimeAdded] datetime NOT NULL, FOREIGN KEY (ItemId) REFERENCES \"Items\"(Id))",
                "CREATE TABLE \"Stables\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [PetId] int NOT NULL, [Name] nvarchar NOT NULL, [Level] int NOT NULL, [Experience] int NOT NULL, [Affection] int NOT NULL, [Hunger] int NOT NULL, [IsSparkly] bit NOT NULL, [IsActive] bit NOT NULL, FOREIGN KEY (PetId) REFERENCES \"Pets\"(Id))",
                "CREATE TABLE \"DungeonLockouts\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [TimerId] int NOT NULL, [Time] datetime NOT NULL, FOREIGN KEY (TimerId) REFERENCES \"DungeonTimers\"(Id))",
                "CREATE TABLE \"BugReports\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [Message] nvarchar NOT NULL, [ReportTime] datetime NOT NULL, [ResolutionMessage] nvarchar, [ResolveTime] datetime)",
                "CREATE TABLE \"DungeonHistories\" ([Id] INTEGER PRIMARY KEY, [Date] datetime NOT NULL, [IsQueueGroup] bit, [DungeonId] int NOT NULL, [ModeId] int NOT NULL, [StepsComplete] int NOT NULL, [Success] bit, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id), FOREIGN KEY (ModeId) REFERENCES \"DungeonModes\"(Id))",
                "CREATE TABLE \"DungeonParticipants\" ([Id] INTEGER PRIMARY KEY, [HistoryId] int NOT NULL, [UserId] string NOT NULL, [WaitTime] int, [ExperienceEarned] int, [CurrencyEarned] int, [ItemDropId] int, [PetDropId] int, FOREIGN KEY (HistoryId) REFERENCES \"DungeonHistories\"(Id), FOREIGN KEY (ItemDropId) REFERENCES \"Items\"(Id), FOREIGN KEY (PetDropId) REFERENCES \"Pets\"(Id))",
                //Move game settings to new table
                "INSERT INTO \"GameSettings\" ([ExperienceFrequency], [ExperienceValue], [CoinValue], [SubRewardMultiplier], [RespecCost], [PryCost], [LevelGloatCost], [PetGloatCost], [PetExperienceToLevel], [PetLevelMax], [PetFeedingAffection], [PetFeedingCost], [PetHungerMax], [DungeonPartySize], [DungeonBaseCost], [DungeonLevelCost], [DungeonStepTime], [DungeonDeathChance], [DungeonCritChance], [DungeonCritBonus], [DungeonLevelRestrictions], [FishingCastMinimum], [FishingCastMaximum], [FishingHookLength], [FishingUseNormalRarity], [FishingUseNormalSizes], [FishingGloatCost], [FishingTournamentDuration], [FishingTournamentInterval], [FishingTournamentCastMinimum], [FishingTournamentCastMaximum]) "
                    + "SELECT 15, 1, 3, 2, 250, 1, 25, 25, 150, 10, 5, 5, 100, 3, 25, 10, 9000, 0.25, 0.25, 1, 0, [FishingCastMinimum], [FishingCastMaximum], [FishingHookLength], [FishingUseNormalRarity], [FishingUseNormalSizes], [FishingGloatCost], [FishingTournamentDuration], [FishingTournamentInterval], [FishingTournamentCastMinimum], [FishingTournamentCastMaximum] FROM \"AppSettings\"",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingCastMinimum]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingCastMaximum]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingHookLength]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingUseNormalRarity]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingUseNormalSizes]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingGloatCost]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingTournamentDuration]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingTournamentInterval]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingTournamentCastMinimum]",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [FishingTournamentCastMaximum]",
                "ALTER TABLE \"AppSettings\" ADD COLUMN [TwitchPlays] NOT NULL DEFAULT 0",
                "ALTER TABLE \"Users\" ADD COLUMN [BanTime] datetime",
                "ALTER TABLE \"Users\" ADD COLUMN [BanMessage] nvarchar",
                //Cleanup database errors from previous versions
                "PRAGMA foreign_keys=OFF",
                // "BEGIN TRANSACTION",
                "CREATE TABLE \"Catches_New\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar, [Length] real NOT NULL, [Weight] real NOT NULL, [Points] int NOT NULL, [FishId] int, FOREIGN KEY (FishId) REFERENCES \"Fish\"(Id))",
                "INSERT INTO \"Catches_New\" ([UserId], [Length], [Weight], [Points], [FishId]) SELECT [UserId], [Length], [Weight], [Points], [FishId] FROM \"Catches\"",
                "DROP TABLE \"Catches\"",
                "ALTER TABLE \"Catches_New\" RENAME TO \"Catches\"",
                // "COMMIT",
                "PRAGMA foreign_keys=ON",
                //Add missing foreign keys
                "ALTER TABLE \"Enrollments\" RENAME TO \"Enrollments_Old\"",
                "CREATE TABLE \"Enrollments\" ([Id] INTEGER PRIMARY KEY, [GroupId] int, [UserId] nvarchar, FOREIGN KEY (GroupId) REFERENCES \"AccessGroups\"(Id))",
                "INSERT INTO \"Enrollments\" (GroupId, UserId) SELECT [GroupId], [UserId] FROM \"Enrollments_Old\"",
                "DROP TABLE \"Enrollments_Old\"",
                "ALTER TABLE \"Restrictions\" RENAME TO \"Restrictions_Old\"",
                "CREATE TABLE \"Restrictions\" ([Id] INTEGER PRIMARY KEY, [GroupId] int, [Command] nvarchar, FOREIGN KEY (GroupId) REFERENCES \"AccessGroups\"(Id))",
                "INSERT INTO \"Restrictions\" (GroupId, Command) SELECT [GroupId], [Command] FROM \"Restrictions_Old\"",
                "DROP TABLE \"Restrictions_Old\"",
                //Add foreign key indices for new tables
                "CREATE INDEX \"IX_Items_Quality_Id\" ON \"Items\" (\"QualityId\")",
                "CREATE INDEX \"IX_Items_Slot_Id\" ON \"Items\" (\"SlotId\")",
                "CREATE INDEX \"IX_Items_Type_Id\" ON \"Items\" (\"TypeId\")",
                "CREATE INDEX \"IX_Pets_Rarity_Id\" ON \"Pets\" (\"RarityId\")",
                "CREATE INDEX \"IX_Loot_Dungeon_Id\" ON \"Loot\" (\"DungeonId\")",
                "CREATE INDEX \"IX_Loot_Item_Id\" ON \"Loot\" (\"ItemId\")",
                "CREATE INDEX \"IX_Encounters_Dungeon_Id\" ON \"Encounters\" (\"DungeonId\")",
                "CREATE INDEX \"IX_PlayerCharacters_CharacterClass_Id\" ON \"PlayerCharacters\" (\"CharacterClassId\")",
                "CREATE INDEX \"IX_Inventories_Item_Id\" ON \"Inventories\" (\"ItemId\")",
                "CREATE INDEX \"IX_Stables_Pet_Id\" ON \"Stables\" (\"PetId\")",
                "CREATE INDEX \"IX_DungeonLockout_Timer_Id\" ON \"DungeonLockouts\" (\"TimerId\")",
                "CREATE INDEX \"IX_Equippables_CharacterClass_Id\" ON \"Equippables\" (\"CharacterClassId\")",
                "CREATE INDEX \"IX_Equippables_ItemType_Id\" ON \"Equippables\" (\"ItemTypeId\")",
                "CREATE INDEX \"IX_DungeonHistories_Dungeon_Id\" ON \"DungeonHistories\" (\"DungeonId\")",
                "CREATE INDEX \"IX_DungeonHistories_DungeonMode_Id\" ON \"DungeonHistories\" (\"ModeId\")",
                "CREATE INDEX \"IX_DungeonParticipants_History_Id\" ON \"DungeonParticipants\" (\"HistoryId\")",
                "CREATE INDEX \"IX_DungeonParticipants_ItemDrop_Id\" ON \"DungeonParticipants\" (\"ItemDropId\")",
                "CREATE INDEX \"IX_DungeonParticipants_PetDrop_Id\" ON \"DungeonParticipants\" (\"PetDropId\")",
                //Add performance indices for new tables
                "CREATE INDEX \"IX_Loot_IsHeroic\" ON \"Loot\" (\"IsHeroic\")",
                "CREATE INDEX \"IX_Inventories_IsEquipped\" ON \"Inventories\" (\"IsEquipped\")",
                "CREATE INDEX \"IX_Stables_IsActive\" ON \"Stables\" (\"IsActive\")",
                "CREATE INDEX \"IX_PlayerCharacters_User_Id\" ON \"PlayerCharacters\" (\"UserId\")",
                "CREATE INDEX \"IX_Inventories_User_Id\" ON \"Inventories\" (\"UserId\")",
                "CREATE INDEX \"IX_Stables_User_Id\" ON \"Stables\" (\"UserId\")",
                "CREATE INDEX \"IX_DungeonLockouts_User_Id\" ON \"DungeonLockouts\" (\"UserId\")",
                "CREATE INDEX \"IX_DungeonHistories_User_Id\" ON \"DungeonHistories\" (\"UserId\")",
                //Add foreign key indices for existing tables
                "CREATE INDEX \"IX_Enrollments_Group_Id\" ON \"Enrollments\" (\"GroupId\")",
                "CREATE INDEX \"IX_LeaderboardEntries_Fish_Id\" ON \"LeaderboardEntries\" (\"FishId\")",
                "CREATE INDEX \"IX_Restrictions_Group_Id\" ON \"Restrictions\" (\"GroupId\")",
                //Add performance indices for existing tables
                "CREATE INDEX \"IX_Users_Twitch_Id\" ON \"Users\" (\"TwitchId\")",
                "CREATE INDEX \"IX_TournamentEntries_User_Id\" ON \"TournamentEntries\" (\"UserId\")",
                "CREATE INDEX \"IX_LeaderboardEntries_User_Id\" ON \"LeaderboardEntries\" (\"UserId\")",
                "CREATE INDEX \"IX_Catches_User_Id\" ON \"Catches\" (\"UserId\")",
                "CREATE INDEX \"IX_Enrollments_User_Id\" ON \"Enrollments\" (\"UserId\")",
                "CREATE INDEX \"IX_Restrictions_Command\" ON \"Restrictions\" (\"Command\")",
            };
            result.DebugOutput.Add("Executing SQL statements to add/update tables...");
            foreach (var command in commands)
            {
                result.DebugOutput.Add(command);
                try
                {
                    context.Database.ExecuteSqlCommand(command);
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.DebugOutput.Add($"Exception: {e}");
                }
            }
            return Task.FromResult(result);
        }
    }
}
