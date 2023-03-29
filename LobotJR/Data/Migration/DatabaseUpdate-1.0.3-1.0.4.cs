using NuGet.Versioning;
using System;
using System.Data.Entity;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_3_1_0_4 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 3);
        public SemanticVersion ToVersion => new SemanticVersion(1, 0, 4);
        public bool UsesMetadata => true;


        public DatabaseMigrationResult Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "ALTER TABLE \"AppSettings\" ADD COLUMN [LoggingMaxSize] int",
                "ALTER TABLE \"AppSettings\" ADD COLUMN [LoggingMaxArchives] int",
                "UPDATE \"AppSettings\" SET [LoggingMaxSize] = 8, [LoggingMaxArchives] = 8"
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
