using LobotJR.Data;
using LobotJR.Shared.Client;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsEditor : Window
    {
        private readonly IEnumerable<UserControl> Pages = new List<UserControl>()
        {
            new TwitchClientSettings(),
            new BehaviorSettings(),

            new AwardSettings()
        };
        private Dictionary<string, UserControl> TreeMap;

        public AppSettings AppSettings { get; set; } = new AppSettings();
        public GameSettings GameSettings { get; set; } = new GameSettings();
        public ClientSettings ClientSettings { get; set; } = new ClientSettings();
        public ClientData ClientData { get; set; } = new ClientData()
        {
            ClientId = "TestId",
            ClientSecret = "TestSecret"
        };

        public SettingsEditor()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TreeMap = Pages.ToDictionary(x => (x as ISettingsPage).PageName, x => x);
            var categories = new Dictionary<string, TreeViewItem>();
            foreach (var control in Pages)
            {
                control.DataContext = this;
                var page = control as ISettingsPage;
                if (categories.TryGetValue(page.Category, out var value))
                {
                    var newItem = new TreeViewItem() { Header = page.PageName };
                    value.Items.Add(newItem);
                }
                else
                {
                    var chain = page.Category.Split('.');
                    var parent = CategoryView as ItemsControl;
                    for (var i = 0; i < chain.Length; i++)
                    {
                        var step = chain[i];
                        if (categories.TryGetValue(step, out var stepValue))
                        {
                            parent = stepValue;
                        }
                        else
                        {
                            var newControl = new TreeViewItem { Header = step };
                            parent.Items.Add(newControl);
                            parent = newControl;
                            categories.Add(string.Join(",", chain.Take(i + 1)), newControl);
                        }
                    }
                    parent.Items.Add(new TreeViewItem() { Header = page.PageName });
                }
                TreeMap.Add($"{page.Category}.{page.PageName}", control);
            }
        }

        private void CategoryView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (CategoryView.SelectedItem is TreeViewItem selected)
            {
                var parentItem = selected.Parent as TreeViewItem;
                var key = selected.Header.ToString();
                while (parentItem != null)
                {
                    key = $"{parentItem.Header}.{key}";
                    parentItem = parentItem.Parent as TreeViewItem;
                }
                if (TreeMap.TryGetValue(key, out var page))
                {
                    SettingsContainer.Children.Clear();
                    SettingsContainer.Children.Add(page);
                }
            }
        }
    }
}
