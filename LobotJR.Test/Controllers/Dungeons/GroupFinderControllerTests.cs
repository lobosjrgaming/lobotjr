using Autofac;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LobotJR.Test.Controllers.Dungeons
{
    [TestClass]
    public class GroupFinderControllerTests
    {
        private IConnectionManager ConnectionManager;
        private PartyController PartyController;
        private GroupFinderController Controller;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            PartyController = AutofacMockSetup.Container.Resolve<PartyController>();
            Controller = AutofacMockSetup.Container.Resolve<GroupFinderController>();
            AutofacMockSetup.ResetPlayers();
            Controller.ResetQueue();
        }

        [TestMethod]
        public void GetLockoutTimeGetsTimeUntilLockoutExpires()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var timer = db.DungeonTimerData.Read().First();
            db.DungeonLockouts.Create(new DungeonLockout()
            {
                UserId = player.UserId,
                Timer = timer,
                Time = DateTime.Now
            });
            db.Commit();
            var time = Controller.GetLockoutTime(player);
            Assert.AreEqual(timer.Length, Math.Round(time.TotalMinutes));
        }

        [TestMethod]
        public void GetLockoutTimeGetsTimeForBaseTimeLockouts()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var timer = db.DungeonTimerData.Read().First();
            timer.BaseTime = DateTime.Now.AddMinutes(-1);
            db.DungeonLockouts.Create(new DungeonLockout()
            {
                UserId = player.UserId,
                Timer = timer,
                Time = DateTime.Now
            });
            db.Commit();
            var time = Controller.GetLockoutTime(player);
            timer.BaseTime = null;
            Assert.AreEqual(timer.Length - 1, Math.Round(time.TotalMinutes));
        }

        [TestMethod]
        public void GetLockoutTimeReturnsZeroIfPlayerHasNoLockouts()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var time = Controller.GetLockoutTime(player);
            Assert.AreEqual(TimeSpan.Zero, time);
        }

        [TestMethod]
        public void GetLockoutTimeReturnsZeroIfLockoutsHaveExpired()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var timer = db.DungeonTimerData.Read().First();
            db.DungeonLockouts.Create(new DungeonLockout()
            {
                UserId = player.UserId,
                Timer = timer,
                Time = DateTime.Now - TimeSpan.FromMinutes(timer.Length)
            });
            db.Commit();
            var time = Controller.GetLockoutTime(player);
            Assert.AreEqual(TimeSpan.Zero, time);
        }

        [TestMethod]
        public void SetLockoutUpdatesLockoutTime()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var timer = db.DungeonTimerData.Read().First();
            db.DungeonLockouts.Create(new DungeonLockout()
            {
                UserId = player.UserId,
                Timer = timer,
                Time = DateTime.Now - TimeSpan.FromMinutes(timer.Length)
            });
            db.Commit();
            Controller.SetLockout(player);
            var now = DateTime.Now;
            db.Commit();
            var lockout = db.DungeonLockouts.Read(x => x.UserId.Equals(player.UserId)).First();
            Assert.AreEqual(now, lockout.Time);
        }

        [TestMethod]
        public void SetLockoutCreatesNewLockoutIfPlayerHasNoLockoutRecord()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            Controller.SetLockout(player);
            var now = DateTime.Now;
            db.Commit();
            var lockout = db.DungeonLockouts.Read(x => x.UserId.Equals(player.UserId)).First();
            Assert.IsTrue(Math.Abs((lockout.Time - now).TotalMilliseconds) < 16);
        }

        [TestMethod]
        public void GetPlayerQueueEntryGetsPlayerGroupFinderRecord()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var now = DateTime.Now;
            Controller.QueuePlayer(player, Array.Empty<DungeonRun>());
            var entry = Controller.GetPlayerQueueEntry(player);
            Assert.AreEqual(player, entry.Player);
            Assert.AreEqual(0, entry.Dungeons.Count());
            Assert.IsTrue(Math.Abs((entry.QueueTime - now).TotalMilliseconds) < 16);
        }

        [TestMethod]
        public void GetPlayerQueueEntryReturnsNullForPlayersNotInQueue()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var entry = Controller.GetPlayerQueueEntry(player);
            Assert.IsNull(entry);
        }

        [TestMethod]
        public void IsPlayerQueuedReturnsTrueIfPlayerInQueue()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            Controller.QueuePlayer(player, Array.Empty<DungeonRun>());
            var result = Controller.IsPlayerQueued(player);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsPlayerQueuedReturnsFalseIfPlayerNotInQueue()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var result = Controller.IsPlayerQueued(player);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void QueuePlayerAddsPlayerToQueue()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            Controller.QueuePlayer(player, Array.Empty<DungeonRun>());
            var entry = Controller.GetPlayerQueueEntry(player);
            var isQueued = Controller.IsPlayerQueued(player);
            Assert.IsNotNull(entry);
            Assert.IsTrue(isQueued);
        }

        [TestMethod]
        public void QueuePlayerCreatesPartyIfPossible()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            player3.CharacterClass = db.CharacterClassData.Read(x => x.CanPlay && !x.Equals(player.CharacterClass)).First();
            var run = new DungeonRun(db.DungeonData.Read().First(), db.DungeonModeData.Read().First());
            var listener = new Mock<GroupFinderController.DungeonQueueHandler>();
            Controller.PartyFound += listener.Object;
            Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player2, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player3, new List<DungeonRun>() { run });
            listener.Verify(x => x(It.Is<Party>(
                y => y.Members.Contains(player) && y.Members.Contains(player2) && y.Members.Contains(player3)
            )), Times.Once());
            Assert.IsFalse(Controller.IsPlayerQueued(player));
            Assert.IsFalse(Controller.IsPlayerQueued(player2));
            Assert.IsFalse(Controller.IsPlayerQueued(player3));
            var party1 = PartyController.GetCurrentGroup(player);
            var party2 = PartyController.GetCurrentGroup(player2);
            var party3 = PartyController.GetCurrentGroup(player3);
            Assert.AreEqual(party1, party2);
            Assert.AreEqual(party2, party3);
            Assert.AreEqual(PartyState.Full, party1.State);
        }

        [TestMethod]
        public void QueuePlayerSetsNewestMemberAsGroupLeader()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            player3.CharacterClass = db.CharacterClassData.Read(x => x.CanPlay && !x.Equals(player.CharacterClass)).First();
            var run = new DungeonRun(db.DungeonData.Read().First(), db.DungeonModeData.Read().First());
            var listener = new Mock<GroupFinderController.DungeonQueueHandler>();
            Controller.PartyFound += listener.Object;
            Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player2, new List<DungeonRun>() { run });
            Thread.Sleep(1);
            Controller.QueuePlayer(player3, new List<DungeonRun>() { run });
            listener.Verify(x => x(It.Is<Party>(y => y.Leader.Equals(player3))), Times.Once());
        }

        [TestMethod]
        public void QueuePlayerPrioritizesOldestQueueEntries()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            var player4 = db.PlayerCharacters.Read().ElementAt(4);
            player4.CharacterClass = db.CharacterClassData.Read(x => x.CanPlay && !x.Equals(player.CharacterClass)).First();
            var run = new DungeonRun(db.DungeonData.Read().First(), db.DungeonModeData.Read().First());
            var listener = new Mock<GroupFinderController.DungeonQueueHandler>();
            Controller.PartyFound += listener.Object;
            Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player2, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player3, new List<DungeonRun>() { run });
            Thread.Sleep(1);
            Controller.QueuePlayer(player4, new List<DungeonRun>() { run });
            listener.Verify(x => x(It.Is<Party>(y => !y.Members.Contains(player3))), Times.Once());
        }

        [TestMethod]
        public void QueuePlayerDoesNotCreateGroupWithMoreThanTwoOfTheSameClass()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            var run = new DungeonRun(db.DungeonData.Read().First(), db.DungeonModeData.Read().First());
            var listener = new Mock<GroupFinderController.DungeonQueueHandler>();
            Controller.PartyFound += listener.Object;
            Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player2, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player3, new List<DungeonRun>() { run });
            listener.Verify(x => x(It.IsAny<Party>()), Times.Never());
        }

        [TestMethod]
        public void QueuePlayerDoesNotCreateGroupIfNoDungeonsInCommon()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            player3.CharacterClass = db.CharacterClassData.Read(x => x.CanPlay && !x.Equals(player.CharacterClass)).First();
            var run = new DungeonRun(db.DungeonData.Read().First(), db.DungeonModeData.Read().First());
            var listener = new Mock<GroupFinderController.DungeonQueueHandler>();
            Controller.PartyFound += listener.Object;
            Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player2, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player3, new List<DungeonRun>());
            listener.Verify(x => x(It.IsAny<Party>()), Times.Never());
        }

        [TestMethod]
        public void QueuePlayerReturnsFalseIfPlayerAlreadyInQueue()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var run = new DungeonRun(db.DungeonData.Read().First(), db.DungeonModeData.Read().First());
            Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            var result = Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetQueueEntriesGetsAllQueueRecords()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            player3.CharacterClass = db.CharacterClassData.Read(x => x.CanPlay && !x.Equals(player.CharacterClass)).First();
            var run = new DungeonRun(db.DungeonData.Read().First(), db.DungeonModeData.Read().First());
            Controller.QueuePlayer(player, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player2, new List<DungeonRun>() { run });
            Controller.QueuePlayer(player3, new List<DungeonRun>());
            var entries = Controller.GetQueueEntries();
            Assert.AreEqual(3, entries.Count());
            Assert.IsTrue(entries.Any(x => x.Player.Equals(player)));
            Assert.IsTrue(entries.Any(x => x.Player.Equals(player2)));
            Assert.IsTrue(entries.Any(x => x.Player.Equals(player3)));
        }
    }
}
