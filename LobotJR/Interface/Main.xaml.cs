using Autofac;
using LobotJR.Command;
using LobotJR.Data;
using LobotJR.Interface.Settings;
using LobotJR.Twitch;
using LobotJR.Twitch.Api.Authentication;
using LobotJR.Twitch.Api.Client;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
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
    public partial class Main : Window
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

        public Main()
        {
            InitializeComponent();
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
        }

        private async Task LaunchBot(ClientData clientData, TokenData tokenData)
        {
            try
            {
                await Bot.Initialize(clientData, tokenData);
                await LoadSettings(Bot.Scope);
                UpdateLogView();
                CancellationTokenSource = Bot.Start();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                Logger.Fatal("Application failed to start.");
            }
        }

        private void SendCommand(string command)
        {
            if (command[0] != '!')
            {
                command = $"!{command}";
            }
            var tokenData = FileUtils.ReadTokenData();
            Bot.ProcessCommand(command, tokenData.BroadcastUser, tokenData.BroadcastId);
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
            var clientData = FileUtils.ReadClientData();
            if (!clientData.RedirectUri.Equals(AuthCallback.RedirectUri))
            {
                var message = "The redirect URI in your saved client data is outdated. Please make sure your registered Twitch application has the new redirect URI (http://localhost:9000/) listed as one of its OAuth Redirect URLs.";
                Logger.Warn(message);
                MessageBox.Show(message, "Authentication Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                clientData.RedirectUri = AuthCallback.RedirectUri;
            }
            var tokenData = await AuthCallback.LoadTokens(clientData);
            if (tokenData.ChatToken == null)
            {
                var message = "Chat user token not found. Launching Twitch login.";
                Logger.Info(message);
                MessageBox.Show(message, "Chat Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
                tokenData.ChatToken = await AuthCallback.GetChatAuthCode(clientData);
            }
            if (tokenData.BroadcastToken == null)
            {
                var message = "Streamer user token not found. Launching Twitch login.";
                Logger.Info(message);
                MessageBox.Show(message, "Streamer Authentication", MessageBoxButton.OK, MessageBoxImage.Information);
                tokenData.BroadcastToken = await AuthCallback.GetBroadcastAuthCode(clientData);
            }
            await AuthCallback.ValidateAndRefresh(clientData, tokenData);
            await LaunchBot(clientData, tokenData);
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsEditor();
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
    }
}
