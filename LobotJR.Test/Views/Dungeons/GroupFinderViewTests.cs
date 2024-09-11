using Autofac;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Command.View.Dungeons;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Views.Dungeons
{
    [TestClass]
    public class GroupFinderViewTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private PartyController PartyController;
        private PlayerController PlayerController;
        private GroupFinderController Controller;
        private GroupFinderView View;
        private User User;
        private PlayerCharacter Player;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            PartyController = AutofacMockSetup.Container.Resolve<PartyController>();
            Controller = AutofacMockSetup.Container.Resolve<GroupFinderController>();
            View = AutofacMockSetup.Container.Resolve<GroupFinderView>();
            User = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().First();
            AutofacMockSetup.ResetPlayers();
            Controller.ResetQueue();
            PlayerController.ClearRespecs();
            PartyController.ResetGroups();
            Player = AutofacMockSetup.ConnectionManager.CurrentConnection.PlayerCharacters.Read(x => x.UserId.Equals(User.TwitchId)).First();
            Player.Level = 10;
            Player.CharacterClass = AutofacMockSetup.ConnectionManager.CurrentConnection.CharacterClassData.Read(x => x.CanPlay).First();
            Player.Currency = 1000;
        }

        [TestMethod]
        public void DailyStatusGetsRemainingTimeToNextBonus()
        {
            Controller.SetLockout(Player);
            ConnectionManager.CurrentConnection.Commit();
            var response = View.DailyStatus(User);
            var lockout = ConnectionManager.CurrentConnection.DungeonTimerData.Read().First();
            // Shave off one second since at least one millisecond will go by between the two times, and readable time floors all time values
            var timestamp = TimeSpan.FromSeconds(lockout.Length * 60f - 1).ToReadableTime();
            Assert.IsTrue(response.Responses.First().Contains(timestamp));
        }

        [TestMethod]
        public void DailyStatusReturnsEligibleMessage()
        {
            var response = View.DailyStatus(User);
            Assert.IsTrue(response.Responses.First().Contains("are eligible"));
        }

        [TestMethod]
        public void QueueAddsUserToGroupFinderQueue()
        {
            SettingsManager.GetGameSettings().DungeonLevelRestrictions = true;
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains("have been placed"));
            var entry = Controller.GetPlayerQueueEntry(Player);
            Assert.IsTrue(Controller.IsPlayerQueued(Player));
            Assert.AreEqual(2, entry.Dungeons.Count());
        }

        [TestMethod]
        public void QueueAddsUserToGroupFinderQueueForSpecificDungeons()
        {
            var response = View.QueueForDungeonFinder(User, "1, 1h");
            Assert.IsTrue(response.Responses.First().Contains("have been placed"));
            var entry = Controller.GetPlayerQueueEntry(Player);
            Assert.IsTrue(Controller.IsPlayerQueued(Player));
            Assert.AreEqual(2, entry.Dungeons.Count());
            Assert.IsTrue(entry.Dungeons.All(x => x.DungeonId.Equals(1)));
            Assert.IsTrue(entry.Dungeons.Any(x => x.ModeId == 1));
            Assert.IsTrue(entry.Dungeons.Any(x => x.ModeId == 2));
        }

        [TestMethod]
        public void QueueReturnsErrorIfUserAlreadyInQueue()
        {
            Controller.QueuePlayer(Player, Array.Empty<DungeonRun>());
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains("!queuetime"));
        }

        [TestMethod]
        public void QueueReturnsErrorIfPlayerCannotAffordDungeon()
        {
            Player.Currency = 0;
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains("money"));
            var settings = SettingsManager.GetGameSettings();
            var cost = settings.DungeonBaseCost + (Player.Level - PlayerController.MinLevel) * settings.DungeonLevelCost;
            Assert.IsTrue(response.Responses.First().Contains("money"));
            Assert.IsTrue(response.Responses.First().Contains(cost.ToString()));
        }

        [TestMethod]
        public void QueueReturnsErrorIfUserHasPendingInvite()
        {
            var party = PartyController.CreateParty(false, Array.Empty<string>());
            PartyController.InvitePlayer(party, Player);
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains("outstanding invite"));
        }

        [TestMethod]
        public void QueueReturnsErrorIfUserInParty()
        {
            PartyController.CreateParty(false, Player);
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains("already have a party"));
        }

        [TestMethod]
        public void QueueReturnsErrorIfUserHasNotSelectedClass()
        {
            Player.CharacterClass = ConnectionManager.CurrentConnection.CharacterClassData.Read(x => !x.CanPlay).First();
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains("select a class"));
        }

        [TestMethod]
        public void QueueReturnsErrorIfUserIsTooLowLevel()
        {
            Player.Level = 0;
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains($"level {PlayerController.MinLevel}"));
        }

        [TestMethod]
        public void QueueReturnsErrorIfUserHasPendingRespec()
        {
            PlayerController.FlagForRespec(Player);
            var response = View.QueueForDungeonFinder(User);
            Assert.IsTrue(response.Responses.First().Contains("respec"));
        }

        [TestMethod]
        public void LeaveQueueRemoveUserFromGroupFinderQueue()
        {
            Controller.QueuePlayer(Player, Array.Empty<DungeonRun>());
            var response = View.LeaveQueue(User);
            Assert.IsTrue(response.Responses.First().Contains("removed"));
        }

        [TestMethod]
        public void LeaveQueueReturnsErrorIfUserNotInQueue()
        {
            var response = View.LeaveQueue(User);
            Assert.IsTrue(response.Responses.First().Contains("not queued"));
        }

        [TestMethod]
        public void GetQueueTimeGetsUserTimeInQueue()
        {
            Controller.QueuePlayer(Player, Array.Empty<DungeonRun>());
            var entry = Controller.GetPlayerQueueEntry(Player);
            entry.QueueTime = DateTime.Now - TimeSpan.FromSeconds(30);
            var response = View.GetQueueTime(User);
            Assert.IsTrue(response.Responses.First().Contains("30 seconds"));
        }

        [TestMethod]
        public void GetQueueTimeReturnsErrorIfUserNotInQueue()
        {
            var response = View.GetQueueTime(User);
            Assert.IsTrue(response.Responses.First().Contains("not queued"));
        }
    }
}
