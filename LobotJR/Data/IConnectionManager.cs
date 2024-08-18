namespace LobotJR.Data
{
    /// <summary>
    /// Factory for creating connections to the database.
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Opens a connection to the database. This connection is not
        /// automatically disposed of, and should always be put in a using
        /// block.
        /// </summary>
        /// <returns>A newly created connection database.</returns>
        IDatabase OpenConnection();
        /// <summary>
        /// The current active connection to the database. Null if no
        /// connection has been opened.
        /// </summary>
        IDatabase CurrentConnection { get; }
        /// <summary>
        /// Creates a new metadata record if none exists in the database.
        /// </summary>
        Metadata SeedMetadata();
        /// <summary>
        /// Creates a new app settings record if none exists in the database.
        /// </summary>
        AppSettings SeedAppSettings();
        /// <summary>
        /// Creates a new game settings record if none exists in the database.
        /// </summary>
        GameSettings SeedGameSettings();
    }
}
