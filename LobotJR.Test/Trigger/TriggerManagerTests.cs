using Autofac;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Trigger;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Trigger
{
    [TestClass]
    public class TriggerManagerTests
    {
        private IConnectionManager ConnectionManager;
        private TriggerManager Manager;
        private PlayerController PlayerController;
        private UserController UserController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            Manager = AutofacMockSetup.Container.Resolve<TriggerManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
        }

        [TestMethod]
        public void TriggerManagerBlocksLinksForNewUsers()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var response = Manager.ProcessTrigger("butt.ass", new User("NewUser", "999"));
                Assert.IsTrue(response.Processed);
                Assert.IsTrue(response.TimeoutSender);
            }
        }

        [TestMethod]
        public void TriggerManagerBlocksLinksForUsersUnderLevel2()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = new User("Level1", "1000");
                var player = PlayerController.GetPlayerByUser(user);
                player.Level = 1;
                var response = Manager.ProcessTrigger("butt.ass", user);
                Assert.IsTrue(response.Processed);
                Assert.IsTrue(response.TimeoutSender);
            }
        }

        [TestMethod]
        public void TriggerManagerAllowsLinksForSubs()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Sub");
                var response = Manager.ProcessTrigger("butt.ass", user);
                Assert.IsNull(response);
            }
        }

        [TestMethod]
        public void TriggerManagerAllowsLinksForLevel2()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = new User("Level2", "2000");
                var player = PlayerController.GetPlayerByUser(user);
                player.Level = 2;
                var response = Manager.ProcessTrigger("butt.ass", user);
                Assert.IsNull(response);
            }
        }
    }
}
