using Autofac;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.View.Fishing;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Modules.Fishing
{
    /// <summary>
    /// Summary description for FishingTests
    /// </summary>
    [TestClass]
    public class FishingAdminModuleTests
    {
        private IConnectionManager ConnectionManager;
        private TournamentController TournamentController;
        private FishingAdmin AdminView;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            TournamentController = AutofacMockSetup.Container.Resolve<TournamentController>();
            AdminView = AutofacMockSetup.Container.Resolve<FishingAdmin>();
        }

        [TestMethod]
        public void DebugTournamentStartsTournament()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var response = AdminView.DebugTournament();
                Assert.IsTrue(response.Processed);
                Assert.IsTrue(TournamentController.IsRunning);
            }
        }

        [TestMethod]
        public void DebugCatchCatchesManyFish()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var response = AdminView.DebugCatch();
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(50, response.Debug.Count);
                Assert.IsTrue(response.Debug.Any(x => db.FishData.Read().Any(y => x.Contains(y.Name))));
            }
        }
    }
}
