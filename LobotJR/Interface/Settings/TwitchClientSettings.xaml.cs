using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for TwitchAuthSettings.xaml
    /// </summary>
    public partial class TwitchClientSettings : UserControl, ISettingsPage
    {
        public string Category => "App";
        public string PageName => "Twitch Authentication";

        public TwitchClientSettings()
        {
            InitializeComponent();
        }

        private void ClientSecret_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsEditor context)
            {
                context.ClientData.ClientSecret = ClientSecret.Password;
            }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsEditor context)
            {
                ClientSecret.Password = context.ClientData.ClientSecret;
            }
        }
    }
}
