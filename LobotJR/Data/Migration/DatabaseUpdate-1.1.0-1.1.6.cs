using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_1_0_1_1_6 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 1, 0);
        public SemanticVersion ToVersion => new SemanticVersion(1, 1, 6);
        public bool UsesMetadata => true;

        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "PRAGMA foreign_keys=OFF",
                "DROP INDEX \"IX_Users_Twitch_Id\"",
                "CREATE TABLE \"Users_New\" ([Id] INTEGER PRIMARY KEY, [Username] nvarchar NOT NULL, [TwitchId] nvarchar NOT NULL UNIQUE, [IsMod] bit NOT NULL DEFAULT 0, [IsVip] bit NOT NULL DEFAULT 0, [IsSub] bit NOT NULL DEFAULT 0, [IsAdmin] bit NOT NULL DEFAULT 0, [BanTime] datetime, [BanMessage] nvarchar)",
                "INSERT INTO \"Users_New\" ([Id], [Username], [TwitchId], [IsMod], [IsVip], [IsSub], [IsAdmin], [BanTime], [BanMessage]) SELECT [Id], [Username], [TwitchId], [IsMod], [IsVip], [IsSub], [IsAdmin], [BanTime], [BanMessage] FROM \"Users\" WHERE [TwitchId] IS NOT NULL AND [TwitchId] != '' GROUP BY [TwitchId]",
                "DROP TABLE \"Users\"",
                "ALTER TABLE \"Users_New\" RENAME TO \"Users\"",
                "CREATE INDEX \"IX_Users_Twitch_Id\" ON \"Users\" (\"TwitchId\")",
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
