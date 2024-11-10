using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for BehaviorSettings.xaml
    /// </summary>
    public partial class BehaviorSettings : UserControl, ISettingsPage
    {
        public string Category => "App";
        public string PageName => "Core";

        public BehaviorSettings()
        {
            InitializeComponent();
        }
    }
}
