using LobotJR.Data;
using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for BehaviorSettings.xaml
    /// </summary>
    public partial class BehaviorSettings : UserControl, ISettingsPage<AppSettings>
    {
        public string Category => "App";

        public BehaviorSettings()
        {
            InitializeComponent();
        }


        public void Load(AppSettings settingsObject)
        {
            throw new System.NotImplementedException();
        }

        public void Save(AppSettings settingsObject)
        {
            throw new System.NotImplementedException();
        }
    }
}
