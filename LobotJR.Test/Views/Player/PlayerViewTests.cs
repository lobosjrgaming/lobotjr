using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Command.View;
using LobotJR.Command.View.Player;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Test.Views.Player
{
    [TestClass]
    public class PlayerViewTests
    {
        private readonly Random Random = new Random();
        private SettingsManager SettingsManager;
        private GroupFinderController GroupFinderController;
        private PartyController PartyController;
        private PlayerController PlayerController;
        private PlayerView View;
        private User User;
        private User Other;
        private IEnumerable<CharacterClass> Classes;

        [TestInitialize]
        public void Initialize()
        {
            var db = AutofacMockSetup.ConnectionManager.CurrentConnection;
            User = db.Users.Read().First();
            Other = db.Users.Read().ElementAt(2);
            Classes = db.CharacterClassData.Read().ToList();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            GroupFinderController = AutofacMockSetup.Container.Resolve<GroupFinderController>();
            PartyController = AutofacMockSetup.Container.Resolve<PartyController>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            View = AutofacMockSetup.Container.Resolve<PlayerView>();
            AutofacMockSetup.ResetPlayers();
            PlayerController.ClearRespecs();
            GroupFinderController.ResetQueue();
        }

        [TestMethod]
        public void GetCoinsRetrievesPlayerCoinCount()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var amount = Random.Next(0, 1000);
            player.Currency = amount;
            var result = View.GetCoins(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(amount.ToString())));
        }

        [TestMethod]
        public void GetCoinsReturnsHelpMessageAtZeroCoins()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = 0;
            var result = View.GetCoins(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("earn coins")));
        }

        [TestMethod]
        public void GetExperienceRetrievesLevel()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.CharacterClass = Classes.First(x => x.CanPlay);
            var level = Random.Next(3, 20);
            player.Level = level;
            var xp = Random.Next(1000, 10000);
            player.Experience = xp;
            var className = player.CharacterClass.Name;
            var result = View.GetExperience(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(level.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(xp.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(className.ToString())));
        }

        [TestMethod]
        public void GetExperienceRetrievesPrestige()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.CharacterClass = Classes.First(x => x.CanPlay);
            var prestige = Random.Next(3, 20);
            player.Prestige = prestige;
            player.Level = 3;
            player.Experience = 200;
            var result = View.GetExperience(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Prestige") && x.Contains(prestige.ToString())));
        }

        [TestMethod]
        public void GetExperienceRetrievesExperienceForLowLevelUsers()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var xp = Random.Next(1, 200);
            player.Level = 1;
            player.Experience = xp;
            var result = View.GetExperience(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(xp.ToString())));
        }

        [TestMethod]
        public void GetExperienceReturnsHelpMessageAtZeroExperience()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Level = 0;
            player.Experience = 0;
            var result = View.GetExperience(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("earn XP")));
        }

        [TestMethod]
        public void GetStatsCompactRetrievesExperienceAndCoins()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var result = View.GetStatsCompact(User);
            Assert.AreEqual(player, result.Items.First());
        }

        [TestMethod]
        public void GetStatsRetrievesExperienceAndCoins()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.CharacterClass = Classes.First(x => x.CanPlay);
            var coins = Random.Next(1, 100);
            player.Currency = coins;
            var level = Random.Next(3, 20);
            player.Level = level;
            var xp = Random.Next(1000, 10000);
            player.Experience = xp;
            var className = player.CharacterClass.Name;
            var result = View.GetStats(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(coins.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(xp.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(level.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(className.ToString())));
        }

        [TestMethod]
        public void PryRetrievesPlayerInfo()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.CharacterClass = Classes.First(x => x.CanPlay);
            var coins = Random.Next(1, 100);
            player.Currency = coins;
            var level = Random.Next(3, 20);
            player.Level = level;
            var xp = Random.Next(1000, 10000);
            player.Experience = xp;
            var className = player.CharacterClass.Name;
            var otherPlayer = PlayerController.GetPlayerByUser(Other);
            otherPlayer.Currency = SettingsManager.GetGameSettings().PryCost;
            var result = View.GetStats(Other, User.Username);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(coins.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(xp.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(level.ToString())));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(className.ToString())));
        }

        [TestMethod]
        public void PryReturnsErrorIfPlayerDoesNotExist()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = SettingsManager.GetGameSettings().PryCost;
            var result = View.GetStats(User, "UserDoesNotExist");
            Assert.IsTrue(result.Responses.Any(x => x.Contains("does not exist")));
        }

        [TestMethod]
        public void PryReturnsErrorIfPlayerCannotAfford()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = 0;
            var result = View.GetStats(User, Other.Username);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(SettingsManager.GetGameSettings().PryCost.ToString())));
        }

        [TestMethod]
        public void GetClassStatsRetrievesClassDistribution()
        {
            var result = View.GetClassStats();
            var classNames = Classes.Where(x => x.CanPlay).Select(x => x.Name);
            foreach (var className in classNames)
            {
                Assert.IsTrue(result.Responses.Any(x => x.Contains(className)));
            }
        }

        [TestMethod]
        public void SelectClassSetsPlayerInitialClass()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Level = 3;
            player.CharacterClass = Classes.First(x => !x.CanPlay);
            var newClass = Classes.First(x => x.CanPlay);
            var result = View.SelectClass(User, newClass.Id.ToString());
            Assert.IsTrue(result.Responses.Any(x => x.Contains(newClass.Name)));
            Assert.AreEqual(newClass, player.CharacterClass);
        }

        [TestMethod]
        public void SelectClassCompletesRespec()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.Currency = cost;
            player.Level = 3;
            PlayerController.FlagForRespec(player);
            var newClass = Classes.First(x => x.CanPlay && !x.Equals(player.CharacterClass));
            var result = View.SelectClass(User, newClass.Id.ToString());
            Assert.IsTrue(result.Responses.Any(x => x.Contains(newClass.Name)));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(cost.ToString())));
            Assert.AreEqual(newClass, player.CharacterClass);
            Assert.AreEqual(0, player.Currency);
        }

        [TestMethod]
        public void SelectClassReturnsErrorOnInvalidChoice()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var oldClass = player.CharacterClass;
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.Currency = cost;
            player.Level = 3;
            PlayerController.FlagForRespec(player);
            var result = View.SelectClass(User, "Invalid");
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Invalid")));
            Assert.AreEqual(oldClass, player.CharacterClass);
            Assert.AreEqual(cost, player.Currency);
        }

        [TestMethod]
        public void SelectClassReturnsErrorIfNotRespeccing()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.CharacterClass = Classes.First(x => x.CanPlay);
            player.Currency = cost;
            player.Level = 3;
            var oldClass = player.CharacterClass;
            var newClass = Classes.First(x => x.CanPlay && !x.Equals(player.CharacterClass));
            var result = View.SelectClass(User, newClass.Id.ToString());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("!respec")));
            Assert.AreEqual(oldClass, player.CharacterClass);
            Assert.AreEqual(cost, player.Currency);
        }

        [TestMethod]
        public void SelectClassReturnsErrorIfNotHighEnoughLevel()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.Currency = cost;
            player.Level = 2;
            var oldClass = player.CharacterClass;
            var newClass = Classes.First(x => x.CanPlay && !x.Equals(player.CharacterClass));
            var result = View.SelectClass(User, newClass.Id.ToString());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("not high enough")));
            Assert.AreEqual(oldClass, player.CharacterClass);
            Assert.AreEqual(cost, player.Currency);
        }

        [TestMethod]
        public void ClassHelpGetsHelpMessage()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Level = 3;
            player.CharacterClass = Classes.First(x => x.CanPlay);
            var result = View.ClassHelp(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("!respec")));
        }

        [TestMethod]
        public void ClassHelpGetsContinueWatchingMessageForLowLevelPlayers()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.Level = 2;
            var result = View.ClassHelp(User);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("not high enough")));
        }

        [TestMethod]
        public void ClassHelpGetsClassListForPlayersWhoHaveNotPickedAClass()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.CharacterClass = Classes.First(x => !x.CanPlay);
            player.Level = 3;
            var result = View.ClassHelp(User);
            var choices = Classes.Where(x => x.CanPlay).Select(x => $"!C{x.Id}");
            foreach (var choice in choices)
            {
                Assert.IsTrue(result.Responses.Any(x => x.Contains(choice)));
            }
        }

        [TestMethod]
        public void RespecFlagsForRespec()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.CharacterClass = Classes.First(x => x.CanPlay);
            player.Currency = cost;
            player.Level = 3;
            var result = View.Respec(User);
            Assert.IsTrue(PlayerController.IsFlaggedForRespec(player));
            var choices = Classes.Where(x => x.CanPlay).Select(x => $"!C{x.Id}");
            foreach (var choice in choices)
            {
                Assert.IsTrue(result.Responses.Any(x => x.Contains(choice)));
            }
        }

        [TestMethod]
        public void RespecReturnsErrorIfCannotAfford()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.CharacterClass = Classes.First(x => x.CanPlay);
            player.Currency = 0;
            player.Level = 3;
            var result = View.Respec(User);
            Assert.IsFalse(PlayerController.IsFlaggedForRespec(player));
            Assert.IsTrue(result.Responses.Any(x => x.Contains(cost.ToString())));
        }

        [TestMethod]
        public void RespecReturnsErrorIfInDungeonQueue()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            GroupFinderController.QueuePlayer(player, Array.Empty<DungeonRun>());
            player.CharacterClass = Classes.First(x => x.CanPlay);
            player.Currency = cost;
            player.Level = 3;
            var result = View.Respec(User);
            Assert.IsFalse(PlayerController.IsFlaggedForRespec(player));
            Assert.IsTrue(result.Responses.Any(x => x.Contains("queue")));
        }

        [TestMethod]
        public void RespecReturnsErrorIfInParty()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            PartyController.CreateParty(false, player);
            player.CharacterClass = Classes.First(x => x.CanPlay);
            player.Currency = cost;
            player.Level = 3;
            var result = View.Respec(User);
            Assert.IsFalse(PlayerController.IsFlaggedForRespec(player));
            Assert.IsTrue(result.Responses.Any(x => x.Contains("party")));
        }

        [TestMethod]
        public void RespecReturnsErrorForPlayersWithoutAClass()
        {
            var player = PlayerController.GetPlayerByUser(User);
            player.CharacterClass = Classes.First(x => !x.CanPlay);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.Currency = cost;
            player.Level = 3;
            var result = View.Respec(User);
            Assert.IsFalse(PlayerController.IsFlaggedForRespec(player));
            var choices = Classes.Where(x => x.CanPlay).Select(x => $"!C{x.Id}");
            foreach (var choice in choices)
            {
                Assert.IsTrue(result.Responses.Any(x => x.Contains(choice)));
            }
        }

        [TestMethod]
        public void RespecReturnsErrorForLowLevelPlayers()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var cost = SettingsManager.GetGameSettings().RespecCost;
            player.Currency = cost;
            player.Level = 2;
            var result = View.Respec(User);
            Assert.IsFalse(PlayerController.IsFlaggedForRespec(player));
            Assert.IsTrue(result.Responses.Any(x => x.Contains("gain experience")));
        }

        [TestMethod]
        public void CancelEventCancelsRespec()
        {
            var player = PlayerController.GetPlayerByUser(User);
            PlayerController.FlagForRespec(player);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var confirm = AutofacMockSetup.Container.Resolve<ConfirmationController>();
            confirm.Cancel(User);
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains("Respec")))));
        }

        [TestMethod]
        public void LevelUpEventSendsLevelUpMessage()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            PlayerController.GainExperience(player, PlayerController.GetExperienceToNextLevel(player.Experience));
            listener.Verify(x => x(User, It.Is<CommandResult>(
                y => y.Responses.Any(z => z.Contains($"level {player.Level}"))
            )));
        }

        [TestMethod]
        public void LevelUpEventSendsPrestigeMessage()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var listener = new Mock<PushNotificationHandler>();
            player.Level = 20;
            player.Experience = 32100;
            View.PushNotification += listener.Object;
            PlayerController.GainExperience(player, PlayerController.GetExperienceToNextLevel(player.Experience));
            listener.Verify(x => x(User, It.Is<CommandResult>(
                y => y.Responses.Any(z => z.Contains($"Prestige {player.Prestige}") && z.Contains("3"))
            )));
        }

        [TestMethod]
        public void LevelUpEventSendsClassChoiceMessage()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var listener = new Mock<PushNotificationHandler>();
            player.Level = 2;
            player.Experience = 150;
            View.PushNotification += listener.Object;
            PlayerController.GainExperience(player, PlayerController.GetExperienceToNextLevel(player.Experience));
            listener.Verify(x => x(User, It.Is<CommandResult>(
                y => y.Responses.Any(z => z.Contains("choose a class"))
            )));
        }

        [TestMethod]
        public void LevelUpEventSendsClassChoiceReminder()
        {
            var player = PlayerController.GetPlayerByUser(User);
            var listener = new Mock<PushNotificationHandler>();
            player.Level = 3;
            player.Experience = 250;
            View.PushNotification += listener.Object;
            PlayerController.GainExperience(player, PlayerController.GetExperienceToNextLevel(player.Experience));
            listener.Verify(x => x(User, It.Is<CommandResult>(
                y => y.Responses.Any(z => z.Contains("haven't yet"))
            )));
        }
    }
}
