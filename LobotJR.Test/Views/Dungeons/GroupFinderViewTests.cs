using Autofac;
using LobotJR.Command.View.Dungeons;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Dungeons
{
    [TestClass]
    public class GroupFinderViewTests
    {
        private GroupFinderView View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<GroupFinderView>();
        }

        [TestMethod]
        public void DailyStatusGetsRemainingTimeToNextBonus() { }

        [TestMethod]
        public void DailyStatusReturnsEligibleMessage() { }

        [TestMethod]
        public void QueueAddsUserToGroupFinderQueue() { }

        [TestMethod]
        public void QueueReturnsErrorIfUserAlreadyInQueue() { }

        [TestMethod]
        public void QueueReturnsErrorIfPlayerCannotAffordDungeon() { }

        [TestMethod]
        public void QueueReturnsErrorIfUserHasPendingInvite() { }

        [TestMethod]
        public void QueueReturnsErrorIfUserInParty() { }

        [TestMethod]
        public void QueueReturnsErrorIfUserHasNotSelectedClass() { }

        [TestMethod]
        public void QueueReturnsErrorIfUserIsTooLowLevel() { }

        [TestMethod]
        public void QueueReturnsErrorIfUserHasPendingRespec() { }

        [TestMethod]
        public void LeaveQueueRemoveUserFromGroupFinderQueue() { }

        [TestMethod]
        public void LeaveQueueReturnsErrorIfUserNotInQueue() { }

        [TestMethod]
        public void GetQueueTimeGetsUserTimeInQueue() { }

        [TestMethod]
        public void GetQueueTimeReturnsErrorIfUserNotInQueue() { }
    }
}
