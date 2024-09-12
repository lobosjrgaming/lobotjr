using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.View;
using LobotJR.Command.View.Fishing;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LobotJR.Test.Views.Fishing
{
    /// <summary>
    /// Summary description for FishingTests
    /// </summary>
    [TestClass]
    public class LeaderboardViewTests
    {
        private IConnectionManager ConnectionManager;
        private UserController UserController;

        private FishingController FishingController;
        private TournamentController TournamentController;
        private LeaderboardController LeaderboardController;

        private FishingView FishingView;
        private LeaderboardView LeaderboardView;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
            FishingController = AutofacMockSetup.Container.Resolve<FishingController>();
            TournamentController = AutofacMockSetup.Container.Resolve<TournamentController>();
            LeaderboardController = AutofacMockSetup.Container.Resolve<LeaderboardController>();
            FishingView = AutofacMockSetup.Container.Resolve<FishingView>();
            LeaderboardView = AutofacMockSetup.Container.Resolve<LeaderboardView>();
            AutofacMockSetup.ResetFishingRecords();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetFishingRecords();
        }

        [TestMethod]
        public void PushesNotificationOnNewGlobalRecord()
        {
            var db = ConnectionManager.CurrentConnection;
            var leaderboard = db.FishingLeaderboard.Read();
            var handlerMock = new Mock<PushNotificationHandler>();
            LeaderboardView.PushNotification += handlerMock.Object;
            foreach (var entry in leaderboard)
            {
                entry.Weight = 0.1f;
            }
            TournamentController.StartTournament();
            var user = db.Users.Read().First();
            var userId = user.TwitchId;
            var userName = user.Username;
            var fisher = FishingController.GetFisherByUser(user);
            fisher.IsFishing = true;
            fisher.Hooked = db.FishData.Read().First();
            fisher.HookedTime = DateTime.Now;
            FishingView.CatchFish(user);
            handlerMock.Verify(x => x(It.IsAny<User>(), It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.AreEqual(0, result.Responses.Count);
            Assert.IsTrue(result.Messages.Any(x => x.Contains(userName)));

        }

        [TestMethod]
        public void RespondsWithPlayerLeaderboard()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var response = LeaderboardView.PlayerLeaderboard(user);
            var responses = response.Responses;
            var fishData = db.FishData.Read();
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(fishData.Count() + 1, responses.Count);
            foreach (var fish in fishData)
            {
                Assert.IsTrue(responses.Any(x => x.Contains(fish.Name)));
            }
        }

        [TestMethod]
        public void RespondsWithCompactPlayerLeaderboard()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fishes = db.FishData.Read().ToList();
            var records = db.Catches.Read(x => x.UserId.Equals(user.TwitchId)).ToList();
            var responses = LeaderboardView.PlayerLeaderboardCompact(user);
            var items = responses.Items.ToList();
            Assert.AreEqual(3, responses.Items.Count());
            var compact = responses.ToCompact();
            foreach (var fish in records)
            {
                Assert.IsTrue(
                    compact.Any(
                        x => x.Contains(fish.Fish.Name)
                        && x.Contains(fish.Length.ToString())
                        && x.Contains(fish.Weight.ToString())
                    )
                );
            }
        }

        [TestMethod]
        public void PlayerLeaderboardUserHasNoFishRecords()
        {
            var db = ConnectionManager.CurrentConnection;
            var fishersWithRecords = db.Catches.Read().Select(x => x.UserId).Distinct();
            var users = db.Users.Read();
            var noRecordsFisherId = users.Where(x => !fishersWithRecords.Any(y => y.Equals(x.TwitchId))).FirstOrDefault();
            var response = LeaderboardView.PlayerLeaderboard(noRecordsFisherId);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            var fishNames = db.FishData.Read().Select(x => x.Name);
            Assert.AreEqual(0, fishNames.Where(x => responses.Any(y => y.Contains(x))).Count());
        }

        [TestMethod]
        public void PlayerLeaderboardProvidesSpecificFishDetails()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            var fish = LeaderboardController.GetPersonalLeaderboard(user).FirstOrDefault();
            var response = LeaderboardView.PlayerLeaderboard(user, 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.IsTrue(responses.Any(x => x.Contains(fish.Fish.Name)));
            Assert.IsTrue(responses.Any(x => x.Contains(fish.Length.ToString())));
            Assert.IsTrue(responses.Any(x => x.Contains(fish.Weight.ToString())));
            Assert.IsTrue(responses.Any(x => x.Contains(fish.Fish.SizeCategory.Name)));
            Assert.IsTrue(responses.Any(x => x.Contains(fish.Fish.FlavorText)));
        }

        [TestMethod]
        public void RespondsWithGlobalLeaderboard()
        {
            var db = ConnectionManager.CurrentConnection;
            var response = LeaderboardView.GlobalLeaderboard();
            var responses = response.Responses;
            var leaderboard = db.FishingLeaderboard.Read();

            foreach (var entry in leaderboard)
            {
                var user = UserController.GetUserById(entry.UserId);
                Assert.IsTrue(
                    responses.Any(
                        x => x.Contains(entry.Fish.Name)
                        && x.Contains(entry.Length.ToString())
                        && x.Contains(entry.Weight.ToString())
                        && x.Contains(user.Username)
                    )
                );
            }
        }

        [TestMethod]
        public void RespondsWithCompactGlobalLeaderboard()
        {
            var db = ConnectionManager.CurrentConnection;
            var responses = LeaderboardView.GlobalLeaderboardCompact();
            var compact = responses.ToCompact();
            var leaderboard = db.FishingLeaderboard.Read();
            foreach (var entry in leaderboard)
            {
                var user = UserController.GetUserById(entry.UserId);
                Assert.IsTrue(
                    compact.Any(
                        x => x.Contains(entry.Fish.Name)
                        && x.Contains(entry.Length.ToString())
                        && x.Contains(entry.Weight.ToString())
                        && x.Contains(user.Username)
                    )
                );
            }
        }

        [TestMethod]
        public void ReleasesSpecificFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            var record = LeaderboardController.GetPersonalLeaderboard(user).FirstOrDefault();
            var fish = record.Fish;
            var response = LeaderboardView.ReleaseFish(user, 1);
            db.Commit();
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains(fish.Name));
            Assert.IsFalse(LeaderboardController.GetPersonalLeaderboard(user).Any(x => x.Fish.Id.Equals(fish.Id)));
        }

        [TestMethod]
        public void ReleaseFishWithInvalidIndexCausesError()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var response = LeaderboardView.ReleaseFish(user, 0);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("doesn't exist", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void ReleaseFishWithNoFishTellsPlayerToFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var usersWithFish = db.Catches.Read().Select(x => x.UserId).Distinct();
            var user = db.Users.Read(x => !usersWithFish.Contains(x.TwitchId)).First();
            var response = LeaderboardView.ReleaseFish(user, 1);
            var responses = response.Responses;
            Assert.IsTrue(response.Processed);
            Assert.AreEqual(0, response.Errors.Count);
            Assert.AreEqual(1, responses.Count);
            Assert.IsTrue(responses[0].Contains("!cast"));
        }
    }
}
