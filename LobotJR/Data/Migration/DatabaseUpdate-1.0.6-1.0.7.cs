using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    public class DatabaseUpdate_1_0_6_1_0_7 : IDatabaseUpdate
    {
        public SemanticVersion FromVersion => new SemanticVersion(1, 0, 6);
        public SemanticVersion ToVersion => new SemanticVersion(1, 0, 7);
        public bool UsesMetadata => true;

        private class TempEnrollment
        {
            public int Id { get; set; }
            public string UserList { get; set; }
        }

        private class TempRestriction
        {
            public int Id { get; set; }
            public string CommandList { get; set; }
        }

        private static List<string> StringToList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }
            return Regex.Split(value, "(?<!\\\\),").Select(x => x.Replace("\\,", ",")).ToList();
        }

        public Task<DatabaseMigrationResult> Update(DbContext context)
        {
            var result = new DatabaseMigrationResult { Success = true };
            var commands = new string[]
            {
                "CREATE TABLE \"Enrollments\" ([Id] INTEGER PRIMARY KEY, [GroupId] int, [UserId] nvarchar)",
                "CREATE TABLE \"Restrictions\" ([Id] INTEGER PRIMARY KEY, [GroupId] int, [Command] nvarchar)",
                "ALTER TABLE \"AccessGroups\" ADD COLUMN [IncludeMods] bit NOT NULL DEFAULT 0",
                "ALTER TABLE \"AccessGroups\" ADD COLUMN [IncludeVips] bit NOT NULL DEFAULT 0",
                "ALTER TABLE \"AccessGroups\" ADD COLUMN [IncludeSubs] bit NOT NULL DEFAULT 0",
                "ALTER TABLE \"AccessGroups\" ADD COLUMN [IncludeAdmins] bit NOT NULL DEFAULT 0",
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

            var migrationCommands = new List<string>();
            var enrollments = context.Database.SqlQuery<TempEnrollment>("SELECT [Id], [UserList] FROM \"AccessGroups\"");
            foreach (var enrollment in enrollments)
            {
                var users = StringToList(enrollment.UserList);
                foreach (var user in users)
                {
                    migrationCommands.Add($"INSERT INTO \"Enrollments\" ([GroupId], [UserId]) VALUES ('{enrollment.Id}', '{user}')");
                }
            }
            var restrictions = context.Database.SqlQuery<TempRestriction>("SELECT [Id], [CommandList] FROM \"AccessGroups\"");
            foreach (var restriction in restrictions)
            {
                var restrictedCommands = StringToList(restriction.CommandList);
                foreach (var command in restrictedCommands)
                {
                    migrationCommands.Add($"INSERT INTO \"Enrollments\" ([GroupId], [Command]) VALUES ('{restriction.Id}', '{command}')");
                }
            }
            migrationCommands.Add("ALTER TABLE \"AccessGroups\" DROP COLUMN [UserList]");
            migrationCommands.Add("ALTER TABLE \"AccessGroups\" DROP COLUMN [CommandList]");

            foreach (var command in migrationCommands)
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
