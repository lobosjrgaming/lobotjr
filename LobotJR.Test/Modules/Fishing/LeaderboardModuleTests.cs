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

namespace LobotJR.Test.Modules.Fishing
{
    /// <summary>
    /// Summary description for FishingTests
    /// </summary>
    [TestClass]
    public class LeaderboardModuleTests
    {
        private IConnectionManager ConnectionManager;
        private UserController UserController;

        private FishingController FishingSystem;
        private TournamentController TournamentSystem;
        private LeaderboardController LeaderboardSystem;

        private FishingView FishingModule;
        private LeaderboardView LeaderboardModule;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
            FishingSystem = AutofacMockSetup.Container.Resolve<FishingController>();
            TournamentSystem = AutofacMockSetup.Container.Resolve<TournamentController>();
            LeaderboardSystem = AutofacMockSetup.Container.Resolve<LeaderboardController>();
            FishingModule = AutofacMockSetup.Container.Resolve<FishingView>();
            LeaderboardModule = AutofacMockSetup.Container.Resolve<LeaderboardView>();
        }

        [TestMethod]
        public void PushesNotificationOnNewGlobalRecord()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var handlerMock = new Mock<PushNotificationHandler>();
                LeaderboardModule.PushNotification += handlerMock.Object;
                var leaderboard = db.FishingLeaderboard.Read();
                foreach (var entry in leaderboard)
                {
                    entry.Weight = 0.1f;
                }
                TournamentSystem.StartTournament();
                var user = db.Users.Read().First();
                var userId = user.TwitchId;
                var userName = user.Username;
                var fisher = FishingSystem.GetFisherByUser(user);
                fisher.IsFishing = true;
                fisher.Hooked = db.FishData.Read().First();
                fisher.HookedTime = DateTime.Now;
                FishingModule.CatchFish(user);
                handlerMock.Verify(x => x(It.IsAny<User>(), It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.AreEqual(0, result.Responses.Count);
                Assert.IsTrue(result.Messages.Any(x => x.Contains(userName)));
            }
        }

        [TestMethod]
        public void RespondsWithPlayerLeaderboard()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var response = LeaderboardModule.PlayerLeaderboard(user);
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
        }

        [TestMethod]
        public void RespondsWithCompactPlayerLeaderboard()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var records = db.Catches.Read(x => x.UserId.Equals(user.TwitchId));
                var responses = LeaderboardModule.PlayerLeaderboardCompact(user);
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
        }

        [TestMethod]
        public void PlayerLeaderboardUserHasNoFishRecords()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var fishersWithRecords = db.Catches.Read().Select(x => x.UserId).Distinct();
                var users = db.Users.Read();
                var noRecordsFisherId = users.Where(x => !fishersWithRecords.Any(y => y.Equals(x.TwitchId))).FirstOrDefault();
                var response = LeaderboardModule.PlayerLeaderboard(noRecordsFisherId);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                var fishNames = db.FishData.Read().Select(x => x.Name);
                Assert.AreEqual(0, fishNames.Where(x => responses.Any(y => y.Contains(x))).Count());
            }
        }

        [TestMethod]
        public void PlayerLeaderboardProvidesSpecificFishDetails()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingSystem.GetFisherByUser(user);
                var fish = LeaderboardSystem.GetPersonalLeaderboard(user).FirstOrDefault();
                var response = LeaderboardModule.PlayerLeaderboard(user, 1);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.IsTrue(responses.Any(x => x.Contains(fish.Fish.Name)));
                Assert.IsTrue(responses.Any(x => x.Contains(fish.Length.ToString())));
                Assert.IsTrue(responses.Any(x => x.Contains(fish.Weight.ToString())));
                Assert.IsTrue(responses.Any(x => x.Contains(fish.Fish.SizeCategory.Name)));
                Assert.IsTrue(responses.Any(x => x.Contains(fish.Fish.FlavorText)));
            }
        }

        [TestMethod]
        public void RespondsWithGlobalLeaderboard()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var response = LeaderboardModule.GlobalLeaderboard();
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
        }

        [TestMethod]
        public void RespondsWithCompactGlobalLeaderboard()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var responses = LeaderboardModule.GlobalLeaderboardCompact();
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
        }

        [TestMethod]
        public void ReleasesSpecificFish()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingSystem.GetFisherByUser(user);
                var fish = LeaderboardSystem.GetPersonalLeaderboard(user).FirstOrDefault().Fish;
                var response = LeaderboardModule.ReleaseFish(user, 1);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains(fish.Name));
                Assert.IsFalse(LeaderboardSystem.GetPersonalLeaderboard(user).Any(x => x.Fish.Id.Equals(fish.Id)));
            }
        }

        [TestMethod]
        public void ReleaseFishWithInvalidIndexCausesError()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var response = LeaderboardModule.ReleaseFish(user, 0);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("doesn't exist", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public void ReleaseFishWithNoFishTellsPlayerToFish()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var usersWithFish = db.Catches.Read().Select(x => x.UserId).Distinct();
                var user = db.Users.Read(x => !usersWithFish.Contains(x.TwitchId)).First();
                var response = LeaderboardModule.ReleaseFish(user, 1);
                var responses = response.Responses;
                Assert.IsTrue(response.Processed);
                Assert.AreEqual(0, response.Errors.Count);
                Assert.AreEqual(1, responses.Count);
                Assert.IsTrue(responses[0].Contains("!cast"));
            }
        }
    }
}
