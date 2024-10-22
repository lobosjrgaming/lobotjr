using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Player;
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

        private readonly List<LogEventInfo> LogHistory = new List<LogEventInfo>();
        private readonly List<string> CommandHistory = new List<string>();
        private int CommandIndex = 0;

        private readonly SemaphoreSlim LogSemaphore = new SemaphoreSlim(1, 1);
        private readonly Bot Bot = new Bot();
        private CancellationTokenSource CancellationTokenSource;

        private ClientData ClientData;
        private TokenData TokenData;

        public event PropertyChangedEventHandler PropertyChanged;

        public Visibility ShowIcons { get { return (Settings.ToolbarDisplay == ToolbarDisplay.Icons || Settings.ToolbarDisplay == ToolbarDisplay.Both) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowText { get { return (Settings.ToolbarDisplay == ToolbarDisplay.Text || Settings.ToolbarDisplay == ToolbarDisplay.Both) ? Visibility.Visible : Visibility.Collapsed; } }
        public bool HasClientData { get { return !string.IsNullOrWhiteSpace(ClientData?.ClientId) && !string.IsNullOrWhiteSpace(ClientData?.ClientSecret); } }
        public bool IsAuthenticated { get; set; }
        public bool IsConnected { get; set; }
        public bool IsStarted { get; set; }
        public Visibility AuthenticateEnabledVisibility { get { return HasClientData ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility AuthenticateDisabledVisibility { get { return !HasClientData ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ConnectEnabledVisibility { get { return IsAuthenticated ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ConnectDisabledVisibility { get { return !IsAuthenticated ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ActivateEnabledVisibility { get { return IsConnected ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ActivateDisabledVisibility { get { return !IsConnected ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility BotActionEnabledVisibility { get { return IsStarted ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility BotActionDisabledVisibility { get { return !IsStarted ? Visibility.Visible : Visibility.Collapsed; } }
        public bool AreAwardsEnabled { get; set; }
        public int AwardMultiplier { get; set; } = 1;

        public Main()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void FireChangeEvent(params string[] names)
        {
            foreach (string name in names)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
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
            }
            Colors[ColorKeys.Background] = InterfaceUtils.BrushFromHex(Settings.BackgroundColor);
            Colors[ColorKeys.Debug] = InterfaceUtils.BrushFromHex(Settings.BackgroundColor);
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
            FireChangeEvent("ShowText", "ShowIcons");
        }

        private async Task LaunchBot(ClientData clientData, TokenData tokenData)
        {
            try
            {
                await Bot.PreLoad(clientData, tokenData);
                var playerController = Bot.Scope.Resolve<PlayerController>();
                playerController.ExperienceToggled += PlayerController_ExperienceToggled;
                playerController.MultiplierModified += PlayerController_MultiplierModified;
                await LoadSettings(Bot.Scope);
                UpdateLogView();
                await Bot.Initialize();
                CancellationTokenSource = Bot.Start();
                IsConnected = true;
                IsStarted = true;
                FireChangeEvent("IsConnected", "IsStarted", "ActivateEnabledVisibility", "ActivateDisabledVisibility", "BotActionEnabledVisibility", "BotActionDisabledVisibility");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                Logger.Fatal("Application failed to start.");
            }
        }

        private void PlayerController_ExperienceToggled(bool enabled)
        {
            AreAwardsEnabled = enabled;
            FireChangeEvent("AreAwardsEnabled");
        }

        private void PlayerController_MultiplierModified(int value)
        {
            AwardMultiplier = value;
            FireChangeEvent("AwardMultiplier");
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
                FireChangeEvent("IsAuthenticated", "ConnectEnabledVisibility", "ConnectDisabledVisibility");
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
            LogOutput.Document.Blocks.Clear();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Lobot {version.Major}.{version.Minor}.{version.Build}";
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole(layout: "${time}|${level:uppercase=true}|${message:withexception=true}");
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToMethodCall(LogEvent);
            });
            Logger.Info("Launching Lobot version {major}.{minor}.{build}", version.Major, version.Minor, version.Build);
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
            FireChangeEvent("HasClientData", "AuthenticateEnabledVisibility", "AuthenticateDisabledVisibility");
            await Authenticate();
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
                    }
                }
            }
        }

        private void AuthenticateButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
