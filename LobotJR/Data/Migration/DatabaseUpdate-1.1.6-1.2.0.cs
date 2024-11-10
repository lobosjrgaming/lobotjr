using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_1_6_1_2_0 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 1, 6);
        public SemanticVersion ToVersion => new SemanticVersion(1, 2, 0);
        public bool UsesMetadata => true;

        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "CREATE TABLE \"ClientSettings\" ([Id] INTEGER PRIMARY KEY, [LogHistorySize] int NOT NULL, [FontFamily] nvarchar NOT NULL, [FontSize] int NOT NULL, [LogFilter] int NOT NULL, [ToolbarDisplay] int NOT NULL, [BackgroundColor] int NOT NULL, [DebugColor] int NOT NULL, [InfoColor] int NOT NULL, [WarningColor] int NOT NULL, [ErrorColor] int NOT NULL, [CrashColor] int NOT NULL)",
                "DROP INDEX \"IX_Loot_IsHeroic\"",
                "DROP INDEX \"IX_DungeonHistories_User_Id\""
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
