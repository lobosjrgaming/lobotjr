using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for AwardSettings.xaml
    /// </summary>
    public partial class PriceSettings : UserControl, ISettingsPage
    {
        public string Category => "RPG";
        public string PageName => "Prices";

        public PriceSettings()
        {
            InitializeComponent();
        }
    }
}
