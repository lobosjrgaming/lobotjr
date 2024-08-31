using Autofac;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Test.Mocks;
using LobotJR.Trigger;
using LobotJR.Trigger.Responder;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Trigger
{
    [TestClass]
    public class TriggerManagerTests
    {
        private TriggerManager Manager;
        private PlayerController PlayerController;
        private UserController UserController;
        private BlockLinks Trigger;

        [TestInitialize]
        public void Initialize()
        {
            Manager = AutofacMockSetup.Container.Resolve<TriggerManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
            Trigger = AutofacMockSetup.Container.Resolve<BlockLinks>();
        }

        [TestMethod]
        public void TriggerManagerBlocksLinksForNewUsers()
        {
            var response = Manager.ProcessTrigger("butt.ass", new User("NewUser", "999"));
            Assert.IsTrue(response.Processed);
            Assert.IsTrue(response.TimeoutSender);
        }

        [TestMethod]
        public void TriggerManagerBlocksLinksForUsersUnderLevel2()
        {
            var user = new User("Level1", "1000");
            var player = PlayerController.GetPlayerByUser(user);
            player.Level = 1;
            var response = Manager.ProcessTrigger("butt.ass", user);
            Assert.IsTrue(response.Processed);
            Assert.IsTrue(response.TimeoutSender);
        }

        [TestMethod]
        public void TriggerManagerAllowsLinksForSubs()
        {
            var user = UserController.GetUserByName("Sub");
            var response = Manager.ProcessTrigger("butt.ass", user);
            Assert.IsFalse(response.Processed);
        }

        [TestMethod]
        public void TriggerManagerAllowsLinksForLevel2()
        {
            var user = new User("Level2", "2000");
            var player = PlayerController.GetPlayerByUser(user);
            player.Level = 2;
            var response = Manager.ProcessTrigger("butt.ass", user);
            Assert.IsFalse(response.Processed);
        }

        [TestMethod]
        public void TriggerManagerDoesNotSendMessagesOnRepeatTriggers()
        {
            var user = new User("Level1", "1000");
            var player = PlayerController.GetPlayerByUser(user);
            player.Level = 1;
            Trigger.LastTrigger = DateTime.Now - TimeSpan.FromMinutes(1);
            var response = Manager.ProcessTrigger("butt.ass", user);
            Assert.IsTrue(response.Processed);
            Assert.IsTrue(response.TimeoutSender);
            Assert.IsTrue(response.Messages.Any());
            response = Manager.ProcessTrigger("butt.ass", user);
            Assert.IsTrue(response.Processed);
            Assert.IsTrue(response.TimeoutSender);
            Assert.IsFalse(response.Messages.Any());
        }
    }
}
