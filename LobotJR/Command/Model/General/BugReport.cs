using LobotJR.Data;
using System;

namespace LobotJR.Command.Model.General
{
    /// <summary>
    /// Holds user-submitted bug reports.
    /// </summary>
    public class BugReport : TableObject
    {
        /// <summary>
        /// The Twitch id of the user that submitted the report.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The text of the user's bug report.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// The time the report was submitted
        /// </summary>
        public DateTime ReportTime { get; set; }
        /// <summary>
        /// The message added when the bug was marked as resolved. This is sent
        /// to the user that submitted the bug report.
        /// </summary>
        public string ResolutionMessage { get; set; }
        /// <summary>
        /// The time the report was resolved. If this value is not set, the
        /// report has not yet been resolved.
        /// </summary>
        public DateTime? ResolveTime { get; set; }
    }
}
