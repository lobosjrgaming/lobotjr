using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for AwardSettings.xaml
    /// </summary>
    public partial class TournamentSettings : UserControl, ISettingsPage
    {
        public string Category => "RPG.Fishing";
        public string PageName => "Tournament";

        public TournamentSettings()
        {
            InitializeComponent();
        }
    }
}
