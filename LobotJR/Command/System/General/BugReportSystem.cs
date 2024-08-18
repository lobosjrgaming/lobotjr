using LobotJR.Command.Model.General;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System;

namespace LobotJR.Command.System.General
{
    /// <summary>
    /// System that manages bug reports from users.
    /// </summary>
    public class BugReportSystem
    {
        private readonly IConnectionManager ConnectionManager;

        public BugReportSystem(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        /// <summary>
        /// Submits a new bug report.
        /// </summary>
        /// <param name="user">The user that submitted the report.</param>
        /// <param name="message">The content of the report.</param>
        public void SubmitReport(User user, string message)
        {
            ConnectionManager.CurrentConnection.BugReports.Create(new BugReport()
            {
                UserId = user.TwitchId,
                Message = message,
                ReportTime = DateTime.Now
            });
        }

        /// <summary>
        /// Marks a bug report as resolved and sets the resolution message.
        /// </summary>
        /// <param name="bugId">The id of the bug to resolve.</param>
        /// <param name="message">The resolution message to send to the user
        /// that reported the bug.</param>
        public void ResolveReport(int bugId, string message)
        {
            var bug = ConnectionManager.CurrentConnection.BugReports.ReadById(bugId);
            bug.ResolveTime = DateTime.Now;
            bug.ResolutionMessage = message;
        }
    }
}
