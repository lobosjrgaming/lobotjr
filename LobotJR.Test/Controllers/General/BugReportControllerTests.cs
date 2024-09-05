using Autofac;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Controllers.General
{
    [TestClass]
    public class BugReportControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            AutofacMockSetup.ResetPlayers();
        }

        [TestMethod]
        public void SubmitsBugReports() { }

        [TestMethod]
        public void ResolvesBugReports() { }
    }
}
