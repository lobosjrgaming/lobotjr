using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for AwardSettings.xaml
    /// </summary>
    public partial class DungeonSettings : UserControl, ISettingsPage
    {
        public string Category => "RPG";
        public string PageName => "Dungeon";

        public DungeonSettings()
        {
            InitializeComponent();
        }
    }
}
