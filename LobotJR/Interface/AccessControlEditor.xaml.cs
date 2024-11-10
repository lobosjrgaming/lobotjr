using LobotJR.Command;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LobotJR.Interface
{
    /// <summary>
    /// Interaction logic for AccessControlEditor.xaml
    /// </summary>
    public partial class AccessControlEditor : Window, INotifyPropertyChanged
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly ICommandManager CommandManager;
        private readonly UserController UserController;
        private readonly Dictionary<string, string> UserCache = new Dictionary<string, string>();
        private readonly Dictionary<string, string> ReverseUserCache = new Dictionary<string, string>();

        public event PropertyChangedEventHandler PropertyChanged;

        public AccessGroup CurrentGroup { get; private set; }
        public bool IsGroupSelected { get; private set; }
        public bool IsUserSelected { get; private set; }
        public bool IsCommandSelected { get; private set; }
        public List<AccessGroup> AccessGroups { get; private set; } = new List<AccessGroup>();

        public AccessControlEditor(IConnectionManager connectionManager, ICommandManager commandManager, UserController userController)
        {
            InitializeComponent();
            ConnectionManager = connectionManager;
            CommandManager = commandManager;
            UserController = userController;
            DataContext = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (var context = await ConnectionManager.OpenConnection())
            {
                AccessGroups.AddRange(context.AccessGroups.ReadWith(x => x.Restrictions, x => x.Enrollments).ToList());
                foreach (var group in AccessGroups)
                {
                    foreach (var enrollment in group.Enrollments)
                    {
                        var username = context.Users.Read(x => x.TwitchId.Equals(enrollment.UserId)).FirstOrDefault()?.Username;
                        if (username != null)
                        {
                            if (!UserCache.ContainsKey(enrollment.UserId))
                            {
                                UserCache.Add(enrollment.UserId, username);
                            }
                            if (!ReverseUserCache.ContainsKey(username))
                            {
                                ReverseUserCache.Add(username, enrollment.UserId);
                            }
                        }
                    }
                }
            }
            foreach (var group in AccessGroups)
            {
                AccessGroupList.Items.Add(group.Name);
            }
        }

        private void AddUserToList(Enrollment enrollment)
        {
            UserList.Items.Add(new ListViewItem() { Content = UserCache[enrollment.UserId] });
        }

        private void AddCommandToList(Restriction restriction)
        {
            var item = new ListViewItem() { Content = restriction.Command };
            if (restriction.Command.Contains('*'))
            {
                var matches = CommandManager.GetMatchingCommands(restriction.Command);
                item.ToolTip = $"This pattern matches {matches.Count()} commands:\n{string.Join("\n", matches)}";
            }
            CommandList.Items.Add(item);
        }

        private void AccessGroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var group = AccessGroups.FirstOrDefault(x => x.Name.Equals(AccessGroupList.SelectedItem?.ToString()));
            if (group != null)
            {
                UserList.Items.Clear();
                CommandList.Items.Clear();
                foreach (var enrollment in group.Enrollments)
                {
                    AddUserToList(enrollment);
                }
                foreach (var restriction in group.Restrictions)
                {
                    AddCommandToList(restriction);
                }
                CurrentGroup = group;
                IsGroupSelected = true;
            }
            else
            {
                UserList.Items.Clear();
                CommandList.Items.Clear();
                CurrentGroup = null;
                IsGroupSelected = false;
                UserList.SelectedItem = null;
                CommandList.SelectedItem = null;
            }
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(CurrentGroup), nameof(IsGroupSelected));
        }

        private void UserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsUserSelected = UserList.SelectedItem != null;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsUserSelected));
        }

        private void CommandList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsCommandSelected = CommandList.SelectedItem != null;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsCommandSelected));
        }

        private void ClearTooltip()
        {
            if (CommandToAdd.ToolTip is ToolTip tt)
            {
                tt.IsOpen = false;
            }
            CommandToAdd.ToolTip = null;
        }

        private void SetTooltip(string value)
        {
            if (!(CommandToAdd.ToolTip is ToolTip tt))
            {
                tt = new ToolTip()
                {
                    Placement = PlacementMode.Bottom,
                    PlacementTarget = CommandToAdd,
                };
                CommandToAdd.ToolTip = tt;
            }
            tt.IsOpen = true;
            tt.Content = value;
        }

        private void CommandToAdd_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                AddCommand_Click(this, new RoutedEventArgs());
                CommandToAdd.Text = string.Empty;
                ClearTooltip();
            }
            else if (e.Key == Key.Tab)
            {
                if (CommandToAdd.Text != string.Empty)
                {
                    var commandName = CommandToAdd.Text.Replace(".", "\\.").Replace("*", ".*");
                    var pattern = new Regex($"^{commandName}(.*)$");
                    var possibleCommands = CommandManager.Commands.Where(x => pattern.IsMatch(x));
                    if (possibleCommands.Any())
                    {
                        var match = possibleCommands.First();
                        var patternMatch = pattern.Match(match);
                        CommandToAdd.Text = $"{CommandToAdd.Text}{patternMatch.Groups[1]}";
                        CommandToAdd.CaretIndex = CommandToAdd.Text.Length;
                        ClearTooltip();
                    }
                    e.Handled = true;
                }
            }
        }

        private void CommandToAdd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CommandToAdd.Text.Length > 0)
            {
                var commandName = CommandToAdd.Text.Replace(".", "\\.").Replace("*", ".*");
                var pattern = new Regex($"^{commandName}.*$");
                var possibleCommands = CommandManager.Commands.Where(x => pattern.IsMatch(x));
                if (possibleCommands.Any())
                {
                    if (possibleCommands.Count() > 10)
                    {
                        var final = $"and {possibleCommands.Count() - 9} others";
                        possibleCommands = possibleCommands.Take(9).Concat(new string[] { final });
                    }
                    SetTooltip(string.Join("\n", possibleCommands));
                }
                else
                {
                    SetTooltip("No matching commands");
                }
            }
            else
            {
                ClearTooltip();
            }
        }

        private void AddCommand_Click(object sender, RoutedEventArgs e)
        {
            var command = CommandToAdd.Text;
            if (!string.IsNullOrWhiteSpace(command))
            {
                if (CommandManager.IsValidCommand(command))
                {
                    if (CurrentGroup.Restrictions.Any(x => x.Command.Equals(command)))
                    {
                        MessageBox.Show($"Access group {CurrentGroup.Name} already contains command {command}", "Invalid Command", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        var restriction = new Restriction(CurrentGroup, command);
                        AddCommandToList(restriction);
                        CurrentGroup.Restrictions.Add(restriction);
                    }
                }
                else
                {
                    var message = $"{command} is not a valid command.";
                    if (!string.IsNullOrWhiteSpace(command) && command.Contains('*'))
                    {
                        message = $"{command} does not match any commands.";
                    }
                    MessageBox.Show(message, "Invalid Command", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                CommandToAdd.Focus();
            }
        }

        private void DeleteCommand_Click(object sender, RoutedEventArgs e)
        {
            var command = (CommandList.SelectedItem as ListViewItem).Content?.ToString();
            if (!string.IsNullOrWhiteSpace(command))
            {
                var toRemove = CurrentGroup.Restrictions.FirstOrDefault(x => x.Command.Equals(command));
                if (toRemove != null)
                {
                    CurrentGroup.Restrictions.Remove(toRemove);
                    CommandList.Items.Remove(CommandList.SelectedItem);
                    CommandList.SelectedIndex = -1;
                }
            }
        }

        private void UserToAdd_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                AddUser_Click(this, new RoutedEventArgs());
            }
        }

        private void ProcessAddUser(User user)
        {
            if (CurrentGroup.Enrollments.Any(x => x.UserId.Equals(user.TwitchId)))
            {
                MessageBox.Show($"Access group {CurrentGroup.Name} already contains user {user.Username}", "Invalid User", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (!UserCache.ContainsKey(user.TwitchId))
                {
                    UserCache.Add(user.TwitchId, user.Username);
                    ReverseUserCache.Add(user.Username, user.TwitchId);
                }
                var enrollment = new Enrollment(CurrentGroup, user.TwitchId);
                AddUserToList(enrollment);
                CurrentGroup.Enrollments.Add(enrollment);
                UserToAdd.Text = string.Empty;
            }
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var username = UserToAdd.Text;
            if (!string.IsNullOrWhiteSpace(username))
            {
                User user = null;
                using (var db = await ConnectionManager.OpenConnection())
                {
                    user = UserController.GetUserByName(username);
                }
                if (user != null)
                {
                    ProcessAddUser(user);
                }
                else
                {
                    var result = MessageBox.Show($"Unable to find user data for {username}. Would you like to retrieve data for this user from Twitch?", "User Not Found", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        using (await ConnectionManager.OpenConnection())
                        {
                            UserController.GetUserByNameAsync(username, newUser =>
                            {
                                if (newUser != null)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        ProcessAddUser(newUser);
                                    });
                                }
                            });
                        }
                    }
                }
            }
            else
            {
                UserToAdd.Focus();
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var username = (UserList.SelectedItem as ListViewItem).Content?.ToString();
            if (!string.IsNullOrWhiteSpace(username))
            {
                if (ReverseUserCache.TryGetValue(username, out var userId))
                {
                    var toRemove = CurrentGroup.Enrollments.FirstOrDefault(x => x.UserId.Equals(userId));
                    if (toRemove != null)
                    {
                        CurrentGroup.Enrollments.Remove(toRemove);
                        UserList.Items.Remove(UserList.SelectedItem);
                        UserList.SelectedIndex = -1;
                    }
                }
            }
        }

        private void GroupToAdd_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                AddAccessGroup_Click(this, new RoutedEventArgs());
                GroupToAdd.Text = string.Empty;
            }
        }

        private void AddAccessGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupName = GroupToAdd.Text;
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                if (AccessGroups.Any(x => x.Name.Equals(groupName)))
                {
                    MessageBox.Show($"Access group with name {groupName} already exists.", "Invalid Group Name", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var newGroup = new AccessGroup() { Name = groupName, Enrollments = new List<Enrollment>(), Restrictions = new List<Restriction>() };
                    AccessGroups.Add(newGroup);
                    int index = AccessGroupList.Items.Add(newGroup.Name);
                    AccessGroupList.SelectedIndex = index;
                }
            }
            else
            {
                GroupToAdd.Focus();
            }
        }

        private void DeleteAccessGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupName = AccessGroupList.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                var toRemove = AccessGroups.FirstOrDefault(x => x.Name.Equals(groupName));
                if (toRemove != null)
                {
                    AccessGroups.Remove(toRemove);
                    AccessGroupList.Items.Remove(AccessGroupList.SelectedItem);
                    AccessGroupList.SelectedIndex = -1;
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
