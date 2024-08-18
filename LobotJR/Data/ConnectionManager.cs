using System;
using System.Linq;

namespace LobotJR.Data
{
    /// <summary>
    /// Factory for creating connections to the database.
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        /// <summary>
        /// The current active connection to the database. If no connection has
        /// been opened, it will return null instead.
        /// </summary>
        /// <returns>The current database connection.</returns>
        public IDatabase CurrentConnection { get; private set; }

        /// <summary>
        /// Opens a connection to the database. This should be used in a using
        /// block, with all changes saved just before the context is disposed.
        /// </summary>
        /// <returns>A new connection to the database.</returns>
        /// <exception cref="Exception">Thrown if a connection is already open.</exception>
        public IDatabase OpenConnection()
        {
            if (CurrentConnection != null)
            {
                throw new Exception("Failed to open database connection, a connection is already open!");
            }
            var context = new SqliteContext();
            context.Initialize();
            CurrentConnection = new SqliteRepositoryManager(context);
            return CurrentConnection;
        }

        /// <summary>
        /// Gets the metadata record for this database. If no record exists, a
        /// new one will be created with default values.
        /// </summary>
        public Metadata SeedMetadata()
        {
            var metadata = CurrentConnection.Metadata.Read().FirstOrDefault();
            if (metadata == null)
            {
                metadata = new Metadata();
                CurrentConnection.Metadata.Create(metadata);
            }
            return metadata;
        }

        /// <summary>
        /// Gets the app settings for this database. If no record exists, a new
        /// one will be created with default values.
        /// </summary>
        public AppSettings SeedAppSettings()
        {
            var appSettings = CurrentConnection.AppSettings.Read().FirstOrDefault();
            if (appSettings == null)
            {
                appSettings = new AppSettings();
                CurrentConnection.AppSettings.Create(appSettings);
            }
            return appSettings;
        }

        /// <summary>
        /// Gets the game settings for this database. If no record exists, a
        /// new one will be created with default values.
        /// </summary>
        public GameSettings SeedGameSettings()
        {
            var gameSettings = CurrentConnection.GameSettings.Read().FirstOrDefault();
            if (gameSettings == null)
            {
                gameSettings = new GameSettings();
                CurrentConnection.GameSettings.Create(gameSettings);
            }
            return gameSettings;
        }
    }
}
