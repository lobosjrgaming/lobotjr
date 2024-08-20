using LobotJR.Command;
using LobotJR.Command.View;
using LobotJR.Command.View.Fishing;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.Fishing
{
    /// <summary>
    /// Summary description for FishingTests
    /// </summary>
    [TestClass]
    public class FishingModuleTests
    {
        private SqliteRepositoryManager Manager;
        private FishingController FishingSystem;
        private TournamentController TournamentSystem;
        private LeaderboardController LeaderboardSystem;
        private FishingView FishingModule;

        [TestInitialize]
        public void Initialize()
        {
            Manager = new SqliteRepositoryManager(MockContext.Create());

            FishingSystem = new FishingSystem(Manager, Manager);
            LeaderboardSystem = new LeaderboardSystem(Manager);
            TournamentSystem = new TournamentSystem(FishingSystem, LeaderboardSystem, Manager);
            FishingModule = new FishingView(FishingSystem, TournamentSystem, LeaderboardSystem);
        }

        [TestMethod]
        public void ImportsFishData()
        {
            //Todo: This test is to make sure the flat file data imports correctly. That code is no longer necessary, but does still technically exist.
        }

        [TestMethod]
        public void PushesNotificationOnFishHooked()
        {
            var handlerMock = new Mock<PushNotificationHandler>();
            FishingModule.PushNotification += handlerMock.Object;
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            fisher.IsFishing = true;
            fisher.HookedTime = DateTime.Now;
            FishingSystem.Process(true);
            handlerMock.Verify(x => x(It.IsAny<User>(), It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.IsTrue(result.Responses.Any(x => x.Contains("!catch")));
        }

        [TestMethod]
        public void PushesNotificationOnFishGotAway()
        {
            var handlerMock = new Mock<PushNotificationHandler>();
            var appSettings = Manager.AppSettings.Read().First();
            FishingModule.PushNotification += handlerMock.Object;
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            fisher.IsFishing = true;
            fisher.Hooked = Manager.FishData.Read().First();
            fisher.HookedTime = DateTime.Now.AddSeconds(-appSettings.FishingHookLength);
            FishingSystem.Process(true);
            handlerMock.Verify(x => x(It.IsAny<User>(), It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.IsFalse(result.Responses.Any(x => x.Contains("!catch")));
        }

        [TestMethod]
        public void CancelsCast()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            fisher.IsFishing = true;
            var response = FishingModule.CancelCast(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsFalse(fisher.IsFishing);
        }

        [TestMethod]
        public void CancelCastFailsIfLineNotCast()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            fisher.IsFishing = false;
            var response = FishingModule.CancelCast(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsFalse(fisher.IsFishing);
        }

        [TestMethod]
        public void CatchesFish()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            TournamentSystem.StartTournament();
            DataUtils.ClearFisherRecords(Manager, user);
            fisher.IsFishing = true;
            fisher.HookedTime = DateTime.Now;
            fisher.Hooked = Manager.FishData.Read().First();
            var response = FishingModule.CatchFish(user);
            var responses = response.Responses;
            var newRecords = LeaderboardSystem.GetPersonalLeaderboard(user);
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(2, responses.Count);
            Assert.IsTrue(responses.Any(x => x.Contains("biggest")));
            Assert.IsTrue(responses.All(x => x.Contains(newRecords.First().Fish.Name)));
            Assert.IsFalse(fisher.IsFishing);
            Assert.AreEqual(1, newRecords.Count());
        }

        [TestMethod]
        public void CatchFishFailsIfLineNotCast()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            TournamentSystem.StartTournament();
            DataUtils.ClearFisherRecords(Manager, user);
            fisher.IsFishing = false;
            fisher.Hooked = null;
            var response = FishingModule.CatchFish(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("!cast"));
            Assert.IsFalse(fisher.IsFishing);
            Assert.AreEqual(0, LeaderboardSystem.GetPersonalLeaderboard(user).Count());
        }

        [TestMethod]
        public void CatchFishFailsIfNoFishBiting()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            TournamentSystem.StartTournament();
            DataUtils.ClearFisherRecords(Manager, user);
            fisher.IsFishing = true;
            fisher.HookedTime = DateTime.Now;
            fisher.Hooked = null;
            var response = FishingModule.CatchFish(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("!cancelcast"));
            Assert.IsFalse(fisher.IsFishing);
            Assert.AreEqual(0, LeaderboardSystem.GetPersonalLeaderboard(user).Count());
        }

        [TestMethod]
        public void CastsLine()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            fisher.IsFishing = false;
            var response = FishingModule.Cast(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(fisher.IsFishing);
        }

        [TestMethod]
        public void CastLineFailsFailsIfLineAlreadyCast()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            fisher.IsFishing = true;
            var response = FishingModule.Cast(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("already"));
            Assert.IsFalse(responses[0].Contains("!catch"));
        }

        [TestMethod]
        public void CastLineFailsIfFishIsBiting()
        {
            var user = Manager.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            fisher.IsFishing = true;
            fisher.Hooked = Manager.FishData.Read().First();
            var response = FishingModule.Cast(user);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("already"));
            Assert.IsTrue(responses[0].Contains("!catch"));
        }
    }
}
