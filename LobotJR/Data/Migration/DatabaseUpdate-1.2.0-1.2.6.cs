﻿using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_2_0_1_2_6 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 2, 0);
        public SemanticVersion ToVersion => new SemanticVersion(1, 2, 6);
        public bool UsesMetadata => true;

        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                //Cleanup database errors from previous versions
                "ALTER TABLE \"ItemQualities\" ADD COLUMN [Color] nvarchar",
                "ALTER TABLE \"PetRarities\" ADD COLUMN [Color] nvarchar",
                "UPDATE \"ItemQualities\" SET [Color] = '#FFFFFF' WHERE [Id] = 1",
                "UPDATE \"PetRarities\" SET [Color] = '#FFFFFF' WHERE [Id] = 1",
                "UPDATE \"ItemQualities\" SET [Color] = '#6495ED' WHERE [Id] = 2",
                "UPDATE \"PetRarities\" SET [Color] = '#6495ED' WHERE [Id] = 2",
                "UPDATE \"ItemQualities\" SET [Color] = '#9932CC' WHERE [Id] = 3",
                "UPDATE \"PetRarities\" SET [Color] = '#9932CC' WHERE [Id] = 3",
                "UPDATE \"ItemQualities\" SET [Color] = '#FFA500' WHERE [Id] = 4",
                "UPDATE \"PetRarities\" SET [Color] = '#FFA500' WHERE [Id] = 4",
                "UPDATE \"ItemQualities\" SET [Color] = '#B22222' WHERE [Id] = 5",
                "UPDATE \"PetRarities\" SET [Color] = '#B22222' WHERE [Id] = 5",
                "PRAGMA foreign_keys=OFF",
                // "BEGIN TRANSACTION",
                "CREATE TABLE \"DungeonParticipants_new\" ([Id] INTEGER PRIMARY KEY, [HistoryId] int NOT NULL, [UserId] nvarchar NOT NULL, [WaitTime] int NOT NULL, [ExperienceEarned] int NOT NULL, [CurrencyEarned] int NOT NULL, [ItemDropId] int, [PetDropId] int, FOREIGN KEY (HistoryId) REFERENCES \"DungeonHistories\"(Id), FOREIGN KEY (ItemDropId) REFERENCES \"Items\"(Id), FOREIGN KEY (PetDropId) REFERENCES \"Pets\"(Id))",
                "INSERT INTO \"DungeonParticipants_New\" ([HistoryId], [UserId], [WaitTime], [ExperienceEarned], [CurrencyEarned], [ItemDropId], [PetDropId]) SELECT [HistoryId], [UserId], [WaitTime], [ExperienceEarned], [CurrencyEarned], [ItemDropId], [PetDropId] FROM \"DungeonParticipants\"",
                "DROP TABLE \"DungeonParticipants\"",
                "ALTER TABLE \"DungeonParticipants_New\" RENAME TO \"DungeonParticipants\"",
                "UPDATE \"DungeonParticipants\" SET [ItemDropId] = NULL WHERE [ItemDropId] = 0",
                "UPDATE \"DungeonParticipants\" SET [PetDropId] = NULL WHERE [PetDropId] = 0",
                // "COMMIT",
                "PRAGMA foreign_keys=ON",
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
