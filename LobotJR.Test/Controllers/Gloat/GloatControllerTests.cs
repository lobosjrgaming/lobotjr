using Autofac;
using LobotJR.Command.Controller.Gloat;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Controllers.Gloat
{
    [TestClass]
    public class GloatControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private PlayerController PlayerController;
        private PetController PetController;
        private GloatController GloatController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            PetController = AutofacMockSetup.Container.Resolve<PetController>();
            GloatController = AutofacMockSetup.Container.Resolve<GloatController>();
        }

        [TestMethod]
        public void CanGloatFishReturnsTrueWithEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().FishingGloatCost;
            var canGloat = GloatController.CanGloatFishing(user);
            Assert.IsTrue(canGloat);
        }

        [TestMethod]
        public void CanGloatLevelReturnsTrueWithEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().LevelGloatCost;
            var canGloat = GloatController.CanGloatLevel(user);
            Assert.IsTrue(canGloat);
        }

        [TestMethod]
        public void CanGloatPetReturnsTrueWithEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().PetGloatCost;
            var canGloat = GloatController.CanGloatPet(user);
            Assert.IsTrue(canGloat);
        }

        [TestMethod]
        public void CanGloatFishReturnsFalseWithoutEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().FishingGloatCost - 1;
            var canGloat = GloatController.CanGloatFishing(user);
            Assert.IsFalse(canGloat);
        }

        [TestMethod]
        public void CanGloatLevelReturnsFalseWithoutEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().LevelGloatCost - 1;
            var canGloat = GloatController.CanGloatLevel(user);
            Assert.IsFalse(canGloat);
        }

        [TestMethod]
        public void CanGloatPetReturnsFalseWithoutEnoughCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().PetGloatCost - 1;
            var canGloat = GloatController.CanGloatPet(user);
            Assert.IsFalse(canGloat);
        }

        [TestMethod]
        public void GloatFishReturnsCorrectRecordAndRemovesCoins()
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
        public void GloatLevelReturnsCorrectRecordAndRemovesCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = SettingsManager.GetGameSettings().LevelGloatCost;
            var gloat = GloatController.LevelGloat(user);
            Assert.AreEqual(0, player.Currency);
            Assert.AreEqual(player.UserId, gloat.UserId);
        }

        [TestMethod]
        public void GloatPetReturnsCorrectValueAndRemovesCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = SettingsManager.GetGameSettings().PetGloatCost;
            PetController.GrantPet(user, db.PetRarityData.Read().First());
            db.Commit();
            var record = PetController.GetStableForUser(user).First();
            var gloat = GloatController.PetGloat(user, record);
            Assert.AreEqual(0, player.Currency);
            Assert.IsTrue(gloat);
        }

        [TestMethod]
        public void GloatFishReturnsNullWithNoRecords()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            DataUtils.ClearFisherRecords(db, user);
            var cost = SettingsManager.GetGameSettings().FishingGloatCost;
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = cost;
            var gloat = GloatController.FishingGloat(user, 0);
            Assert.AreEqual(cost, player.Currency);
            Assert.IsNull(gloat);
        }

        [TestMethod]
        public void GloatFishReturnsNullWithNegativeIndex()
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
        public void GloatFishReturnsNullWithTooHighIndex()
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

        [TestMethod]
        public void GloatPetReturnsFalseWithNullStable()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var cost = SettingsManager.GetGameSettings().PetGloatCost;
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = SettingsManager.GetGameSettings().PetGloatCost;
            PetController.GrantPet(user, db.PetRarityData.Read().First());
            db.Commit();
            player.Currency = cost;
            var gloat = GloatController.PetGloat(user, null);
            Assert.AreEqual(cost, player.Currency);
            Assert.IsFalse(gloat);
        }
    }
}
