﻿using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.View;
using LobotJR.Command.View.Fishing;
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
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private FishingController FishingController;
        private TournamentController TournamentController;
        private LeaderboardController LeaderboardController;
        private FishingView FishingView;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            FishingController = AutofacMockSetup.Container.Resolve<FishingController>();
            LeaderboardController = AutofacMockSetup.Container.Resolve<LeaderboardController>();
            TournamentController = AutofacMockSetup.Container.Resolve<TournamentController>();
            FishingView = AutofacMockSetup.Container.Resolve<FishingView>();
        }

        [TestMethod]
        public void ImportsFishData()
        {
            //Todo: This test is to make sure the flat file data imports correctly. That code is no longer necessary, but does still technically exist.
        }

        [TestMethod]
        public void PushesNotificationOnFishHooked()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var handlerMock = new Mock<PushNotificationHandler>();
                FishingView.PushNotification += handlerMock.Object;
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                fisher.IsFishing = true;
                fisher.HookedTime = DateTime.Now;
                FishingController.Process();
                handlerMock.Verify(x => x(It.IsAny<User>(), It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.IsTrue(result.Responses.Any(x => x.Contains("!catch")));
            }
        }

        [TestMethod]
        public void PushesNotificationOnFishGotAway()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var handlerMock = new Mock<PushNotificationHandler>();
                var settings = SettingsManager.GetGameSettings();
                FishingView.PushNotification += handlerMock.Object;
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                fisher.IsFishing = true;
                fisher.Hooked = db.FishData.Read().First();
                fisher.HookedTime = DateTime.Now.AddSeconds(-settings.FishingHookLength);
                FishingController.Process();
                handlerMock.Verify(x => x(It.IsAny<User>(), It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.IsFalse(result.Responses.Any(x => x.Contains("!catch")));
            }
        }

        [TestMethod]
        public void CancelsCast()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                fisher.IsFishing = true;
                var response = FishingView.CancelCast(user);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsFalse(fisher.IsFishing);
            }
        }

        [TestMethod]
        public void CancelCastFailsIfLineNotCast()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                fisher.IsFishing = false;
                var response = FishingView.CancelCast(user);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsFalse(fisher.IsFishing);
            }
        }

        [TestMethod]
        public void CatchesFish()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                TournamentController.StartTournament();
                DataUtils.ClearFisherRecords(db, user);
                fisher.IsFishing = true;
                fisher.HookedTime = DateTime.Now;
                fisher.Hooked = db.FishData.Read().First();
                var response = FishingView.CatchFish(user);
                var responses = response.Responses;
                var newRecords = LeaderboardController.GetPersonalLeaderboard(user);
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(2, responses.Count);
                Assert.IsTrue(responses.Any(x => x.Contains("biggest")));
                Assert.IsTrue(responses.All(x => x.Contains(newRecords.First().Fish.Name)));
                Assert.IsFalse(fisher.IsFishing);
                Assert.AreEqual(1, newRecords.Count());
            }
        }

        [TestMethod]
        public void CatchFishFailsIfLineNotCast()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                TournamentController.StartTournament();
                DataUtils.ClearFisherRecords(db, user);
                fisher.IsFishing = false;
                fisher.Hooked = null;
                var response = FishingView.CatchFish(user);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("!cast"));
                Assert.IsFalse(fisher.IsFishing);
                Assert.AreEqual(0, LeaderboardController.GetPersonalLeaderboard(user).Count());
            }
        }

        [TestMethod]
        public void CatchFishFailsIfNoFishBiting()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                TournamentController.StartTournament();
                DataUtils.ClearFisherRecords(db, user);
                fisher.IsFishing = true;
                fisher.HookedTime = DateTime.Now;
                fisher.Hooked = null;
                var response = FishingView.CatchFish(user);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("!cancelcast"));
                Assert.IsFalse(fisher.IsFishing);
                Assert.AreEqual(0, LeaderboardController.GetPersonalLeaderboard(user).Count());
            }
        }

        [TestMethod]
        public void CastsLine()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                fisher.IsFishing = false;
                var response = FishingView.Cast(user);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(fisher.IsFishing);
            }
        }

        [TestMethod]
        public void CastLineFailsFailsIfLineAlreadyCast()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                fisher.IsFishing = true;
                var response = FishingView.Cast(user);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("already"));
                Assert.IsFalse(responses[0].Contains("!catch"));
            }
        }

        [TestMethod]
        public void CastLineFailsIfFishIsBiting()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingController.GetFisherByUser(user);
                fisher.IsFishing = true;
                fisher.Hooked = db.FishData.Read().First();
                var response = FishingView.Cast(user);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("already"));
                Assert.IsTrue(responses[0].Contains("!catch"));
            }
        }
    }
}
