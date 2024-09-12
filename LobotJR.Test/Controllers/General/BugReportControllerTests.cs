using Autofac;
using LobotJR.Command.Controller.General;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Controllers.General
{
    [TestClass]
    public class BugReportControllerTests
    {
        private IConnectionManager ConnectionManager;
        private BugReportController Controller;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            Controller = AutofacMockSetup.Container.Resolve<BugReportController>();
            AutofacMockSetup.ResetPlayers();
            ConnectionManager.CurrentConnection.BugReports.Delete();
            ConnectionManager.CurrentConnection.Commit();
        }

        [TestMethod]
        public void SubmitsBugReports()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            Controller.SubmitReport(user, "Error Report");
            db.Commit();
            var bugs = db.BugReports.Read();
            Assert.AreEqual(1, bugs.Count());
            Assert.IsTrue(bugs.Any(x => x.Message.Equals("Error Report")));
        }

        [TestMethod]
        public void ResolvesBugReports()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            Controller.SubmitReport(user, "Error Report");
            db.Commit();
            var bug = db.BugReports.Read().First();
            Controller.ResolveReport(bug.Id, "Error Resolved");
            Assert.AreEqual("Error Resolved", bug.ResolutionMessage);
            Assert.IsNotNull(bug.ResolveTime);
        }
    }
}
