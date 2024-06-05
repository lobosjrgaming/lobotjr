using System.Linq;

namespace LobotJR.Data
{
    public class SettingsManager
    {
        private readonly IConnectionManager ConnectionManager;

        public SettingsManager(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        public GameSettings GetGameSettings()
        {
            var existing = ConnectionManager.CurrentConnection.GameSettings.Read().FirstOrDefault();
            if (existing == null)
            {
                existing = new GameSettings();
                ConnectionManager.CurrentConnection.GameSettings.Create(existing);
            }
            return existing;
        }

        public AppSettings GetAppSettings()
        {
            var existing = ConnectionManager.CurrentConnection.AppSettings.Read().FirstOrDefault();
            if (existing == null)
            {
                existing = new AppSettings();
                ConnectionManager.CurrentConnection.AppSettings.Create(existing);
            }
            return existing;
        }
    }
}
