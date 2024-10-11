using LobotJR.Interface.Settings;
using LobotJR.Shared.Utility;
using NLog;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LobotJR.Interface
{
    public enum ColorKeys
    {
        Background,
        Foreground,
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
            { ColorKeys.Foreground, new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) },
            { ColorKeys.Debug, new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)) },
            { ColorKeys.Warn, new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)) },
            { ColorKeys.Error, new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) },
            { ColorKeys.Crash, new SolidColorBrush(Color.FromArgb(255, 255, 0, 255)) },
        };
        private static readonly Dictionary<int, ColorKeys> LogColors = new Dictionary<int, ColorKeys>()
        {
            { LogLevel.Debug.Ordinal, ColorKeys.Debug },
            { LogLevel.Warn.Ordinal, ColorKeys.Warn },
            { LogLevel.Error.Ordinal, ColorKeys.Error },
            { LogLevel.Info.Ordinal, ColorKeys.Foreground },
        };

        private readonly List<LogEventInfo> LogHistory = new List<LogEventInfo>();
        private readonly int LogHistorySize = 1000;

        public Main()
        {
            InitializeComponent();
        }

        private void LogEvent(LogEventInfo info, object[] objects)
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
            while (LogOutput.Document.Blocks.Count > LogHistorySize)
            {
                LogOutput.Document.Blocks.Remove(LogOutput.Document.Blocks.FirstBlock);
            }
            LogHistory.Add(info);
            while (LogHistory.Count > LogHistorySize)
            {
                LogHistory.RemoveAt(0);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Lobot {version.Major}.{version.Minor}.{version.Build}";
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole(layout: "${time}|${level:uppercase=true}|${message:withexception=true}").WriteToMethodCall(LogEvent);
            });
            Logger.Info("Launching Lobot version {version}", Assembly.GetExecutingAssembly().GetName().Version);
            var clientData = FileUtils.ReadClientData();
            var tokenData = FileUtils.ReadTokenData();
            //This is outside of the try loop because if it fails, it will never succeed until the database state is corrected
            // await ConfigureDatabase(clientData, tokenData);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsEditor();
            dialog.ShowDialog();
        }
    }
}
