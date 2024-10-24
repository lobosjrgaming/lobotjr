using LobotJR.Command;
using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LobotJR.Interface
{
    /// <summary>
    /// Interaction logic for CommandExplorer.xaml
    /// </summary>
    public partial class CommandExplorer : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ICommandManager CommandManager;
        private readonly Dictionary<string, List<string>> Aliases = new Dictionary<string, List<string>>();
        private ItemsControl CachedView;

        public CommandExplorer(ICommandManager commandManager)
        {
            InitializeComponent();
            CommandManager = commandManager;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var categories = new Dictionary<string, TreeViewItem>();
            CachedView = new TreeView();
            var commands = CommandManager.Commands;
            foreach (var command in commands)
            {
                var parts = command.Split('.');
                ItemsControl parent = CachedView;
                var parentChain = new List<TreeViewItem>();
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    var step = string.Join(".", parts.Take(i + 1));
                    if (categories.TryGetValue(step, out var stepValue))
                    {
                        parent = stepValue;
                    }
                    else
                    {
                        var newControl = new TreeViewItem { Header = parts[i], IsExpanded = true };
                        parent.Items.Add(newControl);
                        parent = newControl;
                        categories.Add(string.Join(".", parts.Take(i + 1)), newControl);
                    }
                    parentChain.Add(parent as TreeViewItem);
                }
                var newItem = new TreeViewItem() { Header = parts.Last() };
                parent.Items.Add(newItem);
                var aliases = new List<string>(CommandManager.GetAliases(command)) { command };
                Aliases.Add(command, aliases);
                for (var i = 0; i < parentChain.Count; i++)
                {
                    var id = string.Join(".", parentChain.Take(i + 1));
                    if (Aliases.TryGetValue(id, out var aliasList))
                    {
                        aliasList.AddRange(aliases);
                    }
                    else
                    {
                        Aliases.Add(id, aliases);
                    }
                }
            }
        }

        private void FilterTree(string query)
        {
        }

        private void CategoryView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = CategoryView.SelectedItem as TreeViewItem;
            var chain = new List<TreeViewItem>() { item };
            for (var node = item.Parent as TreeViewItem; node != null; node = node.Parent as TreeViewItem)
            {
                chain.Insert(0, node);
            }
            var name = string.Join(".", chain.Select(x => x.Header));
            CommandManager.DescribeCommand(name);
            CommandManager.GetAliases(name);
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

        private bool Filter(object sender)
        {
            if (!string.IsNullOrWhiteSpace(SearchText.Text))
            {
                var item = sender as TreeViewItem;
                if (item != null)
                {
                    if (Aliases.TryGetValue(item.Header.ToString(), out var aliases))
                    {
                        Logger.Debug("Checking {filter} against {item} ({aliases})", SearchText.Text, item.Header, string.Join(", ", aliases));
                        return aliases.Any(x => x.Contains(SearchText.Text));
                    }
                }
                return false;
            }
            return true;
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            CategoryView.Items.Filter = Filter;
        }
    }
}
