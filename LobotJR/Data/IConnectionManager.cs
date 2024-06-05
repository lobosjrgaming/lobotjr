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
    }
}
