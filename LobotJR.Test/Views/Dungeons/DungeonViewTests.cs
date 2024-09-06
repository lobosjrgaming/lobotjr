using Autofac;
using LobotJR.Command.View.Dungeons;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Dungeons
{
    [TestClass]
    public class DungeonViewTests
    {
        private DungeonView View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<DungeonView>();
        }

        [TestMethod]
        public void DungeonDetailsReturnsDungeonData() { }

        [TestMethod]
        public void DungeonDetailsReturnsErrorOnMalformedDungeon() { }

        [TestMethod]
        public void DungeonDetailsReturnsErrorOnInvalidDungeonId() { }

        [TestMethod]
        public void CreatePartyCreatesNewPartyForUser() { }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserAlreadyInParty() { }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserIsPendingInvite() { }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserIsInGroupFinderQueue() { }

        [TestMethod]
        public void AddPlayerInvitesPlayerToParty() { }

        [TestMethod]
        public void AddPlayerCreatesNewPartyIfUserIsNotInOne() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPartyIsFull() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPartyIsNotInValidState() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfUserIsNotPartyLeader() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerIsInAParty() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerHasNotSelectedClass() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerIsLowLevel() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerIsUser() { }

        [TestMethod]
        public void AddPlayerReturnsErrorOnPlayerNotFound() { }

        [TestMethod]
        public void AddPlayerReturnsErrorIfUserInGroupFinderQueue() { }

        [TestMethod]
        public void RemovePlayerRemovesPlayerFromParty() { }

        [TestMethod]
        public void RemovePlayerDisbandsPartyWithOneUserLeft() { }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfPlayerNotInGroup() { }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfPlayerIsUser() { }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfPartyIsInDungeon() { }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfUserIsNotPartyLeader() { }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfUserIsNotInParty() { }

        [TestMethod]
        public void PromotePlayerSetsNewPartyLeader() { }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfPlayerNotInGroup() { }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfPlayerIsLeader() { }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfUserIsNotPartyLeader() { }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfUserIsNotInParty() { }

        [TestMethod]
        public void LeavePartyRemovesUserFromParty() { }

        [TestMethod]
        public void LeavePartyPromotesNewLeaderIfUserWasLeader() { }

        [TestMethod]
        public void LeavePartyReturnsErrorIfPartyIsInDungeon() { }

        [TestMethod]
        public void LeavePartyReturnsErrorIfUserIsNotInParty() { }

        [TestMethod]
        public void PartyChatSendsMessageToParty() { }

        [TestMethod]
        public void PartyChatReturnsErrorIfUserIsNotInParty() { }

        [TestMethod]
        public void SetReadySetsPartyStateToFull() { }

        [TestMethod]
        public void SetReadyReturnsErrorIfPartyHasPendingInvites() { }

        [TestMethod]
        public void SetReadyReturnsErrorIfPartyIsInDungeon() { }

        [TestMethod]
        public void SetReadyReturnsErrorIfUserIsNotInParty() { }

        [TestMethod]
        public void UnsetReadySetsPartyStateToForming() { }

        [TestMethod]
        public void UnsetReadyReturnsErrorIfPartyIsInDungeon() { }

        [TestMethod]
        public void UnsetReadyReturnsErrorIfUserIsNotPartyLeader() { }

        [TestMethod]
        public void UnsetReadyReturnsErrorIfUserIsNotInParty() { }

        [TestMethod]
        public void StartDungeonSendsPartyIntoDungeon() { }

        [TestMethod]
        public void StartDungeonSelectsRandomEligibleDungeonIfNoIdProvided() { }

        [TestMethod]
        public void StartDungeonReturnsErrorIfAnyPlayerCannotAfford() { }

        [TestMethod]
        public void StartDungeonReturnsErrorOnInvalidDungeonId() { }

        [TestMethod]
        public void StartDungeonReturnsErrorIfUserIsNotPartyLeader() { }

        [TestMethod]
        public void StartDungeonReturnsErrorIfUserIsNotInParty() { }

        [TestMethod]
        public void DungeonProgressEventSendsProgressUpdateMessage() { }

        [TestMethod]
        public void DungeonFailureEventSendsFailureMessage() { }

        [TestMethod]
        public void DungeonCompleteEventSendsCompleteMessageWithAwards() { }

        [TestMethod]
        public void PlayerDeathEventSendsDeathNotificationWithPenalties() { }

        [TestMethod]
        public void ConfirmEventAcceptsPartyInvite() { }

        [TestMethod]
        public void CanceEventDeclinesPartyInvite() { }
    }
}
