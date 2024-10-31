using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Data;
using LobotJR.Interface.Settings;
using LobotJR.Twitch;
using LobotJR.Twitch.Api.Authentication;
using LobotJR.Twitch.Api.Client;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LobotJR.Interface
{
    public enum ColorKeys
    {
        Background,
        Info,
        Debug,
        Warn,
        Error,
        Crash
    }

    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window, INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Dictionary<ColorKeys, Brush> Colors = new Dictionary<ColorKeys, Brush>()
        {
            { ColorKeys.Background, new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) },
            { ColorKeys.Info, new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) },
            { ColorKeys.Debug, new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)) },
            { ColorKeys.Warn, new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)) },
            { ColorKeys.Error, new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) },
            { ColorKeys.Crash, new SolidColorBrush(Color.FromArgb(255, 255, 0, 255)) },
        };
        private static readonly Dictionary<int, ColorKeys> LogColors = new Dictionary<int, ColorKeys>()
        {
            { LogLevel.Debug.Ordinal, ColorKeys.Debug },
            { LogLevel.Info.Ordinal, ColorKeys.Info },
            { LogLevel.Warn.Ordinal, ColorKeys.Warn },
            { LogLevel.Error.Ordinal, ColorKeys.Error },
            { LogLevel.Fatal.Ordinal, ColorKeys.Crash },
        };
        private readonly ClientSettings Settings = new ClientSettings();
        private readonly AuthCallback AuthCallback = new AuthCallback();
        private readonly GameSettings GameSettings = new GameSettings();
        private PlayerController PlayerController;

        private readonly List<LogEventInfo> LogHistory = new List<LogEventInfo>();
        private readonly List<string> CommandHistory = new List<string>();
        private int CommandIndex = 0;

        private readonly SemaphoreSlim LogSemaphore = new SemaphoreSlim(1, 1);
        private readonly Bot Bot = new Bot();

        private ClientData ClientData;
        private TokenData TokenData;

        public event PropertyChangedEventHandler PropertyChanged;

        public Visibility ShowIcons { get { return (Settings.ToolbarDisplay == ToolbarDisplay.Icons || Settings.ToolbarDisplay == ToolbarDisplay.Both) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowText { get { return (Settings.ToolbarDisplay == ToolbarDisplay.Text || Settings.ToolbarDisplay == ToolbarDisplay.Both) ? Visibility.Visible : Visibility.Collapsed; } }
        public bool HasClientData { get { return !string.IsNullOrWhiteSpace(ClientData?.ClientId) && !string.IsNullOrWhiteSpace(ClientData?.ClientSecret); } }
        public bool IsAuthenticated { get; set; }
        public bool IsStarted { get; set; }
        public bool IsConnected { get; set; }

        private async Task SetFlag(LogFilter level, bool value)
        {
            if (value)
            {
                Settings.LogFilter |= level;
            }
            else
            {
                Settings.LogFilter &= ~level;
            }
            var connectionManager = Bot.Scope.Resolve<IConnectionManager>();
            var settingsManager = Bot.Scope.Resolve<SettingsManager>();
            using (await connectionManager.OpenConnection())
            {
                settingsManager.GetClientSettings().LogFilter = Settings.LogFilter;
            }
            UpdateLogView();
        }

        public bool ShowDebug { get { return Settings.LogFilter.HasFlag(LogFilter.Debug); } set { SetFlag(LogFilter.Debug, value).Wait(); } }
        public bool ShowInfo { get { return Settings.LogFilter.HasFlag(LogFilter.Info); } set { SetFlag(LogFilter.Info, value).Wait(); } }
        public bool ShowWarning { get { return Settings.LogFilter.HasFlag(LogFilter.Warning); } set { SetFlag(LogFilter.Warning, value).Wait(); } }
        public bool ShowError { get { return Settings.LogFilter.HasFlag(LogFilter.Error); } set { SetFlag(LogFilter.Error, value).Wait(); } }
        public bool ShowCrash { get { return Settings.LogFilter.HasFlag(LogFilter.Crash); } set { SetFlag(LogFilter.Crash, value).Wait(); } }

        public Main()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void StartUpdateTimer()
        {
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (sender, e) =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (Bot.Scope != null)
                        {
                            var irc = Bot.Scope.Resolve<ITwitchIrcClient>();
                            if (irc != null)
                            {
                                MessageTimeStatus.Content = $"💬{irc.IdleTime:hh\\:mm\\:ss}";
                            }
                            if (PlayerController != null)
                            {
                                var timeToAward = TimeSpan.FromMinutes(GameSettings.ExperienceFrequency) - (DateTime.Now - PlayerController.LastAward);
                                if (!PlayerController.AwardsEnabled)
                                {
                                    timeToAward = TimeSpan.Zero;
                                }
                                AwardTimeStatus.Content = $"🌟{timeToAward:hh\\:mm\\:ss}";
                                var xpMessage = PlayerController.AwardsEnabled ? "on" : "off";
                                var multiMessage = PlayerController.CurrentMultiplier > 1 ? $" ({PlayerController.CurrentMultiplier}x)" : "";
                                AwardStatus.Content = $"XP is {xpMessage}{multiMessage}";
                            }
                        }
                    });
                }
                catch { }
            };
            timer.Start();
        }

        private void AddLog(LogEventInfo info)
        {
            var para = new Paragraph()
            {
                Background = Colors[ColorKeys.Background],
                Foreground = Colors[LogColors[info.Level.Ordinal]]
            };
            para.Inlines.Add(new Bold(new Run($"{info.TimeStamp:T}|{info.Level.Name}|")));
            para.Inlines.Add(new Run(info.FormattedMessage));
            LogOutput.Document.Blocks.Add(para);
            LogOutput.ScrollToEnd();
            while (LogOutput.Document.Blocks.Count > Settings.LogHistorySize)
            {
                LogOutput.Document.Blocks.Remove(LogOutput.Document.Blocks.FirstBlock);
            }
        }

        private bool ShouldShow(LogEventInfo info)
        {
            return Settings.LogFilter.HasFlag((LogFilter)(1 << (info.Level.Ordinal - 1)));
        }

        private void LogEvent(LogEventInfo info, object[] objects)
        {
            LogSemaphore.Wait();
            try
            {
                if (ShouldShow(info))
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddLog(info);
                        LogOutput.ScrollToEnd();
                    });
                }
                LogHistory.Add(info);
                while (LogHistory.Count > Settings.LogHistorySize)
                {
                    LogHistory.RemoveAt(0);
                }
            }
            finally
            {
                LogSemaphore.Release();
            }
        }

        private void UpdateLogView()
        {
            LogSemaphore.Wait();
            try
            {
                var toShow = LogHistory.Where(x => ShouldShow(x));
                Dispatcher.Invoke(() =>
                {
                    LogOutput.Document.Blocks.Clear();
                    foreach (var item in toShow)
                    {
                        AddLog(item);
                    }
                    LogOutput.ScrollToEnd();
                });
            }
            finally
            {
                LogSemaphore.Release();
            }
        }

        private async Task LoadSettings(ILifetimeScope scope)
        {
            var connectionManager = scope.Resolve<IConnectionManager>();
            using (var db = await connectionManager.OpenConnection())
            {
                var settingsManager = scope.Resolve<SettingsManager>();
                Settings.CopyFrom(settingsManager.GetClientSettings());
                GameSettings.CopyFrom(settingsManager.GetGameSettings());
            }
            Colors[ColorKeys.Background] = InterfaceUtils.BrushFromHex(Settings.BackgroundColor);
            Colors[ColorKeys.Debug] = InterfaceUtils.BrushFromHex(Settings.DebugColor);
            Colors[ColorKeys.Info] = InterfaceUtils.BrushFromHex(Settings.InfoColor);
            Colors[ColorKeys.Warn] = InterfaceUtils.BrushFromHex(Settings.WarningColor);
            Colors[ColorKeys.Error] = InterfaceUtils.BrushFromHex(Settings.ErrorColor);
            Colors[ColorKeys.Crash] = InterfaceUtils.BrushFromHex(Settings.CrashColor);
            LogOutput.FontFamily = new FontFamily(Settings.FontFamily);
            LogOutput.FontSize = Settings.FontSize;
            LogOutput.Background = Colors[ColorKeys.Background];
            CommandInputPanel.Background = Colors[ColorKeys.Background];
            CommandInputLabel.Foreground = Colors[ColorKeys.Info];
            CommandInput.Foreground = Colors[ColorKeys.Info];
            CommandInput.CaretBrush = Colors[ColorKeys.Info];
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(ShowText), nameof(ShowIcons), nameof(ShowDebug), nameof(ShowInfo), nameof(ShowWarning), nameof(ShowError), nameof(ShowCrash));
        }

        private async Task LaunchBot(ClientData clientData, TokenData tokenData)
        {
            try
            {
                BotStatus.Content = "Loading Bot Logic...";
                await Bot.PreLoad(clientData, tokenData);
                PlayerController = Bot.Scope.Resolve<PlayerController>();
                PlayerController.ExperienceToggled += PlayerController_ExperienceToggled;
                PlayerController.MultiplierModified += PlayerController_MultiplierModified;
                BotStatus.Content = "Loading Settings...";
                await LoadSettings(Bot.Scope);
                IsStarted = true;
                InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsStarted));
                UpdateLogView();
                BotStatus.Content = "Initializing Bot Runner...";
                await Bot.Initialize();
                BotStatus.Content = "Starting Bot Runner...";
                Bot.Start();
                IsConnected = true;
                InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsConnected));
                BotStatus.Content = "Ready";
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                Logger.Fatal("Application failed to start.");
            }
        }

        private void PlayerController_ExperienceToggled(bool enabled)
        {
        }

        private void PlayerController_MultiplierModified(int value)
        {
        }

        private void SendCommand(string command)
        {
            if (command[0] != '!')
            {
                command = $"!{command}";
            }
            Bot.ProcessCommand(command, TokenData.BroadcastUser, TokenData.BroadcastId);
        }

        private async Task Authenticate()
        {
            BotStatus.Content = "Authenticating...";
            if (!ClientData.RedirectUri.Equals(AuthCallback.RedirectUri))
            {
                var message = "The redirect URI saved in your client data is outdated. Please make sure your registered Twitch application has the new redirect URI (http://localhost:9000/) listed as one of its OAuth Redirect URLs.";
                Logger.Warn(message);
                MessageBox.Show(this, message, "Authentication Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClientData.RedirectUri = AuthCallback.RedirectUri;
                FileUtils.WriteClientData(ClientData);
                AuthCallback.ClearTokens();
            }
            TokenData = await AuthCallback.LoadTokens(ClientData);
            if (TokenData.ChatToken == null)
            {
                var message = "Chat user token not found. Launching Twitch login.";
                Logger.Info(message);
                MessageBox.Show(this, message, "Chat Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
                TokenData.ChatToken = await AuthCallback.GetChatAuthCode(ClientData);
            }
            if (TokenData.BroadcastToken == null)
            {
                var message = "Streamer user token not found. Launching Twitch login.";
                Logger.Info(message);
                MessageBox.Show(this, message, "Streamer Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
                TokenData.BroadcastToken = await AuthCallback.GetBroadcastAuthCode(ClientData);
            }
            if (await AuthCallback.ValidateAndRefresh(ClientData, TokenData))
            {
                IsAuthenticated = true;
                InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsAuthenticated));
                await LaunchBot(ClientData, TokenData);
            }
            else
            {
                var message = "Something went wrong authenticating. Please re-authenticate and try again.";
                Logger.Error(message);
                MessageBox.Show(this, message, "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BotStatus.Content = "Initializing...";
            LogOutput.Document.Blocks.Clear();
            StartUpdateTimer();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Lobot {version.Major}.{version.Minor}.{version.Build}";
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole(layout: "${time}|${level:uppercase=true}|${message:withexception=true}");
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToMethodCall(LogEvent);
            });
            Logger.Info("Launching Lobot version {major}.{minor}.{build}", version.Major, version.Minor, version.Build);
            BotStatus.Content = "Loading Client Data...";
            if (FileUtils.HasClientData())
            {
                ClientData = FileUtils.ReadClientData();
            }
            if (string.IsNullOrWhiteSpace(ClientData?.ClientId) || string.IsNullOrWhiteSpace(ClientData?.ClientSecret))
            {
                var dialog = new SettingsEditor(true)
                {
                    Owner = this,
                    Topmost = true,
                    ShowInTaskbar = false
                };
                ClientData = new ClientData() { RedirectUri = AuthCallback.RedirectUri };
                dialog.ClientData = ClientData;
                dialog.Left = Left + Width / 2 - dialog.Width / 2;
                dialog.Top = Top + Height / 2 - dialog.Height / 2;
                var result = dialog.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    ClientData.ClientId = dialog.ClientData.ClientId;
                    ClientData.ClientSecret = dialog.ClientData.ClientSecret;
                    FileUtils.WriteClientData(dialog.ClientData);
                }
            }
            if (string.IsNullOrWhiteSpace(ClientData.ClientId) || string.IsNullOrWhiteSpace(ClientData.ClientSecret))
            {
                MessageBox.Show(this, "Unable to launch without Twitch Client Data. The app will now close.", "Missing Client Data", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(HasClientData));
            await Authenticate();
        }

        private void ClearTooltip()
        {
            if (CommandInput.ToolTip is ToolTip tt)
            {
                tt.IsOpen = false;
            }
            CommandInput.ToolTip = null;
        }

        private void SetTooltip(string value)
        {
            if (!(CommandInput.ToolTip is ToolTip tt))
            {
                tt = new ToolTip()
                {
                    Placement = PlacementMode.Bottom,
                    PlacementTarget = CommandInput,
                };
                CommandInput.ToolTip = tt;
            }
            tt.IsOpen = true;
            tt.Content = value;
        }

        private void CommandInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                if (CommandInput.Text.Length > 0)
                {
                    SendCommand(CommandInput.Text);
                    CommandHistory.Insert(0, CommandInput.Text);
                    CommandInput.Text = string.Empty;
                    CommandIndex = 0;
                }
                ClearTooltip();
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                if (CommandIndex < CommandHistory.Count)
                {
                    CommandIndex = Math.Min(CommandIndex + 1, CommandHistory.Count);
                    CommandInput.Text = CommandHistory.ElementAt(CommandIndex - 1);
                    CommandInput.CaretIndex = CommandInput.Text.Length;
                }
                ClearTooltip();
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                if (CommandIndex >= 0)
                {
                    CommandIndex = Math.Max(CommandIndex - 1, 0);
                    if (CommandIndex == 0)
                    {
                        CommandInput.Text = string.Empty;
                    }
                    else
                    {
                        CommandInput.Text = CommandHistory.ElementAt(CommandIndex - 1);
                    }
                    CommandInput.CaretIndex = CommandInput.Text.Length;
                }
                ClearTooltip();
            }
            else if (e.Key == Key.Tab)
            {
                e.Handled = true;
                if (CommandInput.Text.Length > 0)
                {
                    var commandManager = Bot.Scope.Resolve<ICommandManager>();
                    var hasBang = CommandInput.Text[0] == '!';
                    var tabString = hasBang ? CommandInput.Text.Substring(1) : CommandInput.Text;
                    var commandString = commandManager.CommandStrings.FirstOrDefault(x => x.StartsWith(tabString));
                    if (commandString != null)
                    {
                        if (hasBang)
                        {
                            commandString = $"!{commandString}";
                        }
                        CommandInput.Text = commandString;
                        CommandInput.Focus();
                        CommandInput.CaretIndex = CommandInput.Text.Length;
                        ClearTooltip();
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                ClearTooltip();
            }
        }

        private void CommandInput_LostFocus(object sender, RoutedEventArgs e)
        {
            ClearTooltip();
        }

        private void CommandInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CommandInput.Text.Length > 0)
            {
                var commandManager = Bot.Scope.Resolve<ICommandManager>();
                var hasBang = CommandInput.Text[0] == '!';
                var commandName = hasBang ? CommandInput.Text.Substring(1) : CommandInput.Text;
                var spaceIndex = commandName.IndexOf(' ');
                commandName = spaceIndex >= 0 ? commandName.Substring(0, spaceIndex) : commandName;
                var possibleCommands = commandManager.CommandStrings.Where(x => spaceIndex >= 0 ? x.Equals(commandName) : x.StartsWith(commandName));
                if (possibleCommands.Any())
                {
                    if (possibleCommands.Count() == 1)
                    {
                        var command = possibleCommands.First();
                        SetTooltip($"{command} {commandManager.DescribeCommand(command)}");
                    }
                    else
                    {
                        if (possibleCommands.Count() > 10)
                        {
                            var final = $"and {possibleCommands.Count() - 9} others";
                            possibleCommands = possibleCommands.Take(9).Concat(new string[] { final });
                        }
                        SetTooltip(string.Join("\n", possibleCommands));
                    }
                }
                else
                {
                    SetTooltip("*Unknown Command*");
                }
            }
            else
            {
                ClearTooltip();
            }
        }

        private async void AuthenticateButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsAuthenticated)
            {
                var response = MessageBox.Show("You are already authenticated. Would you like to clear your credentials and re-authenticate to Twitch? (This will also restart the bot)", "Confirm Authentication", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (response == MessageBoxResult.Yes)
                {
                    await Bot.CancelAsync();
                    await Authenticate();
                }
            }
            else
            {
                await Authenticate();
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsStarted)
            {
                var response = MessageBox.Show("The bot is currently running, would you like to restart it? (This will cancel any active fishing tournaments and dungeon groups)", "Confirm Restart", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (response == MessageBoxResult.Yes)
                {
                    await Bot.CancelAsync();
                    await Authenticate();
                }
            }
            else
            {
                await Authenticate();
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
            {
                var response = MessageBox.Show("You are already connected to the Twitch IRC server. Would you like to disconnect and reconnect?", "Confirm Reconnect", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (response == MessageBoxResult.Yes)
                {
                    var irc = Bot.Scope.Resolve<ITwitchIrcClient>();
                    irc.ForceReconnect();
                }
            }
            else
            {
                if (Bot != null && Bot.Scope != null)
                {
                    var irc = Bot.Scope.Resolve<ITwitchIrcClient>();
                    if (irc != null)
                    {
                        irc.ForceReconnect();
                    }
                    else
                    {
                        MessageBox.Show("Unable to resolve IRC client. Please restart the application.", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Bot is not started, cannot create IRC connection", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CommandButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommandExplorer(Bot.Scope.Resolve<ICommandManager>())
            {
                Owner = this,
                Topmost = true,
                ShowInTaskbar = false
            };
            dialog.Left = Left + Width / 2 - dialog.Width / 2;
            dialog.Top = Top + Height / 2 - dialog.Height / 2;
            dialog.ShowDialog();
        }

        private void ContentButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlayerButton_Click(object sender, RoutedEventArgs e)
        {
            var connectionManager = Bot.Scope.Resolve<IConnectionManager>();
            var dialog = new PlayerEditor(connectionManager, Bot.Scope.Resolve<UserController>(), Bot.Scope.Resolve<PlayerController>(), Bot.Scope.Resolve<EquipmentController>(), Bot.Scope.Resolve<PetController>())
            {
                Owner = this,
                Topmost = true,
                ShowInTaskbar = false
            };
            dialog.Left = Left + Width / 2 - dialog.Width / 2;
            dialog.Top = Top + Height / 2 - dialog.Height / 2;
            var result = dialog.ShowDialog();
        }

        private async void AccessButton_Click(object sender, RoutedEventArgs e)
        {
            var connectionManager = Bot.Scope.Resolve<IConnectionManager>();
            var dialog = new AccessControlEditor(connectionManager, Bot.Scope.Resolve<ICommandManager>(), Bot.Scope.Resolve<UserController>())
            {
                Owner = this,
                Topmost = true,
                ShowInTaskbar = false
            };
            dialog.Left = Left + Width / 2 - dialog.Width / 2;
            dialog.Top = Top + Height / 2 - dialog.Height / 2;
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                using (var db = await connectionManager.OpenConnection())
                {
                    var groups = dialog.AccessGroups;
                    var groupNames = groups.Select(x => x.Name).ToList();
                    var allGroups = db.AccessGroups.ReadWith(x => x.Enrollments, x => x.Restrictions).ToList();
                    var groupsToDelete = allGroups.Where(x => !groupNames.Contains(x.Name));
                    foreach (var group in groups)
                    {
                        if (group.Id > 0)
                        {
                            var dbGroup = db.AccessGroups.ReadWith(x => x.Enrollments, x => x.Restrictions).FirstOrDefault(x => x.Id == group.Id);
                            dbGroup.IncludeAdmins = group.IncludeAdmins;
                            dbGroup.IncludeMods = group.IncludeMods;
                            dbGroup.IncludeVips = group.IncludeVips;
                            dbGroup.IncludeSubs = group.IncludeSubs;
                            dbGroup.Name = group.Name;
                            var enrollmentNames = group.Enrollments.Select(x => x.UserId);
                            var dbEnrollmentNames = dbGroup.Enrollments.Select(x => x.UserId);
                            var enrollmentsToDelete = dbGroup.Enrollments.Where(x => !enrollmentNames.Contains(x.UserId)).ToList();
                            var enrollmentsToAdd = group.Enrollments.Where(x => !dbEnrollmentNames.Contains(x.UserId));
                            foreach (var toDelete in enrollmentsToDelete)
                            {
                                db.Enrollments.Delete(toDelete);
                            }
                            foreach (var toAdd in enrollmentsToAdd)
                            {
                                db.Enrollments.Create(new Enrollment(dbGroup, toAdd.UserId));
                            }
                            var restrictionNames = group.Restrictions.Select(x => x.Command);
                            var dbRestrictionNames = dbGroup.Restrictions.Select(x => x.Command);
                            var restrictionsToDelete = dbGroup.Restrictions.Where(x => !restrictionNames.Contains(x.Command)).ToList();
                            var restrictionsToAdd = group.Restrictions.Where(x => !dbRestrictionNames.Contains(x.Command));
                            foreach (var toDelete in restrictionsToDelete)
                            {
                                db.Restrictions.Delete(toDelete);
                            }
                            foreach (var toAdd in restrictionsToAdd)
                            {
                                db.Restrictions.Create(new Restriction(dbGroup, toAdd.Command));
                            }
                        }
                        else
                        {
                            db.AccessGroups.Create(group);
                        }
                    }
                    foreach (var group in groupsToDelete)
                    {
                        db.Enrollments.DeleteRange(db.Enrollments.Read(x => x.GroupId == group.Id));
                        db.Restrictions.DeleteRange(db.Restrictions.Read(x => x.GroupId == group.Id));
                        db.AccessGroups.Delete(group);
                    }
                }
            }
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsEditor()
            {
                Owner = this,
                Topmost = true,
                ShowInTaskbar = false
            };
            var connectionManager = Bot.Scope.Resolve<IConnectionManager>();
            using (var db = await connectionManager.OpenConnection())
            {
                var manager = Bot.Scope.Resolve<SettingsManager>();
                dialog.GameSettings.CopyFrom(manager.GetGameSettings());
                dialog.AppSettings.CopyFrom(manager.GetAppSettings());
                dialog.ClientSettings.CopyFrom(manager.GetClientSettings());
                dialog.ClientData = FileUtils.ReadClientData();
            }
            dialog.Left = Left + Width / 2 - dialog.Width / 2;
            dialog.Top = Top + Height / 2 - dialog.Height / 2;
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                using (var db = await connectionManager.OpenConnection())
                {
                    var manager = Bot.Scope.Resolve<SettingsManager>();
                    manager.GetGameSettings().CopyFrom(dialog.GameSettings);
                    manager.GetAppSettings().CopyFrom(dialog.AppSettings);
                    manager.GetClientSettings().CopyFrom(dialog.ClientSettings);
                    Settings.CopyFrom(dialog.ClientSettings);
                    FileUtils.WriteClientData(dialog.ClientData);
                }
                await LoadSettings(Bot.Scope);
                UpdateLogView();
            }
        }
    }
}
