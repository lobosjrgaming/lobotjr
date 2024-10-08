﻿using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Data.Migration
{
    /// <summary>
    /// TODO: Add Comments
    /// </summary>
    public class SqliteDatabaseUpdater
    {

        private readonly IEnumerable<IDatabaseUpdate> DatabaseUpdates;

        public DbContext Context { get; set; }
        public SemanticVersion LatestVersion { get; private set; }
        public SemanticVersion CurrentVersion { get; set; }

        public SqliteDatabaseUpdater(IEnumerable<IDatabaseUpdate> databaseUpdates)
        {
            DatabaseUpdates = databaseUpdates.OrderBy(x => x.ToVersion);
            LatestVersion = DatabaseUpdates.Last().ToVersion;
        }

        /// <summary>
        /// Retrieves the minimal DbContext and database version which can be
        /// safely used to run the updater.
        /// </summary>
        public void Initialize()
        {
            try
            {
                var tempContext = new SqliteUpdateContext();
                var appSettings = tempContext.Metadata.First();
                if (string.IsNullOrWhiteSpace(appSettings?.DatabaseVersion))
                {
                    appSettings.DatabaseVersion = LatestVersion.ToString();
                }
                CurrentVersion = SemanticVersion.Parse(appSettings.DatabaseVersion);
                Context = tempContext;
            }
            catch { }
            if (Context == null)
            {

                try
                {
                    var tempContext = new SqliteDeprecatedContext();
                    var appSettings = tempContext.AppSettings.First();
                    CurrentVersion = SemanticVersion.Parse(appSettings.DatabaseVersion);
                    Context = tempContext;
                }
                catch
                {
                    Context = new SqliteDeprecatedContext();
                }
            }
        }

        private string GetDatabaseFile()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["SqliteContext"].ConnectionString;
            return connectionString.Split('=')[1];
        }

        private string BackupDatabase(string databaseFile, SemanticVersion currentVersion)
        {
            var backupFile = $"{databaseFile}-{currentVersion}-{DateTime.Now.ToFileTimeUtc()}.backup";
            File.Copy(databaseFile, backupFile);
            return backupFile;
        }

        private bool RestoreBackup(string backupFile, string databaseFile)
        {
            try
            {
                Context.Database.Connection.Close();
                Context.Database.Connection.Dispose();
                Context.Dispose();
                GC.Collect();
                File.Delete(databaseFile);
                File.Move(backupFile, databaseFile);
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        private async Task<DatabaseMigrationResult> ProcessDatabaseUpdates(DbContext context, SemanticVersion currentVersion)
        {
            var result = new DatabaseMigrationResult { PreviousVersion = currentVersion };
            var updates = DatabaseUpdates.Where(x => currentVersion == null && x.FromVersion == null || x.FromVersion >= currentVersion).OrderBy(x => x.FromVersion);
            foreach (var update in updates)
            {
                var updateResult = await update.Update(context);
                result.DebugOutput.Add($"Updating database version from {update.FromVersion} to {update.ToVersion}...");
                if (updateResult.Success)
                {
                    result.DebugOutput.AddRange(updateResult.DebugOutput);
                    result.NewVersion = update.ToVersion;
                }
                else
                {
                    result.DebugOutput.Add($"Update failed, restoring database backup.");
                    return result;
                }
            }
            result.Success = true;
            return result;
        }

        /// <summary>
        /// Updates the database schema to the latest version. If the update
        /// fails, the context will be disposed and the database backup will be
        /// restored.
        /// </summary>
        /// <returns>The result of the migration attempt.</returns>
        public async Task<DatabaseMigrationResult> UpdateDatabase()
        {
            if (CurrentVersion < LatestVersion)
            {
                var databaseFile = GetDatabaseFile();
                var backup = BackupDatabase(databaseFile, CurrentVersion);
                var results = await ProcessDatabaseUpdates(Context, CurrentVersion);
                if (!results.Success)
                {
                    RestoreBackup(backup, databaseFile);
                }
                return results;
            }
            return null;
        }

        /// <summary>
        /// To be called after UpdateDatabase returns a success. This has to be
        /// separated so a new context to the database can be created with the
        /// new schema post update.
        /// </summary>
        public void WriteUpdatedVersion()
        {
            Context.Dispose();
            var updateContext = new SqliteUpdateContext();
            var metadataRepo = new SqliteRepository<Metadata>(updateContext);
            var metadata = metadataRepo.Read().FirstOrDefault();
            if (metadata == null)
            {
                metadata = new Metadata();
                metadataRepo.Create(metadata);
            }
            metadata.DatabaseVersion = LatestVersion.ToString();
            metadata.LastSchemaUpdate = DateTime.Now;
            metadataRepo.Commit();
            updateContext.Dispose();
        }
    }
}
