using Autofac;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.View.Dungeons;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Views.Dungeons
{
    [TestClass]
    public class GroupFinderAdminTests
    {
        private GroupFinderAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            var db = AutofacMockSetup.ConnectionManager.CurrentConnection;
            View = AutofacMockSetup.Container.Resolve<GroupFinderAdmin>();
            var gfController = AutofacMockSetup.Container.Resolve<GroupFinderController>();
            var dungeonController = AutofacMockSetup.Container.Resolve<DungeonController>();
            var runs = dungeonController.GetAllDungeons();
            gfController.ResetQueue();
            var players = db.PlayerCharacters.Read().Take(3);
            gfController.QueuePlayer(players.ElementAt(0), runs.Take(1));
            gfController.QueuePlayer(players.ElementAt(1), runs.Take(2));
            gfController.QueuePlayer(players.ElementAt(2), runs.Skip(1).Take(1));
        }

        [TestMethod]
        public void QueueStatusGetsGroupFinderQueueDetails()
        {
            var response = View.QueueStatus();
            Assert.IsTrue(response.Responses.Any(x => x.Contains("3 players")));
            Assert.AreEqual(3, response.Responses.Count());
        }
    }
}
