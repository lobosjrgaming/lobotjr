using Autofac;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Controllers.Dungeons
{
    [TestClass]
    public class PartyControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private PartyController Controller;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            Controller = AutofacMockSetup.Container.Resolve<PartyController>();
            AutofacMockSetup.ResetPlayers();
            Controller.ResetGroups();
        }

        [TestMethod]
        public void GetAllGroupsGetsAllRegisteredGroups()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            Controller.CreateParty(true, players.ElementAt(3), players.ElementAt(4), players.ElementAt(5));
            var groups = Controller.GetAllGroups();
            Assert.AreEqual(2, groups.Count());
            Assert.IsTrue(groups.Any(x => x.IsQueueGroup));
            Assert.IsTrue(groups.Any(x => !x.IsQueueGroup));
        }

        [TestMethod]
        public void GetCurrentGroupGetsGroupForPlayer()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            Controller.CreateParty(true, players.ElementAt(3), players.ElementAt(4), players.ElementAt(5));
            var group = Controller.GetCurrentGroup(players.First());
            Assert.IsNotNull(group);
        }

        [TestMethod]
        public void GetCurrentGroupsReturnsNullForPlayerNotInGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var group = Controller.GetCurrentGroup(players.First());
            Assert.IsNull(group);
        }

        [TestMethod]
        public void CreatePartyCreatesNewPartyWithMembers()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            Assert.IsNotNull(party);
            Assert.IsTrue(party.Members.Any(x => x.Equals(players.First())));
            Assert.IsTrue(party.Members.Any(x => x.Equals(players.ElementAt(1))));
            Assert.IsTrue(party.Members.Any(x => x.Equals(players.ElementAt(2))));
        }

        [TestMethod]
        public void DisbandPartyRemovesParty()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            Controller.DisbandParty(party);
            Assert.AreEqual(0, Controller.GetAllGroups().Count());
        }

        [TestMethod]
        public void IsPartyLeaderReturnsTrueForPartyLeader()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            var result = Controller.IsLeader(party, players.First());
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsPartyLeaderReturnsFalseForNonLeaders()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            var result1 = Controller.IsLeader(party, players.ElementAt(1));
            var result2 = Controller.IsLeader(party, players.ElementAt(2));
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void SetLeaderSetsNewPartyLeader()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            var result = Controller.SetLeader(party, players.ElementAt(2));
            var oldLeader = Controller.IsLeader(party, players.First());
            var newLeader = Controller.IsLeader(party, players.ElementAt(2));
            Assert.IsTrue(result);
            Assert.IsFalse(oldLeader);
            Assert.IsTrue(newLeader);
        }

        [TestMethod]
        public void SetLeaderReturnsFalseIfPlayerIsAlreadyLeader()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            var oldLeader = Controller.IsLeader(party, players.First());
            var result = Controller.SetLeader(party, players.First());
            var newLeader = Controller.IsLeader(party, players.First());
            Assert.IsFalse(result);
            Assert.IsTrue(oldLeader);
            Assert.IsTrue(newLeader);
        }

        [TestMethod]
        public void InvitePlayerAddsPendingInvite()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            var result = Controller.InvitePlayer(party, players.ElementAt(1));
            Assert.IsTrue(result);
            Assert.IsTrue(party.PendingInvites.Contains(players.ElementAt(1)));
        }

        [TestMethod]
        public void InvitePlayerReturnsFalseIfPlayerAlreadyInvited()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.PendingInvites.Add(players.ElementAt(1));
            var result = Controller.InvitePlayer(party, players.ElementAt(1));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void InvitePlayerReturnsFalseIfPlayerAlreadyInGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1));
            var result = Controller.InvitePlayer(party, players.ElementAt(1));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void InvitePlayerReturnsFalseIfGroupIsFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            var result = Controller.InvitePlayer(party, players.ElementAt(3));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void InvitePlayerReturnsFalseIfGroupPendingInvitesMakesGroupFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.PendingInvites.Add(players.ElementAt(1));
            party.PendingInvites.Add(players.ElementAt(2));
            var result = Controller.InvitePlayer(party, players.ElementAt(3));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AcceptInviteAddsPlayerToGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Forming;
            party.PendingInvites.Add(players.ElementAt(1));
            var result = Controller.AcceptInvite(party, players.ElementAt(1));
            Assert.IsTrue(result);
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(party.Members.Any(x => x.Equals(players.ElementAt(1))));
        }

        [TestMethod]
        public void AcceptInviteReturnsFalseIfPlayerNotInvited()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Forming;
            var result = Controller.AcceptInvite(party, players.ElementAt(1));
            Assert.IsFalse(result);
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.AreEqual(1, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(1))));
        }

        [TestMethod]
        public void AcceptInviteReturnsFalseIfGroupIsFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            party.State = PartyState.Forming;
            party.PendingInvites.Add(players.ElementAt(3));
            var result = Controller.AcceptInvite(party, players.ElementAt(1));
            Assert.IsFalse(result);
            Assert.AreEqual(3, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(3))));
        }

        [TestMethod]
        public void DeclineInviteRemovesPendingInvite()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.PendingInvites.Add(players.ElementAt(1));
            var result = Controller.DeclineInvite(party, players.ElementAt(1));
            Assert.IsTrue(result);
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.AreEqual(1, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(1))));
        }

        [TestMethod]
        public void DeclineInviteReturnsFalseIfPlayerNotInvited()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            var result = Controller.DeclineInvite(party, players.ElementAt(1));
            Assert.IsFalse(result);
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.AreEqual(1, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(1))));
        }

        [TestMethod]
        public void AddPlayerAddsPlayerToParty()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            var result = Controller.AddPlayer(party, players.ElementAt(1));
            Assert.IsTrue(result);
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(party.Members.Any(x => x.Equals(players.ElementAt(1))));
        }

        [TestMethod]
        public void AddPlayerReturnsFalseIfPartyStateNotForming()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Full;
            var result = Controller.AddPlayer(party, players.ElementAt(1));
            Assert.IsFalse(result);
            Assert.AreEqual(1, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(1))));
        }

        [TestMethod]
        public void AddPlayerReturnsFalseIfPartyIsFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            party.State = PartyState.Forming;
            var result = Controller.AddPlayer(party, players.ElementAt(3));
            Assert.IsFalse(result);
            Assert.AreEqual(3, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(3))));
        }

        [TestMethod]
        public void AddPlayerSetsPartyStateToReadyIfPartyIsFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1));
            party.State = PartyState.Forming;
            var result = Controller.AddPlayer(party, players.ElementAt(2));
            Assert.IsTrue(result);
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.AreEqual(3, party.Members.Count());
            Assert.IsTrue(party.Members.Any(x => x.Equals(players.ElementAt(2))));
        }

        [TestMethod]
        public void RemovePlayerRemovesPlayerFromGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            party.State = PartyState.Full;
            var result = Controller.RemovePlayer(party, players.ElementAt(2));
            Assert.IsTrue(result);
            Assert.AreEqual(PartyState.Forming, party.State);
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(2))));
        }

        [TestMethod]
        public void RemovePlayerReturnsFalseIfPlayerNotInGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1));
            party.State = PartyState.Forming;
            var result = Controller.RemovePlayer(party, players.ElementAt(2));
            Assert.IsFalse(result);
            Assert.AreEqual(PartyState.Forming, party.State);
            Assert.AreEqual(2, party.Members.Count());
        }

        [TestMethod]
        public void RemovePlayerReturnsFalseIfPartyStateNotFullOrForming()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            party.State = PartyState.Started;
            var result = Controller.RemovePlayer(party, players.ElementAt(2));
            Assert.IsFalse(result);
            Assert.AreEqual(PartyState.Started, party.State);
            Assert.AreEqual(3, party.Members.Count());
            Assert.IsTrue(party.Members.Any(x => x.Equals(players.ElementAt(2))));
        }

        [TestMethod]
        public void RemovePlayerSetsPartyStateToFormingIfPartyCountWasMax()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            party.State = PartyState.Full;
            var result = Controller.RemovePlayer(party, players.ElementAt(2));
            Assert.IsTrue(result);
            Assert.AreEqual(PartyState.Forming, party.State);
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.ElementAt(2))));
        }

        [TestMethod]
        public void RemovePlayerDisbandsEmptyGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Forming;
            var result = Controller.RemovePlayer(party, players.First());
            Assert.IsTrue(result);
            Assert.AreEqual(PartyState.Disbanded, party.State);
            Assert.AreEqual(0, party.Members.Count());
            Assert.IsFalse(party.Members.Any(x => x.Equals(players.First())));
        }

        [TestMethod]
        public void SetReadySetsStateToFullIfForming()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Forming;
            var result = Controller.SetReady(party);
            Assert.IsTrue(result);
            Assert.AreEqual(PartyState.Full, party.State);
        }

        [TestMethod]
        public void SetReadyReturnsFalseIfPartyStateNotForming()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Started;
            var result = Controller.SetReady(party);
            Assert.IsFalse(result);
            Assert.AreEqual(PartyState.Started, party.State);
        }

        [TestMethod]
        public void SetReadyReturnsFalseIfPartyHasPendingInvites()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.PendingInvites.Add(players.ElementAt(1));
            party.State = PartyState.Forming;
            var result = Controller.SetReady(party);
            Assert.IsFalse(result);
            Assert.AreEqual(PartyState.Forming, party.State);
        }

        [TestMethod]
        public void UnsetReadyRemovesReadyState()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Full;
            var result = Controller.UnsetReady(party);
            Assert.IsTrue(result);
            Assert.AreEqual(PartyState.Forming, party.State);
        }

        [TestMethod]
        public void UnsetReadyReturnsFalseIfPartyStateNotFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First());
            party.State = PartyState.Started;
            var result = Controller.UnsetReady(party);
            Assert.IsFalse(result);
            Assert.AreEqual(PartyState.Started, party.State);
        }

        [TestMethod]
        public void UnsetReadyReturnsFalseIfPartyIsFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var players = db.PlayerCharacters.Read();
            var party = Controller.CreateParty(false, players.First(), players.ElementAt(1), players.ElementAt(2));
            party.State = PartyState.Full;
            var result = Controller.UnsetReady(party);
            Assert.IsFalse(result);
            Assert.AreEqual(PartyState.Full, party.State);
        }
    }
}
