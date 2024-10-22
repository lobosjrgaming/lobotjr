using Autofac;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Player;
using LobotJR.Command.View.Player;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Views.Player
{
    [TestClass]
    public class PlayerAdminTests
    {
        private SettingsManager SettingsManager;
        private UserController UserController;
        private PlayerController PlayerController;
        private PlayerAdmin View;
        private User User;
        private User Other;
        private List<CharacterClass> Classes;

        [TestInitialize]
        public void Initialize()
        {
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            User = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().First();
            Other = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().ElementAt(1);
            View = AutofacMockSetup.Container.Resolve<PlayerAdmin>();
            Classes = AutofacMockSetup.ConnectionManager.CurrentConnection.CharacterClassData.Read().ToList();
        }

        [TestMethod]
        public void GiveExperienceToUserReturnsErrorOnUserNotFound()
        {
            var username = "InvalidUsername";
            var response = View.GiveExperienceToUser(username, 100);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Unable") && x.Contains(username)));
        }

        [TestMethod]
        public void GiveExperienceToUsersGivesExperience()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var amount = 100;
            var response = View.GiveExperienceToUser(User.Username, amount);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(amount.ToString()) && x.Contains(User.Username)));
        }

        [TestMethod]
        public async Task GiveExperienceToAllGivesExperienceToViewers()
        {
            var viewers = await UserController.GetViewerList();
            var amounts = viewers.Select(x => PlayerController.GetPlayerByUser(x)).ToDictionary(x => x, x => x.Experience);
            var amount = 100;
            var response = View.GiveExperienceToAll(amount);
            Assert.AreNotEqual(0, amounts.Count);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(amounts.Count.ToString())));
            foreach (var pair in amounts)
            {
                Assert.AreEqual(pair.Value + amount, pair.Key.Experience);
            }
        }

        [TestMethod]
        public void SetExperienceReturnsErrorOnUserNotFound()
        {
            var username = "InvalidUsername";
            var response = View.SetExperience(username, 100);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Unable") && x.Contains(username)));
        }

        [TestMethod]
        public void SetExperienceSetsPlayerExperience()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Experience = 0;
            var amount = 1000;
            var response = View.GiveExperienceToUser(User.Username, amount);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(amount.ToString()) && x.Contains(User.Username)));
            Assert.AreEqual(amount, player.Experience);
        }

        [TestMethod]
        public void SetPrestigeReturnsErrorOnUserNotFound()
        {
            var username = "InvalidUsername";
            var response = View.SetPrestige(username, 100);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Unable") && x.Contains(username)));
        }

        [TestMethod]
        public void SetPrestigeSetsPlayerPrestige()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Prestige = 0;
            var amount = 10;
            var response = View.SetPrestige(User.Username, amount);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(amount.ToString()) && x.Contains(User.Username)));
            Assert.AreEqual(amount, player.Prestige);
        }

        [TestMethod]
        public void GiveCoinsReturnsErrorOnUserNotFound()
        {
            var username = "InvalidUsername";
            var response = View.GiveCoins(username, 100);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Unable") && x.Contains(username)));
        }

        [TestMethod]
        public void GiveCoinsGivesCoinsToPlayer()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = 0;
            var amount = 1000;
            var response = View.GiveCoins(User.Username, amount);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(amount.ToString()) && x.Contains(User.Username)));
            Assert.AreEqual(amount, player.Currency);
        }

        [TestMethod]
        public void RemoveCoinsReturnsErrorOnUserNotFound()
        {
            var username = "InvalidUsername";
            var response = View.RemoveCoins(username, 100);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Unable") && x.Contains(username)));
        }

        [TestMethod]
        public void RemoveCoinsRemovesCoinsFromPlayer()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = 1000;
            var amount = 1000;
            var response = View.RemoveCoins(User.Username, amount);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(amount.ToString()) && x.Contains(User.Username)));
            Assert.AreEqual(0, player.Currency);
        }

        [TestMethod]
        public void ResetPlayerReturnsErrorOnUserNotFound()
        {
            var username = "InvalidUsername";
            var response = View.ResetPlayer(username);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Unable") && x.Contains(username)));
        }

        [TestMethod]
        public void ResetPlayerRemovesPlayerClass()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var validClass = Classes.First(x => x.CanPlay);
            var invalidClass = Classes.First(x => !x.CanPlay);
            player.CharacterClass = validClass;
            var response = View.ResetPlayer(User.Username);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(User.Username)));
            Assert.AreEqual(invalidClass, player.CharacterClass);
            Assert.AreEqual(5, player.Level);
        }

        [TestMethod]
        public void SetIntervalUpdatesAwardInterval()
        {
            SettingsManager.GetGameSettings().ExperienceFrequency = 100;
            var frequency = 10;
            View.SetInterval(frequency);
            Assert.AreEqual(frequency, SettingsManager.GetGameSettings().ExperienceFrequency);
        }

        [TestMethod]
        public void SetMultiplierSetsAwardMultiplier()
        {
            PlayerController.SetMultiplier(1);
            var multiplier = 2;
            View.SetMultiplier(multiplier);
            Assert.AreEqual(multiplier, PlayerController.CurrentMultiplier);
        }

        [TestMethod]
        public void EnableExperienceTurnsOnExperience()
        {
            PlayerController.AwardsEnabled = false;
            View.EnableExperience(User, "");
            Assert.IsTrue(PlayerController.AwardsEnabled);
            Assert.AreEqual(User, PlayerController.AwardSetter);
        }

        [TestMethod]
        public void EnableExperienceReturnsAwardSetterIfAlreadyEnabled()
        {
            PlayerController.AwardsEnabled = true;
            PlayerController.AwardSetter = Other;
            var result = View.EnableExperience(User, "");
            Assert.IsTrue(PlayerController.AwardsEnabled);
            Assert.IsTrue(result.Messages.Any(x => x.Contains(Other.Username)));
        }

        [TestMethod]
        public void DisableExperienceTurnsOffExperience()
        {
            PlayerController.AwardsEnabled = true;
            View.DisableExperience("");
            Assert.IsFalse(PlayerController.AwardsEnabled);
        }

        [TestMethod]
        public void DisableExperienceReturnsErrorIfExperienceNotEnabled()
        {
            PlayerController.AwardsEnabled = false;
            var result = View.DisableExperience("");
            Assert.IsTrue(result.Messages.Any(x => x.Contains("isn't on")));
        }

        [TestMethod]
        public void PrintNextAwardSendsNextAwardTimeToChannel()
        {
            SettingsManager.GetGameSettings().ExperienceFrequency = 10;
            PlayerController.AwardsEnabled = true;
            PlayerController.LastAward = DateTime.Now - TimeSpan.FromSeconds(1);
            var result = View.PrintNextAward();
            Assert.IsTrue(result.Messages.Any(x => x.Contains($"{SettingsManager.GetGameSettings().ExperienceFrequency - 1} minutes")));
        }

        [TestMethod]
        public void PrintNextAwardGivesErrorIfAwardsOverdue()
        {
            SettingsManager.GetGameSettings().ExperienceFrequency = 10;
            PlayerController.AwardsEnabled = true;
            PlayerController.LastAward = DateTime.Now - TimeSpan.FromMinutes(20);
            var result = View.PrintNextAward();
            Assert.IsTrue(result.Messages.Any(x => x.Contains("overdue")));
        }

        [TestMethod]
        public void PrintNextAwardGivesErrorIfAwardsDisabled()
        {
            SettingsManager.GetGameSettings().ExperienceFrequency = 10;
            PlayerController.AwardsEnabled = false;
            PlayerController.LastAward = DateTime.Now;
            var result = View.PrintNextAward();
            Assert.IsTrue(result.Messages.Any(x => x.Contains($"not currently enabled")));

        }
    }
}
