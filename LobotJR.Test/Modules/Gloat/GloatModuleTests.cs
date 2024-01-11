using LobotJR.Command.Module.Gloat;
using LobotJR.Command.System.Gloat;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Wolfcoins;

namespace LobotJR.Test.Modules.Gloat
{
    /// <summary>
    /// Summary description for FishingTests
    /// </summary>
    [TestClass]
    public class FishingModuleTests
    {
        private SqliteRepositoryManager Manager;
        private Dictionary<string, int> Wolfcoins;
        private GloatSystem GloatSystem;
        private GloatModule GloatModule;

        [TestInitialize]
        public void Initialize()
        {
            Manager = new SqliteRepositoryManager(MockContext.Create());

            var currency = new Currency();
            Wolfcoins = currency.coinList;
            GloatSystem = new GloatSystem(Manager, currency);
            GloatModule = new GloatModule(GloatSystem);
        }

        [TestMethod]
        public void Gloats()
        {
            var user = Manager.Users.Read().First();
            var userId = user.TwitchId;
            Wolfcoins.Add(user.Username, GloatSystem.FishingGloatCost);
            var response = GloatModule.GloatFish(user, 1);
            var responses = response.Responses;
            var messages = response.Messages;
            var record = Manager.Catches.Read(x => x.UserId.Equals(userId)).OrderBy(x => x.FishId).First();
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.AreEqual(1, messages.Count);
            Assert.IsTrue(responses[0].Contains(GloatSystem.FishingGloatCost.ToString()));
            Assert.IsTrue(responses[0].Contains(record.Fish.Name));
            Assert.IsTrue(messages[0].Contains(user.Username));
            Assert.IsTrue(messages[0].Contains(record.Fish.Name));
            Assert.IsTrue(messages[0].Contains(record.Length.ToString()));
            Assert.IsTrue(messages[0].Contains(record.Weight.ToString()));
        }

        [TestMethod]
        public void GloatFailsWithInvalidFish()
        {
            var user = Manager.Users.Read().First();
            var userId = user.TwitchId;
            Wolfcoins.Add(user.Username, GloatSystem.FishingGloatCost);
            var response = GloatModule.GloatFish(user, 0);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("invalid", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void GloatFailsWithNoFish()
        {
            var user = Manager.Users.Read().First();
            var userId = user.TwitchId;
            Wolfcoins.Add(user.Username, GloatSystem.FishingGloatCost);
            DataUtils.ClearFisherRecords(Manager, user);
            var response = GloatModule.GloatFish(user, 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("!cast"));
        }

        [TestMethod]
        public void GloatFailsWithInsufficientCoins()
        {
            var user = Manager.Users.Read().First();
            var response = GloatModule.GloatFish(user, 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("coins"));
            Assert.IsFalse(responses[0].Contains("wolfcoins"));
        }
    }
}
