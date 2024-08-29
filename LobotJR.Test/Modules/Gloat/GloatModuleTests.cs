using Autofac;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.View.Gloat;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.Gloat
{
    /// <summary>
    /// Summary description for FishingTests
    /// </summary>
    [TestClass]
    public class FishingModuleTests
    {
        private PlayerController PlayerController;
        private GloatView GloatView;
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            GloatView = AutofacMockSetup.Container.Resolve<GloatView>();
        }

        [TestMethod]
        public void Gloats()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var userId = user.TwitchId;
                var settings = SettingsManager.GetGameSettings();
                PlayerController.GetPlayerByUser(user).Currency += settings.FishingGloatCost;
                var response = GloatView.GloatFish(user, 1);
                var responses = response.Responses;
                var messages = response.Messages;
                var record = db.Catches.Read(x => x.UserId.Equals(userId)).OrderBy(x => x.FishId).First();
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.AreEqual(1, messages.Count);
                Assert.IsTrue(responses[0].Contains(settings.FishingGloatCost.ToString()));
                Assert.IsTrue(responses[0].Contains(record.Fish.Name));
                Assert.IsTrue(messages[0].Contains(user.Username));
                Assert.IsTrue(messages[0].Contains(record.Fish.Name));
                Assert.IsTrue(messages[0].Contains(record.Length.ToString()));
                Assert.IsTrue(messages[0].Contains(record.Weight.ToString()));
            }
        }

        [TestMethod]
        public void GloatFailsWithInvalidFish()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                PlayerController.GetPlayerByUser(user).Currency += SettingsManager.GetGameSettings().FishingGloatCost;
                var response = GloatView.GloatFish(user, 0);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(0, response.Messages.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("invalid", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public void GloatFailsWithNoFish()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                PlayerController.GetPlayerByUser(user).Currency += SettingsManager.GetGameSettings().FishingGloatCost;
                DataUtils.ClearFisherRecords(db, user);
                var response = GloatView.GloatFish(user, 1);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(0, response.Messages.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("!cast"));
            }
        }

        [TestMethod]
        public void GloatFailsWithInsufficientCoins()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var response = GloatView.GloatFish(user, 1);
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
}
