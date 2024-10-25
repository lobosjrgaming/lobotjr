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

        public CommandExplorer(ICommandManager commandManager)
        {
            InitializeComponent();
            CommandManager = commandManager;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var categories = new Dictionary<string, TreeViewItem>();
            var commands = CommandManager.Commands;
            foreach (var command in commands)
            {
                var parts = command.Split('.');
                ItemsControl parent = CategoryView;
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
                    var id = string.Join(".", parentChain.Take(i + 1).Select(x => x.Header));
                    if (Aliases.TryGetValue(id, out var aliasList))
                    {
                        aliasList.AddRange(aliases);
                    }
                    else
                    {
                        Aliases.Add(id, new List<string>(aliases));
                    }
                }
            }
        }

        private void CategoryView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = CategoryView.SelectedItem as TreeViewItem;
            if (!item.HasItems)
            {
                var chain = new List<TreeViewItem>() { item };
                for (var node = item.Parent as TreeViewItem; node != null; node = node.Parent as TreeViewItem)
                {
                    chain.Insert(0, node);
                }
                var name = string.Join(".", chain.Select(x => x.Header));
                var aliases = CommandManager.GetAliases(name);
                CommandSignature.Text = $"!{aliases.First()} {CommandManager.DescribeCommand(aliases.First())}";
                if (aliases.Count() == 1)
                {
                    AliasList.Text = "None";
                }
                else
                {
                    AliasList.Text = string.Join("\n", aliases.Skip(1).Select(x => $"!{x}"));
                }
            }
            else
            {
                CommandSignature.Text = string.Empty;
                AliasList.Text = string.Empty;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private bool Filter(TreeViewItem node, string parentName = "")
        {
            if (!string.IsNullOrWhiteSpace(SearchText.Text))
            {
                if (Aliases.TryGetValue($"{parentName}{node.Header}", out var aliases))
                {
                    var show = aliases.Any(x => x.Contains(SearchText.Text));
                    foreach (var child in node.Items)
                    {
                        var showChild = Filter(child as TreeViewItem, $"{parentName}{node.Header}.");
                        show = showChild || show;
                    }
                    node.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                    if (node.HasItems && show)
                    {
                        node.IsExpanded = true;
                    }
                    return show;
                }
                node.Visibility = Visibility.Collapsed;
                return false;
            }
            node.Visibility = Visibility.Visible;
            return true;
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var node in CategoryView.Items)
            {
                Filter(node as TreeViewItem);
            }
        }
    }
}
