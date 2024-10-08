﻿using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_1_1_0_2 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 1);
        public SemanticVersion ToVersion => new SemanticVersion(1, 0, 2);
        public bool UsesMetadata => true;


        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "DROP TABLE \"Fishers\"",
                "CREATE TABLE \"DataTimers\" ([Name] TEXT PRIMARY KEY, [Timestamp] datetime NOT NULL)",
                "CREATE TABLE \"Metadata\" ([Id] INTEGER PRIMARY KEY, [DatabaseVersion] nvarchar, [LastSchemaUpdate] datetime)",
                "ALTER TABLE \"AppSettings\" DROP COLUMN [DatabaseVersion]"
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
