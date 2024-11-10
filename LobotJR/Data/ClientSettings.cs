using System;

namespace LobotJR.Data
{
    public enum ToolbarDisplay
    {
        Icons,
        Text,
        Both
    }

    [Flags]
    public enum LogFilter
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
        Crash = 16
    }

    /// <summary>
    /// Settings that modify the UI of the app.
    /// </summary>
    public class ClientSettings : TableObject
    {
        /// <summary>
        /// The number of messages to keep in the log display.
        /// </summary>
        public int LogHistorySize { get; set; } = 1000;
        /// <summary>
        /// The name of the font family to use for the log display.
        /// </summary>
        public string FontFamily { get; set; } = "Consolas";
        /// <summary>
        /// The point size of the font to use for the log display.
        /// </summary>
        public int FontSize { get; set; } = 12;
        /// <summary>
        /// Bitmask enum that determines which levels of logs are shown.
        /// </summary>
        public LogFilter LogFilter { get; set; } = LogFilter.Info | LogFilter.Warning | LogFilter.Error | LogFilter.Crash;
        /// <summary>
        /// Whether to show only icons, names, or both for buttons on the
        /// toolbar.
        /// </summary>
        public ToolbarDisplay ToolbarDisplay { get; set; } = ToolbarDisplay.Icons;
        /// <summary>
        /// The RGB color value for the background of the log display.
        /// </summary>
        public int BackgroundColor { get; set; } = 0x000000;
        /// <summary>
        /// The RGB color value for the text of debug messages.
        /// </summary>
        public int DebugColor { get; set; } = 0x808080;
        /// <summary>
        /// The RGB color value for the text of info messages.
        /// </summary>
        public int InfoColor { get; set; } = 0xFFFFFF;
        /// <summary>
        /// The RGB color value for the text of warning messages.
        /// </summary>
        public int WarningColor { get; set; } = 0xFFFF00;
        /// <summary>
        /// The RGB color value for the text of error messages.
        /// </summary>
        public int ErrorColor { get; set; } = 0xFF0000;
        /// <summary>
        /// The RGB color value for the text of crash messages.
        /// </summary>
        public int CrashColor { get; set; } = 0xFF00FF;

        public void CopyFrom(ClientSettings other)
        {
            LogHistorySize = other.LogHistorySize;
            FontFamily = other.FontFamily;
            FontSize = other.FontSize;
            LogFilter = other.LogFilter;
            ToolbarDisplay = other.ToolbarDisplay;
            BackgroundColor = other.BackgroundColor;
            DebugColor = other.DebugColor;
            InfoColor = other.InfoColor;
            WarningColor = other.WarningColor;
            ErrorColor = other.ErrorColor;
            CrashColor = other.CrashColor;
        }
    }
}
