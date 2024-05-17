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
                "CREATE TABLE \"ItemQualities\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [DropRatePenalty] int NOT NULL)",
                "CREATE TABLE \"ItemSlots\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [MaxEquipped] int NOT NULL)",
                "CREATE TABLE \"ItemTypes\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar)",
                "CREATE TABLE \"Items\" ([Id] INTEGER PRIMARY KEY, [QualityId] int NOT NULL, [SlotId] int NOT NULL, [TypeId] int NOT NULL, [Name] nvarchar, [Description] nvarchar, [SuccessChance] real NOT NULL, [ItemFind] int NOT NULL, [CoinBonus] int NOT NULL, [XpBonus] int NOT NULL, [PreventDeathBonus] real NOT NULL, FOREIGN KEY (QualityId) REFERENCES \"ItemQualities\"(Id), FOREIGN KEY (SlotId) REFERENCES \"ItemSlots\"(Id), FOREIGN KEY (TypeId) REFERENCES \"ItemTypes\"(Id))",
                "CREATE TABLE \"PetRarities\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [DropRate] int NOT NULL)",
                "CREATE TABLE \"Pets\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Description] nvarchar, [RarityId] int NOT NULL, FOREIGN KEY (RarityId) REFERENCES \"PetRarities\"(Id))",
                "CREATE TABLE \"DungeonTimers\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [BaseTime] datetime, [Length] int NOT NULL)",
                "CREATE TABLE \"Dungeons\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [Description] nvarchar, [Introduction] nvarchar, [FailureText] nvarchar, [LevelMinimum] int NOT NULL, [LevelMaximum] int NOT NULL, [HeroicMinimum] int NOT NULL, [HeroicMaximum] int NOT NULL)",
                "CREATE TABLE \"Loot\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [ItemId] int NOT NULL, [DropChance] real NOT NULL, [IsHeroic] bit NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id), FOREIGN KEY (ItemId) REFERENCES \"Items\"(Id))",
                "CREATE TABLE \"Encounters\" ([Id] INTEGER PRIMARY KEY, [DungeonId] int NOT NULL, [Enemy] nvarchar NOT NULL, [Difficulty] int NOT NULL, [SetupText] nvarchar NOT NULL, [CompleteText] nvarchar NOT NULL, FOREIGN KEY (DungeonId) REFERENCES \"Dungeons\"(Id))",
                "CREATE TABLE \"CharacterClasses\" ([Id] INTEGER PRIMARY KEY, [Name] nvarchar, [CanPlay] bit NOT NULL, [SuccessChance] real NOT NULL, [ItemFind] int NOT NULL, [CoinBonus] int NOT NULL, [XpBonus] int NOT NULL, [PreventDeathBonus] real NOT NULL)",
                //Add player tables
                "CREATE TABLE \"PlayerCharacters\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [CharacterClassId] int NOT NULL, [Experience] int NOT NULL, [Currency] int NOT NULL, [Level] int NOT NULL, [Prestige] int NOT NULL, FOREIGN KEY (CharacterClassId) REFERENCES \"CharacterClasses\"(Id))",
                "CREATE TABLE \"Inventories\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [ItemId] int NOT NULL, [IsEquipped] bit NOT NULL, FOREIGN KEY (ItemId) REFERENCES \"Items\"(Id))",
                "CREATE TABLE \"Stables\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [PetId] int NOT NULL, [Name] nvarchar NOT NULL, [Level] int NOT NULL, [Experience] int NOT NULL, [Affection] int NOT NULL, [Hunger] int NOT NULL, [IsSparkly] bit NOT NULL, [IsActive] bit NOT NULL, FOREIGN KEY (PetId) REFERENCES \"Pets\"(Id))",
                "CREATE TABLE \"DungeonLockouts\" ([Id] INTEGER PRIMARY KEY, [UserId] nvarchar NOT NULL, [TimerId] int NOT NULL, [Time] datetime NOT NULL, FOREIGN KEY (TimerId) REFERENCES \"DungeonTimers\"(Id))",
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
