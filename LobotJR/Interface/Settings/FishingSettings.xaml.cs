using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for AwardSettings.xaml
    /// </summary>
    public partial class FishingSettings : UserControl, ISettingsPage
    {
        public string Category => "RPG.Fishing";
        public string PageName => "Base";

        public FishingSettings()
        {
            InitializeComponent();
        }
    }
}
