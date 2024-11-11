using Autofac;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.View.Gloat;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Views.Gloat
{
    /// <summary>
    /// Summary description for FishingTests
    /// </summary>
    [TestClass]
    public class GloatViewTests
    {
        private PlayerController PlayerController;
        private LeaderboardController LeaderboardController;
        private PetController PetController;
        private GloatView GloatView;
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            LeaderboardController = AutofacMockSetup.Container.Resolve<LeaderboardController>();
            PetController = AutofacMockSetup.Container.Resolve<PetController>();
            GloatView = AutofacMockSetup.Container.Resolve<GloatView>();
            AutofacMockSetup.ResetFishingRecords();
            AutofacMockSetup.ResetPlayers();
        }

        [TestMethod]
        public void GloatsAboutFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var userid = db.Catches.Read().First().UserId;
            var user = db.Users.Read(x => x.TwitchId.Equals(userid)).First();
            var settings = SettingsManager.GetGameSettings();
            PlayerController.GetPlayerByUser(user).Currency = settings.FishingGloatCost;
            var response = GloatView.GloatFish(user, 1);
            var responses = response.Responses;
            var messages = response.Messages;
            var record = LeaderboardController.GetPersonalLeaderboard(user).First();
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

        [TestMethod]
        public void GloatsAboutLevel()
        {
            var db = ConnectionManager.CurrentConnection;
            var userid = db.Catches.Read().First().UserId;
            var user = db.Users.Read(x => x.TwitchId.Equals(userid)).First();
            var player = PlayerController.GetPlayerByUser(user);
            var settings = SettingsManager.GetGameSettings();
            player.Currency = settings.LevelGloatCost;
            player.Level = 20;
            player.Prestige = 5;
            var response = GloatView.GloatLevel(user);
            var responses = response.Responses;
            var messages = response.Messages;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, responses.Count);
            Assert.AreEqual(1, messages.Count);
            Assert.IsTrue(messages[0].Contains(user.Username));
            Assert.IsTrue(messages[0].Contains(settings.LevelGloatCost.ToString()));
            Assert.IsTrue(messages[0].Contains(player.Level.ToString()));
            Assert.IsTrue(messages[0].Contains(player.Prestige.ToString()));
            Assert.IsTrue(messages[0].Contains("Wolfpack God"));
        }

        [TestMethod]
        public void GloatsAboutActivePet()
        {
            var db = ConnectionManager.CurrentConnection;
            var userid = db.Catches.Read().First().UserId;
            var user = db.Users.Read(x => x.TwitchId.Equals(userid)).First();
            var settings = SettingsManager.GetGameSettings();
            var player = PlayerController.GetPlayerByUser(user).Currency = settings.PetGloatCost;
            PetController.GrantPet(user, db.PetRarityData.Read().First());
            db.Commit();
            var stables = PetController.GetStableForUser(user);
            stables.First().IsActive = true;
            stables.First().Name = "TestName";
            db.Commit();
            var record = PetController.GetActivePet(user);
            var response = GloatView.GloatPet(user);
            var responses = response.Responses;
            var messages = response.Messages;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.AreEqual(1, messages.Count);
            Assert.IsTrue(responses[0].Contains(settings.PetGloatCost.ToString()));
            Assert.IsTrue(responses[0].Contains(record.Name));
            Assert.IsTrue(messages[0].Contains(user.Username));
            Assert.IsTrue(messages[0].Contains(record.Level.ToString()));
            Assert.IsTrue(messages[0].Contains(record.Name));
            Assert.IsTrue(messages[0].Contains(record.Pet.Name));
        }

        [TestMethod]
        public void GloatsAboutSpecificPet()
        {
            var db = ConnectionManager.CurrentConnection;
            var userid = db.Catches.Read().First().UserId;
            var user = db.Users.Read(x => x.TwitchId.Equals(userid)).First();
            var settings = SettingsManager.GetGameSettings();
            var player = PlayerController.GetPlayerByUser(user).Currency = settings.PetGloatCost;
            PetController.GrantPet(user, db.PetRarityData.Read().First());
            db.Commit();
            var response = GloatView.GloatPet(user, 1);
            var responses = response.Responses;
            var messages = response.Messages;
            var record = PetController.GetStableForUser(user).First();
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.AreEqual(1, messages.Count);
            Assert.IsTrue(responses[0].Contains(settings.PetGloatCost.ToString()));
            Assert.IsTrue(responses[0].Contains(record.Name));
            Assert.IsTrue(messages[0].Contains(user.Username));
            Assert.IsTrue(messages[0].Contains(record.Level.ToString()));
            Assert.IsTrue(messages[0].Contains(record.Name));
            Assert.IsTrue(messages[0].Contains(record.Pet.Name));
        }

        [TestMethod]
        public void GloatFishFailsWithZeroIndex()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().FishingGloatCost;
            var response = GloatView.GloatFish(user, 0);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("invalid", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void GloatPetFailsWithZeroIndexAndNoActivePet()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().PetGloatCost;
            PetController.GrantPet(user, db.PetRarityData.Read().First());
            db.Commit();
            var active = PetController.GetActivePet(user);
            if (active != null)
            {
                active.IsActive = false;
            }
            var response = GloatView.GloatPet(user, 0);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("!summon", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void GloatFishFailsWithInvalidIndex()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().FishingGloatCost;
            var records = LeaderboardController.GetPersonalLeaderboard(user);
            var response = GloatView.GloatFish(user, records.Count() + 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("invalid", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void GloatPetFailsWithInvalidIndex()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().PetGloatCost;
            PetController.GrantPet(user, db.PetRarityData.Read().First());
            db.Commit();
            var records = PetController.GetStableForUser(user);
            var response = GloatView.GloatPet(user, records.Count() + 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("invalid", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void GloatFishFailsWithNoFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().FishingGloatCost;
            MockUtils.ClearFisherRecords(db, user);
            var response = GloatView.GloatFish(user, 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("!cast"));
        }

        [TestMethod]
        public void GloatPetFailsWithNoPets()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.GetPlayerByUser(user).Currency = SettingsManager.GetGameSettings().PetGloatCost;
            MockUtils.ClearPetRecords(db, user);
            var response = GloatView.GloatPet(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("!queue"));
        }

        [TestMethod]
        public void GloatFishFailsWithInsufficientCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = 0;
            var response = GloatView.GloatFish(user, 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("coins"));
            Assert.IsFalse(responses[0].Contains("wolfcoins"));
        }

        [TestMethod]
        public void GloatLevelFailsWithInsufficientCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = 0;
            var response = GloatView.GloatLevel(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(0, response.Messages.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("coins"));
            Assert.IsFalse(responses[0].Contains("wolfcoins"));
        }

        [TestMethod]
        public void GloatPetFailsWithInsufficientCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Currency = 0;
            var response = GloatView.GloatPet(user);
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
