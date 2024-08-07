using NuGet.Versioning;
using System;
using System.Data.Entity;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_7_1_0_8 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 7);
        public SemanticVersion ToVersion => new SemanticVersion(1, 0, 8);
        public bool UsesMetadata => true;

        public DatabaseMigrationResult Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                //Add content tables
                "CREATE TABLE \"GameSettings\" ([Id] INTEGER PRIMARY KEY, [ExperienceFrequency] int NOT NULL, [ExperienceValue] int NOT NULL, [CoinValue] int NOT NULL, [SubRewardMultiplier] int NOT NULL, [RespecCost] int NOT NULL, [PryCost] int NOT NULL, [FishingCastMinimum] int NOT NULL, [FishingCastMaximum] int NOT NULL, [FishingHookLength] int NOT NULL, [FishingUseNormalRarity] bit NOT NULL, [FishingUseNormalSizes] bit NOT NULL, [FishingGloatCost] int NOT NULL, [FishingTournamentDuration] int NOT NULL, [FishingTournamentInterval] int NOT NULL, [FishingTournamentCastMinimum] int NOT NULL, [FishingTournamentCastMaximum] int NOT NULL)",
                //TODO: Add game settings that don't have comments yet
                "CREATE TABLE \"ItemQualities\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [DropRatePenalty] int NOT NULL)",
                "CREATE TABLE \"ItemSlots\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [MaxEquipped] int NOT NULL)",
                "CREATE TABLE \"ItemTypes\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar)",
                "CREATE TABLE \"Items\" ([Id] INTEGER PRIMARY KEY, [QualityId] int NOT NULL, [SlotId] int NOT NULL, [TypeId] int NOT NULL, [Name] nvarchar, [Description] nvarchar, [Max] int NOT NULL, [SuccessChance] real NOT NULL, [ItemFind] real NOT NULL, [CoinBonus] real NOT NULL, [XpBonus] real NOT NULL, [PreventDeathBonus] real NOT NULL, FOREIGN KEY (QualityId) REFERENCES \"ItemQualities\"(Id), FOREIGN KEY (SlotId) REFERENCES \"ItemSlots\"(Id), FOREIGN KEY (TypeId) REFERENCES \"ItemTypes\"(Id))",
                "CREATE TABLE \"PetRarities\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [DropRate] int NOT NULL)",
                "CREATE TABLE \"Pets\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Description] nvarchar, [RarityId] int NOT NULL, FOREIGN KEY (RarityId) REFERENCES \"PetRarities\"(Id))",
                "CREATE TABLE \"DungeonTimers\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [BaseTime] datetime, [Length] int NOT NULL)",
                "CREATE TABLE \"Dungeons\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Description] nvarchar, [Introduction] nvarchar, [FailureText] nvarchar)",
                "CREATE TABLE \"DungeonModes\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Flag] nvarchar UNIQUE NOT NULL, [IsDefault] bit)",
                "CREATE TABLE \"LevelRanges\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [ModeId] int NOT NULL, [Minimum] int NOT NULL, [Maximum] int NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id), FOREIGN KEY (ModeId) REFERENCES \"DungeonModes\"(Id))",
                "CREATE TABLE \"Loot\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [ItemId] int NOT NULL, [DropChance] real NOT NULL, [ModeId] int NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id), FOREIGN KEY (ItemId) REFERENCES \"Items\"(Id), FOREIGN KEY (ModeId) REFERENCES \"DungeonModes\"(Id))",
                "CREATE TABLE \"Encounters\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [Enemy] nvarchar NOT NULL, [Difficulty] int NOT NULL, [HeroicDifficulty] int NOT NULL, [SetupText] nvarchar NOT NULL, [CompleteText] nvarchar NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id))",
                "CREATE TABLE \"EncounterLevels\" ([Id] INTEGER PRIMARY KEY, [EncounterId] int NOT NULL, [ModeId] int NOT NULL, [Difficulty] int NOT NULL, FOREIGN KEY (EncounterId) REFERENCES \"Encounters\"(Id), FOREIGN KEY (ModeId) REFERENCES \"DungeonModes\"(Id))",
                "CREATE TABLE \"CharacterClasses\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [CanPlay] bit NOT NULL, [SuccessChance] real NOT NULL, [ItemFind] int NOT NULL, [CoinBonus] int NOT NULL, [XpBonus] int NOT NULL, [PreventDeathBonus] real NOT NULL)",
                //Add player tables
                "CREATE TABLE \"PlayerCharacters\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [CharacterClassId] int NOT NULL, [Experience] int NOT NULL, [Currency] int NOT NULL, [Level] int NOT NULL, [Prestige] int NOT NULL, FOREIGN KEY (CharacterClassId) REFERENCES \"CharacterClasses\"(Id))",
                "CREATE TABLE \"Inventories\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [ItemId] int NOT NULL, [Count] int NOT NULL, [IsEquipped] bit NOT NULL, [TimeAdded] datetime NOT NULL, FOREIGN KEY (ItemId) REFERENCES \"Items\"(Id))",
                "CREATE TABLE \"Stables\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [PetId] int NOT NULL, [Name] nvarchar NOT NULL, [Level] int NOT NULL, [Experience] int NOT NULL, [Affection] int NOT NULL, [Hunger] int NOT NULL, [IsSparkly] bit NOT NULL, [IsActive] bit NOT NULL, FOREIGN KEY (PetId) REFERENCES \"Pets\"(Id))",
                "CREATE TABLE \"DungeonLockouts\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [TimerId] int NOT NULL, [Time] datetime NOT NULL, FOREIGN KEY (TimerId) REFERENCES \"DungeonTimers\"(Id))",
                //Move game settings to new table
                //TODO: Add game settings that don't have comments yet
                "INSERT INTO \"GameSettings\" ([ExperiencFrequency], [ExperienceValue], [CoinValue], [SubRewardMultiplier], [RespecCost], [PryCost], [FishingCastMinimum], [FishingCastMaximum], [FishingHookLength], [FishingUseNormalRarity], [FishingUseNormalSizes], [FishingGloatCost], [FishingTournamentDuration], [FishingTournamentInterval], [FishingTournamentCastMinimum], [FishingTournamentCastMaximum]) SELECT 15, 1, 3, 2, 250, 1, [FishingCastMinimum], [FishingCastMaximum], [FishingHookLength], [FishingUseNormalRarity], [FishingUseNormalSizes], [FishingGloatCost], [FishingTournamentDuration], [FishingTournamentInterval], [FishingTournamentCastMinimum], [FishingTournamentCastMaximum] FROM \"AppSettings\"",
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
                //Cleanup database errors from previous versions
                "DROP INDEX \"IX_Catch_Fisher_Id\"",
                "ALTER TABLE \"Catches\" DROP COLUMN [Fisher_Id]",
                //Add missing foreign keys
                "ALTER TABLE \"Enrollments\" RENAME TO \"Enrollments_Old\"",
                "CREATE TABLE \"Enrollments\" ([Id] INTEGER PRIMARY KEY, [GroupId] int, [UserId] nvarchar, FOREIGN KEY (GroupId) REFERENCES \"AccessGroups\"(Id))",
                "INSERT INTO \"Enrollments\" (GroupId, UserId) SELECT [GroupId], [UserId] FROM \"Enrollments_Old\"",
                "DROP TABLE \"Enrollments_Old\"",
                "ALTER TABLE \"Restrictions\" RENAME TO \"Restrictions_Old\"",
                "CREATE TABLE \"Restrictions\" ([Id] INTEGER PRIMARY KEY, [GroupId] int, [Command] nvarchar, FOREIGN KEY (GroupId) REFERENCES \"AccessGroups\"(Id))",
                "INSERT INTO \"Restrictions\" (GroupId, UserId) SELECT [GroupId], [UserId] FROM \"Restrictions_Old\"",
                "DROP TABLE \"Restrictions_Old\"",
                //Add foreign key indices for new tables
                "CREATE INDEX \"IX_Items_Quality_Id\" ON \"Items\" (\"QualityId\")",
                "CREATE INDEX \"IX_Items_Slot_Id\" ON \"Items\" (\"SlotId\")",
                "CREATE INDEX \"IX_Items_Type_Id\" ON \"Items\" (\"TypeId\")",
                "CREATE INDEX \"IX_Pets_Rarity_Id\" ON \"Pets\" (\"RarityId\")",
                "CREATE INDEX \"IX_Loot_Dungeon_Id\" ON \"Loot\" (\"DungeonId\")",
                "CREATE INDEX \"IX_Loot_Item_Id\" ON \"Loot\" (\"ItemId\")",
                "CREATE INDEX \"IX_Encounters_Dungeon_Id\" ON \"Encounters\" (\"DungeonId\")",
                "CREATE INDEX \"IX_PlayerCharacters_CharacterClass_Id\" ON \"PlayerCharacter\" (\"CharacterClassId\")",
                "CREATE INDEX \"IX_Inventories_Item_Id\" ON \"Inventories\" (\"ItemId\")",
                "CREATE INDEX \"IX_Stables_Pet_Id\" ON \"Stables\" (\"PetId\")",
                "CREATE INDEX \"IX_DungeonLockout_Timer_Id\" ON \"DungeonLockout\" (\"TimerId\")",
                //Add performance indices for new tables
                "CREATE INDEX \"IX_Loot_IsHeroic\" ON \"Loot\" (\"IsHeroic\")",
                "CREATE INDEX \"IX_Inventories_IsEquipped\" ON \"Inventories\" (\"IsEquipped\")",
                "CREATE INDEX \"IX_Stables_IsActive\" ON \"Stables\" (\"IsActive\")",
                "CREATE INDEX \"IX_PlayerCharacters_User_Id\" ON \"PlayerCharacters\" (\"UserId\")",
                "CREATE INDEX \"IX_Inventories_User_Id\" ON \"Inventories\" (\"UserId\")",
                "CREATE INDEX \"IX_Stables_User_Id\" ON \"Stables\" (\"UserId\")",
                "CREATE INDEX \"IX_DungeonLockouts_User_Id\" ON \"DungeonLockouts\" (\"UserId\")",
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
                    result.DebugOutput.Add($"Exception: {e}");
                }
            }
            return result;
        }
    }
}
