using System.Linq;
using System.Threading.Tasks;

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
        public async Task<IDatabase> OpenConnection()
        {
            while (CurrentConnection != null && !CurrentConnection.IsDisposed)
            {
                await Task.Delay(1);
            }

            var context = new SqliteContext();
            context.Initialize();
            CurrentConnection = new SqliteRepositoryManager(context);
            return CurrentConnection;
        }

        /// <summary>
        /// Creates default data entries in the database needed for the app to
        /// run. Only creates new entries if none already exist.
        /// </summary>
        public void SeedData()
        {
            if (!CurrentConnection.Metadata.Read().Any())
            {
                CurrentConnection.Metadata.Create(new Metadata());
            }
            if (!CurrentConnection.AppSettings.Read().Any())
            {
                CurrentConnection.AppSettings.Create(new AppSettings());
            }
            if (!CurrentConnection.GameSettings.Read().Any())
            {
                CurrentConnection.GameSettings.Create(new GameSettings());
            }
        }
    }
}
