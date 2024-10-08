﻿using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_0_1_0_1 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 0);
        public SemanticVersion ToVersion => new SemanticVersion(1, 0, 1);
        public bool UsesMetadata => false;


        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "ALTER TABLE \"Catches\" RENAME COLUMN [Fish_Id] TO [FishId]",
                "ALTER TABLE \"LeaderboardEntries\" RENAME COLUMN [Fish_Id] TO [FishId]",
                "ALTER TABLE \"Fish\" RENAME COLUMN [Rarity_Id] TO [RarityId]",
                "ALTER TABLE \"Fish\" RENAME COLUMN [SizeCategory_Id] TO [SizeCategoryId]"
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
