using Autofac;
using LobotJR.Command.Controller.Gloat;
using LobotJR.Command.Controller.Player;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Systems.Gloat
{
    [TestClass]
    public class GloatSystemTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private PlayerController PlayerController;
        private GloatController GloatController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            GloatController = AutofacMockSetup.Container.Resolve<GloatController>();
        }

        [TestMethod]
        public void CanGloatReturnsTrueWithEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().FishingGloatCost;
            var canGloat = GloatController.CanGloatFishing(user);
            Assert.IsTrue(canGloat);
        }

        [TestMethod]
        public void CanGloatReturnsFalseWithoutEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().FishingGloatCost - 1;
            var canGloat = GloatController.CanGloatFishing(user);
            Assert.IsFalse(canGloat);
        }

        [TestMethod]
        public void GloatReturnsCorrectRecordAndRemovesCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var userId = user.TwitchId;
            var expectedFish = db.Catches.Read(x => x.UserId.Equals(userId)).OrderBy(x => x.FishId).First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = SettingsManager.GetGameSettings().FishingGloatCost;
            var gloat = GloatController.FishingGloat(user, 0);
            Assert.AreEqual(0, player.Currency);
            Assert.AreEqual(expectedFish.FishId, gloat.FishId);
        }

        [TestMethod]
        public void GloatReturnsNullWithNoRecords()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var userId = user.TwitchId;
            var records = db.Catches.Read(x => x.UserId.Equals(userId)).ToList();
            foreach (var record in records)
            {
                db.Catches.Delete(record);
            }
            db.Catches.Commit();
            var cost = SettingsManager.GetGameSettings().FishingGloatCost;
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = cost;
            var gloat = GloatController.FishingGloat(user, 0);
            Assert.AreEqual(cost, player.Currency);
            Assert.IsNull(gloat);
        }

        [TestMethod]
        public void GloatReturnsNullWithNegativeIndex()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var cost = SettingsManager.GetGameSettings().FishingGloatCost;
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = cost;
            var gloat = GloatController.FishingGloat(user, -1);
            Assert.AreEqual(cost, player.Currency);
            Assert.IsNull(gloat);
        }

        [TestMethod]
        public void GloatReturnsNullWithTooHighIndex()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var userId = user.TwitchId;
            var cost = SettingsManager.GetGameSettings().FishingGloatCost;
            var recordCount = db.Catches.Read(x => x.UserId.Equals(userId)).Count();
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = cost;
            var gloat = GloatController.FishingGloat(user, recordCount);
            Assert.AreEqual(cost, player.Currency);
            Assert.IsNull(gloat);
        }
    }
}
