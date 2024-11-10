using LobotJR.Utils;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for AwardSettings.xaml
    /// </summary>
    public partial class LogSettings : UserControl, ISettingsPage
    {
        public string Category => "Interface";
        public string PageName => "Log";

        public LogSettings()
        {
            InitializeComponent();
        }
        private void SetElementColor(int index, int colorHex)
        {
            var element = Preview.Document.Blocks.ElementAtOrDefault(index);
            if (element != null)
            {
                element.Foreground = InterfaceUtils.BrushFromHex(colorHex);
            }
        }

        private void UpdatePreview()
        {
            if (DataContext is SettingsEditor context)
            {
                var settings = context.ClientSettings;
                Preview.FontFamily = new FontFamily(settings.FontFamily);
                Preview.FontSize = settings.FontSize;
                Preview.Background = InterfaceUtils.BrushFromHex(settings.BackgroundColor);
                SetElementColor(1, settings.DebugColor);
                SetElementColor(2, settings.InfoColor);
                SetElementColor(3, settings.WarningColor);
                SetElementColor(4, settings.ErrorColor);
                SetElementColor(5, settings.CrashColor);
            }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void Preview_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void Family_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }
    }
}
