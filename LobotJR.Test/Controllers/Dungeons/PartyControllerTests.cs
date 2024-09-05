using Autofac;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Controllers.Dungeons
{
    [TestClass]
    public class PartyControllerTests
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
        public void GetAllGroupsGetsAllRegisteredGroups() { }

        [TestMethod]
        public void GetCurrentGroupGetsGroupForPlayer() { }

        [TestMethod]
        public void GetCurrentGroupsReturnsNullForPlayerNotInGroup() { }

        [TestMethod]
        public void CreatePartyCreatesNewPartyWithMembers() { }

        [TestMethod]
        public void DisbandPartyRemovesParty() { }

        [TestMethod]
        public void IsPartyLeaderReturnsTrueForPartyLeader() { }

        [TestMethod]
        public void IsPartyLeaderReturnsFalseForNonLeaders() { }

        [TestMethod]
        public void SetLeaderSetsNewPartyLeader() { }

        [TestMethod]
        public void SetLeaderReturnsFalseIfPlayerIsAlreadyLeader() { }

        [TestMethod]
        public void InvitePlayerAddsPendingInvite() { }

        [TestMethod]
        public void InvitePlayerReturnsFalseIfPlayerAlreadyInvited() { }

        [TestMethod]
        public void InvitePlayerReturnsFalseIfPlayerAlreadyInGroup() { }

        [TestMethod]
        public void InvitePlayerReturnsFalseIfGroupIsFull() { }

        [TestMethod]
        public void AcceptInviteAddsPlayerToGroup() { }

        [TestMethod]
        public void AcceptInviteReturnsFalseIfPlayerNotInvited() { }

        [TestMethod]
        public void AcceptInviteReturnsFalseIfGroupIsFull() { }

        [TestMethod]
        public void DeclineInviteRemovesPendingInvite() { }

        [TestMethod]
        public void DeclineInviteReturnsFalseIfPlayerNotInvited() { }

        [TestMethod]
        public void AddPlayerAddsPlayerToParty() { }

        [TestMethod]
        public void AddPlayerReturnsFalseIfPartyStateNotForming() { }

        [TestMethod]
        public void AddPlayerReturnsFalseIfPartyIsFull() { }

        [TestMethod]
        public void AddPlayerSetsPartyStateToReadyIfPartyIsFull() { }

        [TestMethod]
        public void RemovePlayerRemovesPlayerFromGroup() { }

        [TestMethod]
        public void RemovePlayerReturnsFalseIfPlayerNotInGroup() { }

        [TestMethod]
        public void RemovePlayerReturnsFalseIfPartyStateStartedOrComplete() { }

        [TestMethod]
        public void RemovePlayerSetsPartyStateToFormingIfPartyCountWasMax() { }

        [TestMethod]
        public void RemovePlayerDisbandsEmptyGroup() { }

        [TestMethod]
        public void SetReadySetsStateToFullIfForming() { }

        [TestMethod]
        public void SetReadyReturnsFalseIfPartyStateNotForming() { }

        [TestMethod]
        public void SetReadyReturnsFalseIfPartyHasPendingInvites() { }

        [TestMethod]
        public void UnsetReadyRemovesReadyState() { }

        [TestMethod]
        public void UnsetReadyReturnsFalseIfPartyStateNotFull() { }

        [TestMethod]
        public void UnsetReadyReturnsFalseIfPartyIsFull() { }
    }
}
