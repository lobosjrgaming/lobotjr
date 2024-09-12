using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Command.View;
using LobotJR.Command.View.Dungeons;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Test.Views.Dungeons
{
    [TestClass]
    public class DungeonViewTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private PartyController PartyController;
        private GroupFinderController GroupFinderController;
        private ConfirmationController ConfirmationController;
        private DungeonController Controller;
        private DungeonView View;
        private User User;
        private IEnumerable<User> OtherUsers;
        private PlayerCharacter Player;
        private IEnumerable<PlayerCharacter> OtherPlayers;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PartyController = AutofacMockSetup.Container.Resolve<PartyController>();
            GroupFinderController = AutofacMockSetup.Container.Resolve<GroupFinderController>();
            ConfirmationController = AutofacMockSetup.Container.Resolve<ConfirmationController>();
            Controller = AutofacMockSetup.Container.Resolve<DungeonController>();
            View = AutofacMockSetup.Container.Resolve<DungeonView>();
            User = ConnectionManager.CurrentConnection.Users.Read().First();
            OtherUsers = ConnectionManager.CurrentConnection.Users.Read().Skip(1).Take(3);
            Player = ConnectionManager.CurrentConnection.PlayerCharacters.Read(x => x.UserId.Equals(User.TwitchId)).First();
            OtherPlayers = OtherUsers.Select(x => ConnectionManager.CurrentConnection.PlayerCharacters.Read(y => y.UserId.Equals(x.TwitchId)).First());
            GroupFinderController.ResetQueue();
            PartyController.ResetGroups();
            var playClass = ConnectionManager.CurrentConnection.CharacterClassData.Read(x => x.CanPlay).First();
            Player.Level = 3;
            Player.Currency = 1000;
            Player.CharacterClass = playClass;
            foreach (var player in OtherPlayers)
            {
                player.Level = 3;
                player.Currency = 1000;
                player.CharacterClass = playClass;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetDungeons();
        }

        [TestMethod]
        public void DungeonDetailsReturnsDungeonData()
        {
            var dungeon = ConnectionManager.CurrentConnection.DungeonData.Read().First();
            var result = View.DungeonDetails(dungeon.Id.ToString());
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains(dungeon.Name));
            Assert.IsTrue(response.Contains(dungeon.LevelRanges.First(x => x.Mode.IsDefault).Minimum.ToString()));
            Assert.IsTrue(response.Contains(dungeon.LevelRanges.First(x => x.Mode.IsDefault).Maximum.ToString()));
            Assert.IsTrue(response.Contains(dungeon.Description));
        }

        [TestMethod]
        public void DungeonDetailsReturnsErrorOnMalformedDungeon()
        {
            var dungeon = ConnectionManager.CurrentConnection.DungeonData.Read().First();
            ConnectionManager.CurrentConnection.LevelRangeData.Delete();
            ConnectionManager.CurrentConnection.Commit();
            var result = View.DungeonDetails(dungeon.Id.ToString());
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains("missing level range data"));
        }

        [TestMethod]
        public void DungeonDetailsReturnsErrorOnInvalidDungeonId()
        {
            var result = View.DungeonDetails("invalidid");
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains("Invalid"));
        }

        [TestMethod]
        public void CreatePartyCreatesNewPartyForUser()
        {
            var result = View.CreateParty(User);
            var response = result.Responses.First();
            var party = PartyController.GetCurrentGroup(Player);
            Assert.IsNotNull(party);
            Assert.IsTrue(response.Contains("!add"));
        }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserAlreadyInParty()
        {
            PartyController.CreateParty(false, Player);
            var result = View.CreateParty(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains("already have a party"));
        }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserIsPendingInvite()
        {
            var party = PartyController.CreateParty(false, OtherPlayers.First());
            PartyController.InvitePlayer(party, Player);
            var result = View.CreateParty(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains("outstanding invite"));
        }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserIsTooLowLevel()
        {
            Player.Level = 0;
            var result = View.CreateParty(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"level {PlayerController.MinLevel} or higher"));
        }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserHasNotPickedAClass()
        {
            Player.CharacterClass.CanPlay = false;
            var result = View.CreateParty(User);
            Player.CharacterClass.CanPlay = true;
            var response = result.Responses.First();
            Assert.AreEqual(2, result.Responses.Count);
            Assert.IsTrue(response.Contains("haven't picked"));
        }

        [TestMethod]
        public void CreatePartyReturnsErrorIfUserIsInGroupFinderQueue()
        {
            GroupFinderController.QueuePlayer(Player, Array.Empty<DungeonRun>());
            var result = View.CreateParty(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains("queued"));
        }

        [TestMethod]
        public void AddPlayerInvitesPlayerToParty()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.Last());
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var result = View.AddPlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.IsTrue(party.PendingInvites.Contains(otherPlayer.UserId));
            Assert.IsTrue(response.Contains($"invited {otherUser.Username}"));
            listener.Verify(x => x(otherUser, It.IsAny<CommandResult>()), Times.Once());
            listener.Verify(x => x(OtherUsers.Last(), It.IsAny<CommandResult>()), Times.Once());
        }

        [TestMethod]
        public void AddPlayerCreatesNewPartyIfUserIsNotInOne()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var result = View.AddPlayer(User, otherUser.Username);
            var party = PartyController.GetCurrentGroup(Player);
            var response = result.Responses.First();
            Assert.IsTrue(party.PendingInvites.Contains(otherPlayer.UserId));
            Assert.IsTrue(response.Contains($"invited {otherUser.Username}"));
            listener.Verify(x => x(otherUser, It.IsAny<CommandResult>()));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPartyIsFull()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1), OtherPlayers.ElementAt(2));
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var result = View.AddPlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(3, party.Members.Count() + party.PendingInvites.Count());
            Assert.IsTrue(response.Contains($"more than {SettingsManager.GetGameSettings().DungeonPartySize}"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPartyIsNotInValidState()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1));
            party.State = PartyState.Started;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var result = View.AddPlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.IsTrue(response.Contains("unable to add members"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfUserIsNotPartyLeader()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1));
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var result = View.AddPlayer(OtherUsers.ElementAt(1), otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.IsTrue(response.Contains($"party leader ({User.Username})"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerIsInAParty()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1));
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            PartyController.CreateParty(false, otherPlayer);
            var result = View.AddPlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.IsTrue(response.Contains($"{otherUser.Username} is already in a group"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerHasNotSelectedClass()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1));
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            otherPlayer.CharacterClass = ConnectionManager.CurrentConnection.CharacterClassData.Read(x => !x.CanPlay).First();
            var result = View.AddPlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.IsTrue(response.Contains(otherUser.Username) && response.Contains("not picked a class"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerIsLowLevel()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1));
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            otherPlayer.Level = 0;
            var result = View.AddPlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.IsTrue(response.Contains(otherUser.Username) && response.Contains("not high enough level"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfPlayerIsUser()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1), OtherPlayers.ElementAt(2));
            var result = View.AddPlayer(User, User.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.IsTrue(response.Contains("invite"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorOnPlayerNotFound()
        {
            PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1), OtherPlayers.ElementAt(2));
            var username = "InvalidUserName";
            var result = View.AddPlayer(User, username);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains(username) && response.Contains("Can't find"));
        }

        [TestMethod]
        public void AddPlayerReturnsErrorIfUserInGroupFinderQueue()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1));
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            GroupFinderController.QueuePlayer(otherPlayer, Array.Empty<DungeonRun>());
            var result = View.AddPlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, party.PendingInvites.Count());
            Assert.IsTrue(response.Contains(otherUser.Username) && response.Contains("queue"));
        }

        [TestMethod]
        public void RemovePlayerRemovesPlayerFromParty()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer, OtherPlayers.ElementAt(1));
            var result = View.RemovePlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"{otherUser.Username} was removed"));
            listener.Verify(x => x(otherUser, It.IsAny<CommandResult>()), Times.Once());
            listener.Verify(x => x(OtherUsers.ElementAt(1), It.IsAny<CommandResult>()), Times.Once());
        }

        [TestMethod]
        public void RemovePlayerDisbandsPartyWithOneUserLeft()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            var result = View.RemovePlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(0, PartyController.PartyCount);
            Assert.IsTrue(response.Contains($"disbanded"));
            listener.Verify(x => x(otherUser, It.IsAny<CommandResult>()), Times.Once());
        }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfPlayerNotInGroup()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, OtherPlayers.ElementAt(1));
            var result = View.RemovePlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"Couldn't find"));
        }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfPlayerIsUser()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            var result = View.RemovePlayer(User, User.Username);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"kick yourself"));
        }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfPartyIsInDungeon()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Started;
            var result = View.RemovePlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"middle of a dungeon"));
        }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfUserIsNotPartyLeader()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            var result = View.RemovePlayer(otherUser, User.Username);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"party leader"));
        }

        [TestMethod]
        public void RemovePlayerReturnsErrorIfUserIsNotInParty()
        {
            var result = View.RemovePlayer(User, OtherUsers.First().Username);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"not in a party"));
        }

        [TestMethod]
        public void PromotePlayerSetsNewPartyLeader()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Started;
            var result = View.PromotePlayer(User, otherUser.Username);
            var response = result.Responses.First();
            Assert.AreEqual(otherPlayer.UserId, party.Leader);
            Assert.IsTrue(response.Contains($"promoted {otherUser.Username}"));
            listener.Verify(x => x(OtherUsers.First(), It.IsAny<CommandResult>()), Times.Once());
        }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfPlayerNotInGroup()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Started;
            var result = View.PromotePlayer(User, OtherUsers.ElementAt(1).Username);
            var response = result.Responses.First();
            Assert.AreEqual(Player.UserId, party.Leader);
            Assert.IsTrue(response.Contains($"'{OtherUsers.ElementAt(1).Username}' not found"));
        }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfPlayerIsLeader()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Started;
            var result = View.PromotePlayer(User, User.Username);
            var response = result.Responses.First();
            Assert.AreEqual(Player.UserId, party.Leader);
            Assert.IsTrue(response.Contains($"already the party leader"));
        }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfUserIsNotPartyLeader()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            var result = View.PromotePlayer(otherUser, User.Username);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"party leader"));
        }

        [TestMethod]
        public void PromotePlayerReturnsErrorIfUserIsNotInParty()
        {
            var result = View.PromotePlayer(User, OtherUsers.First().Username);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"not in a party"));
        }

        [TestMethod]
        public void LeavePartyRemovesUserFromParty()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer, OtherPlayers.ElementAt(1));
            var result = View.LeaveParty(User);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"You left"));
            listener.Verify(x => x(otherUser, It.IsAny<CommandResult>()), Times.Once());
            listener.Verify(x => x(OtherUsers.ElementAt(1), It.IsAny<CommandResult>()), Times.Once());
        }

        [TestMethod]
        public void LeavePartyPromotesNewLeaderIfUserWasLeader()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer, OtherPlayers.ElementAt(1));
            var result = View.LeaveParty(User);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsNotNull(party.Leader);
            Assert.IsTrue(response.Contains($"You left"));
            listener.Verify(x => x(otherUser, It.IsAny<CommandResult>()), Times.Once());
            listener.Verify(x => x(OtherUsers.ElementAt(1), It.IsAny<CommandResult>()), Times.Once());
        }

        [TestMethod]
        public void LeavePartyReturnsErrorIfPartyIsInDungeon()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer, OtherPlayers.ElementAt(1));
            party.State = PartyState.Started;
            var result = View.LeaveParty(User);
            var response = result.Responses.First();
            Assert.AreEqual(3, party.Members.Count());
            Assert.IsTrue(response.Contains($"can't leave"));
        }

        [TestMethod]
        public void LeavePartyReturnsErrorIfUserIsNotInParty()
        {
            var result = View.LeaveParty(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"not in a party"));
        }

        [TestMethod]
        public void PartyChatSendsMessageToParty()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer, OtherPlayers.ElementAt(1));
            var message = "Test message";
            var result = View.PartyChat(User, message);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains(message));
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains(message))), Times.Once());
            listener.Verify(x => x(OtherUsers.ElementAt(1), It.Is<CommandResult>(y => y.Responses.First().Contains(message))), Times.Once());
        }

        [TestMethod]
        public void PartyChatReturnsErrorIfUserIsNotInParty()
        {
            var result = View.PartyChat(User, "Test message");
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"not in a party"));
        }

        [TestMethod]
        public void SetReadySetsPartyStateToFull()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            var result = View.SetReady(User);
            var response = result.Responses.First();
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.IsTrue(response.Contains("'Ready'"));
        }

        [TestMethod]
        public void SetReadyReturnsErrorIfPartyHasPendingInvites()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.PendingInvites.Add(OtherPlayers.ElementAt(1).UserId);
            var result = View.SetReady(User);
            var response = result.Responses.First();
            Assert.AreEqual(PartyState.Forming, party.State);
            Assert.IsTrue(response.Contains("invitation"));
        }

        [TestMethod]
        public void SetReadyReturnsErrorIfPartyIsInDungeon()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Started;
            var result = View.SetReady(User);
            var response = result.Responses.First();
            Assert.AreEqual(PartyState.Started, party.State);
            Assert.IsTrue(response.Contains("can't be readied"));
        }

        [TestMethod]
        public void SetReadyReturnsErrorIfUserIsNotInParty()
        {
            var result = View.SetReady(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"not in a party"));
        }

        [TestMethod]
        public void UnsetReadySetsPartyStateToForming()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            var result = View.UnsetReady(User);
            var response = result.Responses.First();
            Assert.AreEqual(PartyState.Forming, party.State);
            Assert.IsTrue(response.Contains("'Ready'"));
        }

        [TestMethod]
        public void UnsetReadyReturnsErrorIfPartyIsInDungeon()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Started;
            var result = View.UnsetReady(User);
            var response = result.Responses.First();
            Assert.AreEqual(PartyState.Started, party.State);
            Assert.IsTrue(response.Contains("can't be changed"));
        }

        [TestMethod]
        public void UnsetReadyReturnsErrorIfUserIsNotPartyLeader()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            var result = View.UnsetReady(otherUser);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"party leader"));
        }

        [TestMethod]
        public void UnsetReadyReturnsErrorIfUserIsNotInParty()
        {
            var result = View.UnsetReady(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"not in a party"));
        }

        [TestMethod]
        public void StartDungeonSendsPartyIntoDungeon()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            var result = View.StartDungeon(User, "1");
            Assert.AreEqual(PartyState.Started, party.State);
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains("initiated"))), Times.Once());
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains(otherUser.Username))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains("initiated"))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains(User.Username))), Times.Once());
        }

        [TestMethod]
        public void StartDungeonSelectsRandomEligibleDungeonIfNoIdProvided()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            var result = View.StartDungeon(User, "1");
            Assert.IsNotNull(party.DungeonId);
            Assert.IsNotNull(party.ModeId);
            Assert.AreEqual(PartyState.Started, party.State);
        }

        [TestMethod]
        public void StartDungeonReturnsErrorIfAnyPlayerCannotAfford()
        {
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            Player.Currency = 0;
            var result = View.StartDungeon(User, "1");
            Assert.AreEqual(PartyState.Full, party.State);
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains("not have enough"))), Times.Once());
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains(User.Username))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains("not have enough"))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains(User.Username))), Times.Once());
        }

        [TestMethod]
        public void StartDungeonReturnsErrorOnInvalidDungeonId()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            var result = View.StartDungeon(User, "Invalid");
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.IsTrue(result.Responses.First().Contains("Invalid"));
        }

        [TestMethod]
        public void StartDungeonReturnsErrorIfUserIsNotPartyLeader()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            var result = View.StartDungeon(otherUser);
            var response = result.Responses.First();
            Assert.AreEqual(2, party.Members.Count());
            Assert.IsTrue(response.Contains($"party leader"));
        }

        [TestMethod]
        public void StartDungeonReturnsErrorIfUserIsNotInParty()
        {
            var result = View.StartDungeon(User);
            var response = result.Responses.First();
            Assert.IsTrue(response.Contains($"not in a party"));
        }

        [TestMethod]
        public void DungeonProgressEventSendsProgressUpdateMessage()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            View.StartDungeon(User);
            party.State = PartyState.Started;
            party.LastUpdate = DateTime.MinValue;
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            Controller.Process();
            var dungeon = Controller.GetDungeonById(party.DungeonId);
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains(dungeon.Introduction))));
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains(dungeon.Introduction))));
        }

        [TestMethod]
        public void DungeonFailureEventSendsFailureMessage()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            View.StartDungeon(User);
            party.State = PartyState.Failed;
            party.LastUpdate = DateTime.MinValue;
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            Controller.Process();
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains("No XP"))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains("No XP"))), Times.Once());
        }

        [TestMethod]
        public void DungeonCompleteEventSendsCompleteMessageWithAwards()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            View.StartDungeon(User);
            party.State = PartyState.Complete;
            party.LastUpdate = DateTime.MinValue;
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            Controller.Process();
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains("you've earned")))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains("you've earned")))), Times.Once());
        }

        [TestMethod]
        public void PlayerDeathEventSendsDeathNotificationWithPenalties()
        {
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var party = PartyController.CreateParty(false, Player, otherPlayer);
            party.State = PartyState.Full;
            View.StartDungeon(User);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var old = SettingsManager.GetGameSettings().DungeonDeathChance;
            SettingsManager.GetGameSettings().DungeonDeathChance = 1;
            party.State = PartyState.Failed;
            party.LastUpdate = DateTime.MinValue;
            Controller.Process();
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains("you have died"))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains("you have died"))), Times.Once());
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains("No XP"))), Times.Once());
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains("No XP"))), Times.Once());
            SettingsManager.GetGameSettings().DungeonDeathChance = old;
        }

        [TestMethod]
        public void ConfirmEventAcceptsPartyInvite()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.Last());
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            party.PendingInvites.Add(otherPlayer.UserId);
            var result = View.AddPlayer(User, otherUser.Username);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            ConfirmationController.Confirm(otherUser);
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains("joined a party"))), Times.Once());
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains("joined your party"))), Times.Once());
        }

        [TestMethod]
        public void CanceEventDeclinesPartyInvite()
        {
            var party = PartyController.CreateParty(false, Player, OtherPlayers.Last());
            var otherUser = OtherUsers.First();
            var otherPlayer = OtherPlayers.First(x => x.UserId.Equals(otherUser.TwitchId));
            var result = View.AddPlayer(User, otherUser.Username);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            ConfirmationController.Cancel(otherUser);
            listener.Verify(x => x(otherUser, It.Is<CommandResult>(y => y.Responses.First().Contains("You declined"))), Times.Once());
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.First().Contains("has declined"))), Times.Once());
        }
    }
}
