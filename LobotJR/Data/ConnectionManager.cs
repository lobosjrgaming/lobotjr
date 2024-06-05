using System;

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
            CurrentConnection = new SqliteRepositoryManager(new SqliteContext());
            return CurrentConnection;
        }
    }
}
