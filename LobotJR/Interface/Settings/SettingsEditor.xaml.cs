using LobotJR.Data;
using LobotJR.Shared.Client;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsEditor : Window
    {
        private TwitchClientSettings TwitchClientSettings;
        private BehaviorSettings BehaviorSettings;


        private IEnumerable<ISettingsPage<GameSettings>> GameSettingsPages = new List<ISettingsPage<GameSettings>>();
        private IEnumerable<ISettingsPage<AppSettings>> AppSettingsPages = new List<ISettingsPage<AppSettings>>();
        private IEnumerable<ISettingsPage<ClientData>> ClientSettingsPages = new List<ISettingsPage<ClientData>>();

        public SettingsEditor()
        {
            InitializeComponent();
        }

        private void CategoryView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selected = CategoryView.SelectedItem as TreeViewItem;
            if (selected != null)
            {
                selected.
            }
        }
    }
}
