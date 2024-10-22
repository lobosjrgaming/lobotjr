using LobotJR.Data;
using LobotJR.Twitch.Api.Client;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
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
        private readonly List<UserControl> Pages = new List<UserControl>()
        {
            new TwitchClientSettings(),
            new BehaviorSettings(),

            new AwardSettings(),
            new PriceSettings(),
            new DungeonSettings(),
            new PetSettings(),
            new FishingSettings(),
            new TournamentSettings(),

            new LogSettings()
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

        public IEnumerable<string> Fonts { get; set; } = Array.Empty<string>();
        public IEnumerable<ToolbarDisplay> ToolbarDisplayEnum { get; set; } = Array.Empty<ToolbarDisplay>();

        public SettingsEditor(bool clientDataOnly = false)
        {
            InitializeComponent();
            DataContext = this;
            if (clientDataOnly)
            {
                Pages.RemoveRange(1, Pages.Count - 1);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (var installedFonts = new InstalledFontCollection())
            {
                Fonts = installedFonts.Families.Select(x => x.Name);
            }
            ToolbarDisplayEnum = (IEnumerable<ToolbarDisplay>)Enum.GetValues(typeof(ToolbarDisplay));
            TreeMap = Pages.ToDictionary(x => (x as ISettingsPage).PageName, x => x);
            var categories = new Dictionary<string, TreeViewItem>();
            TreeViewItem first = null;
            foreach (var control in Pages)
            {
                control.DataContext = this;
                var page = control as ISettingsPage;
                if (categories.TryGetValue(page.Category, out var value))
                {
                    var newItem = new TreeViewItem() { Header = page.PageName, IsExpanded = true };
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
                            var newControl = new TreeViewItem { Header = step, IsExpanded = true };
                            parent.Items.Add(newControl);
                            parent = newControl;
                            categories.Add(string.Join(".", chain.Take(i + 1)), newControl);
                        }
                    }
                    var newItem = new TreeViewItem() { Header = page.PageName, IsExpanded = true };
                    parent.Items.Add(newItem);
                    if (first == null)
                    {
                        first = newItem;
                    }
                }
                TreeMap.Add($"{page.Category}.{page.PageName}", control);
            }
            first.IsSelected = true;
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

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
