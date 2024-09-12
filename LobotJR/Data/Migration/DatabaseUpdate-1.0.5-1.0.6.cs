using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_5_1_0_6 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 5);
        public SemanticVersion ToVersion => new SemanticVersion(1, 0, 6);
        public bool UsesMetadata => true;


        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "ALTER TABLE \"AppSettings\" RENAME COLUMN [GeneralCacheUpdateTime] to [UserDatabaseUpdateTime]",
                "ALTER TABLE \"AppSettings\" ADD COLUMN [UserLookupBatchTime] int NOT NULL DEFAULT 5",
                "ALTER TABLE \"UserMaps\" RENAME TO \"Users\"",
                "ALTER TABLE \"Users\" ADD COLUMN [IsMod] bit NOT NULL DEFAULT 0",
                "ALTER TABLE \"Users\" ADD COLUMN [IsVip] bit NOT NULL DEFAULT 0",
                "ALTER TABLE \"Users\" ADD COLUMN [IsSub] bit NOT NULL DEFAULT 0",
                "ALTER TABLE \"Users\" ADD COLUMN [IsAdmin] bit NOT NULL DEFAULT 0",
                "ALTER TABLE \"UserRoles\" RENAME TO \"AccessGroups\"",
                "UPDATE \"AppSettings\" SET [UserDatabaseUpdateTime] = 15"
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
