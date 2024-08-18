using NuGet.Versioning;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_4_1_0_5 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 4);
        public SemanticVersion ToVersion => new SemanticVersion(1, 0, 5);
        public bool UsesMetadata => true;


        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "ALTER TABLE \"AppSettings\" ADD COLUMN [MaxWhisperRecipients] int",
                "UPDATE \"AppSettings\" SET [MaxWhisperRecipients] = 0"
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
            return Task.FromResult(result);
        }
    }
}
