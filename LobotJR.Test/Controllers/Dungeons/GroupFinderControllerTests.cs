using Autofac;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Controllers.Dungeons
{
    [TestClass]
    public class GroupFinderControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            AutofacMockSetup.ResetPlayers();
        }

        [TestMethod]
        public void GetLockoutTimeGetsTimeUntilLockoutExpires() { }

        [TestMethod]
        public void GetLockoutTimeGetsTimeForBaseTimeLockouts() { }

        [TestMethod]
        public void GetLockoutTimeReturnsZeroIfPlayerHasNoLockouts() { }

        [TestMethod]
        public void GetLockoutTimeReturnsZeroIfLockoutsHaveExpired() { }

        [TestMethod]
        public void SetLockoutUpdatesLockoutTime() { }

        [TestMethod]
        public void SetLockoutCreatesNewLockoutIfPlayerHasNoLockoutRecord() { }

        [TestMethod]
        public void GetPlayerQueueEntryGetsPlayerGroupFinderRecord() { }

        [TestMethod]
        public void GetPlayerQueueEntryReturnsNullForPlayersNotInQueue() { }

        [TestMethod]
        public void IsPlayerQueuedReturnsTrueIfPlayerInQueue() { }

        [TestMethod]
        public void IsPlayerQueuedReturnsFalseIfPlayerNotInQueue() { }

        [TestMethod]
        public void QueuePlayerAddsPlayerToQueue() { }

        [TestMethod]
        public void QueuePlayerCreatesPartyIfPossible() { }

        [TestMethod]
        public void QueuePlayerSetsNewestMemberAsGroupLeader() { }

        [TestMethod]
        public void QueuePlayerPrioritizesOldestQueueEntries() { }

        [TestMethod]
        public void QueuePlayerDoesNotCreateGroupWithMoreThanTwoOfTheSameClass() { }

        [TestMethod]
        public void QueuePlayerDoesNotCreateGroupIfNoDungeonsInCommon() { }

        [TestMethod]
        public void QueuePlayerReturnsFalseIfPlayerAlreadyInQueue() { }

        [TestMethod]
        public void GetQueueEntriesGetsAllQueueRecords() { }
    }
}
