﻿using LobotJR.Trigger;
using LobotJR.Trigger.Responder;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Wolfcoins;

namespace LobotJR.Test.Trigger
{
    [TestClass]
    public class TriggerManagerTests
    {
        private TriggerManager Manager;
        private Currency Currency;

        [TestInitialize]
        public void Initialize()
        {
            Currency = new Currency();
            Currency.xpList = new Dictionary<string, int>
            {
                { "Level1", Currency.XPForLevel(1) },
                { "Level2", Currency.XPForLevel(2) }
            };
            Manager = new TriggerManager(new ITriggerResponder[]
            {
                new BlockLinks(Currency),
                new NoceanMan()
            });
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
            var response = Manager.ProcessTrigger("butt.ass", new User("Level1", ""));
            Assert.IsTrue(response.Processed);
            Assert.IsTrue(response.TimeoutSender);
        }

        [TestMethod]
        public void TriggerManagerAllowsLinksForSubs()
        {
            var response = Manager.ProcessTrigger("butt.ass", new User("Sub", "") { IsSub = true });
            Assert.IsNull(response);
        }

        [TestMethod]
        public void TriggerManagerAllowsLinksForLevel2()
        {
            var response = Manager.ProcessTrigger("butt.ass", new User("Level2", ""));
            Assert.IsNull(response);
        }
    }
}
