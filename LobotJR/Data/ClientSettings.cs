namespace LobotJR.Data
{
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
            BackgroundColor = other.BackgroundColor;
            DebugColor = other.DebugColor;
            InfoColor = other.InfoColor;
            WarningColor = other.WarningColor;
            ErrorColor = other.ErrorColor;
            CrashColor = other.CrashColor;
        }
    }
}
