using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;
using System;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_2_6_1_3_0 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 2, 6);
        public SemanticVersion ToVersion => new SemanticVersion(1, 3, 0);
        public bool UsesMetadata => true;

        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "ALTER TABLE \"Dungeons\" ADD COLUMN [CommandId] int NOT NULL DEFAULT 0",
                "CREATE INDEX \"IX_Dungeons_Command_Id\" ON \"Dungeons\" (\"CommandId\")",
                "UPDATE \"Dungeons\" SET [CommandId] = [Id]"
            };
            result.DebugOutput.Add("Executing SQL statements to add/update tables...");
            foreach (var command in commands)
            {
                result.DebugOutput.Add(command);
                try
                {
                    context.Database.ExecuteSqlRaw(command);
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
