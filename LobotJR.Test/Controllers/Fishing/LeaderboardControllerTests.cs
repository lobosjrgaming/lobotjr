using Autofac;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using static LobotJR.Command.Controller.Fishing.LeaderboardController;

namespace LobotJR.Test.Controllers.Fishing
{
    [TestClass]
    public class LeaderboardControllerTests
    {
        private IConnectionManager ConnectionManager;
        private FishingController FishingController;
        private TournamentController TournamentController;
        private LeaderboardController LeaderboardController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            FishingController = AutofacMockSetup.Container.Resolve<FishingController>();
            TournamentController = AutofacMockSetup.Container.Resolve<TournamentController>();
            LeaderboardController = AutofacMockSetup.Container.Resolve<LeaderboardController>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetFishingRecords();
        }

        [TestMethod]
        public void GetsLeaderboard()
        {
            var db = ConnectionManager.CurrentConnection;
            var leaderboard = db.FishingLeaderboard.Read();
            var retrieved = LeaderboardController.GetLeaderboard();
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(leaderboard.Count(), retrieved.Count());
            for (var i = 0; i < leaderboard.Count(); i++)
            {
                var lEntry = leaderboard.ElementAt(i);
                var rEntry = retrieved.ElementAt(i);
                Assert.IsTrue(lEntry.DeeplyEquals(rEntry));
            }
        }

        [TestMethod]
        public void DeletesFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var records = LeaderboardController.GetPersonalLeaderboard(user);
            var fish = records.First().FishId;
            LeaderboardController.DeleteFish(user, 0);
            db.Commit();
            Assert.IsFalse(records.Any(x => x.Fish.Id.Equals(fish)));
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnNegativeIndex()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var records = LeaderboardController.GetPersonalLeaderboard(user);
            var recordCount = records.Count();
            LeaderboardController.DeleteFish(user, -1);
            Assert.AreEqual(recordCount, records.Count());
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnIndexAboveCount()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var records = LeaderboardController.GetPersonalLeaderboard(user);
            var recordCount = records.Count();
            LeaderboardController.DeleteFish(user, recordCount);
            Assert.AreEqual(recordCount, records.Count());
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnMissingFisher()
        {
            var db = ConnectionManager.CurrentConnection;
            var recordCount = db.Catches.Read().Count();
            LeaderboardController.DeleteFish(new User("Invalid Id", "-1"), 0);
            Assert.AreEqual(recordCount, db.Catches.Read().Count());
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnNullFisher()
        {
            var db = ConnectionManager.CurrentConnection;
            var recordCount = db.Catches.Read().Count();
            LeaderboardController.DeleteFish(null, 0);
            Assert.AreEqual(recordCount, db.Catches.Read().Count());
        }

        [TestMethod]
        public void UpdatesPersonalLeaderboardWithNewFishType()
        {
            var db = ConnectionManager.CurrentConnection;
            var existing = db.Catches.Read().First();
            var user = db.Users.Read(x => x.TwitchId.Equals(existing.UserId)).First();
            var fish = existing.Fish;
            var records = db.Catches.Read(x => x.UserId.Equals(user));
            DataUtils.ClearFisherRecords(db, user);
            var catchData = new Catch()
            {
                Fish = fish,
                UserId = user.TwitchId,
                Weight = 100
            };
            var result = LeaderboardController.UpdatePersonalLeaderboard(user, catchData);
            db.Commit();
            var updatedRecords = db.Catches.Read(x => x.UserId.Equals(user.TwitchId));
            Assert.IsTrue(result);
            Assert.AreEqual(1, updatedRecords.Count());
            Assert.AreEqual(catchData.Fish.Id, updatedRecords.ElementAt(0).Fish.Id);
        }

        [TestMethod]
        public void UpdatesPersonalLeaderboardWithExistingFishType()
        {
            var db = ConnectionManager.CurrentConnection;
            var existing = db.Catches.Read().First();
            var user = db.Users.Read(x => x.TwitchId.Equals(existing.UserId)).First();
            var fish = existing.Fish;
            var catchData = new Catch()
            {
                Fish = fish,
                UserId = user.TwitchId,
                Weight = existing.Weight + 1
            };
            var result = LeaderboardController.UpdatePersonalLeaderboard(user, catchData);
            db.Commit();
            Assert.IsTrue(result);
            Assert.AreEqual(catchData.Weight, LeaderboardController.GetUserRecordForFish(user, fish).Weight);
        }

        [TestMethod]
        public void UpdatePersonalLeaderboardReturnsFalseWithNullFisher()
        {
            var result = LeaderboardController.UpdatePersonalLeaderboard(null, new Catch());
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UpdatePersonalLeaderboardReturnsFalseWithNullCatchData()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var result = LeaderboardController.UpdatePersonalLeaderboard(user, null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UpdatePersonalLeaderboardReturnsFalseWhenCatchIsNotNewRecord()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var records = LeaderboardController.GetPersonalLeaderboard(user);
            var recordCount = records.Count();
            var record = records.FirstOrDefault();
            var catchData = new Catch()
            {
                UserId = user.TwitchId,
                Fish = record.Fish,
                Weight = record.Weight - 0.01f
            };
            var result = LeaderboardController.UpdatePersonalLeaderboard(user, catchData);
            Assert.IsFalse(result);
            Assert.AreEqual(recordCount, LeaderboardController.GetPersonalLeaderboard(user).Count());
            Assert.AreNotEqual(catchData.Weight, LeaderboardController.GetUserRecordForFish(user, record.Fish).Weight);
        }

        [TestMethod]
        public void UpdatesGlobalLeaderboardWithNewFishType()
        {
            var db = ConnectionManager.CurrentConnection;
            var userId = db.Users.Read().First().TwitchId;
            var fish = db.FishData.Read().First();
            var entries = db.FishingLeaderboard.Read(x => x.Fish.Id == fish.Id).ToList();
            var entry = entries.First();
            db.FishingLeaderboard.Delete(entry);
            db.Commit();
            var initialCount = db.FishingLeaderboard.Read().Count();
            var catchData = new Catch()
            {
                Fish = fish,
                UserId = userId,
                Weight = 100
            };
            var result = LeaderboardController.UpdateGlobalLeaderboard(catchData);
            db.Commit();
            var leaderboard = LeaderboardController.GetLeaderboard();
            Assert.IsTrue(result);
            Assert.AreEqual(initialCount + 1, leaderboard.Count());
            Assert.AreEqual(catchData.Weight, leaderboard.First(x => x.Fish.Id == fish.Id).Weight);
        }

        [TestMethod]
        public void UpdatesGlobalLeaderboardWithExistingFishType()
        {
            var db = ConnectionManager.CurrentConnection;
            var fish = db.FishData.Read().First();
            var entry = db.FishingLeaderboard.Read(x => x.Fish.Id == fish.Id).First();
            var newUser = db.Users.Read(x => !x.TwitchId.Equals(entry.UserId)).First();
            var catchData = new Catch()
            {
                Fish = fish,
                UserId = newUser.TwitchId,
                Weight = entry.Weight + 1
            };
            var result = LeaderboardController.UpdateGlobalLeaderboard(catchData);
            var leaderboard = LeaderboardController.GetLeaderboard();
            Assert.IsTrue(result);
            Assert.AreEqual(catchData.Weight, leaderboard.First().Weight);
            Assert.AreEqual(newUser.TwitchId, leaderboard.First().UserId);
        }

        [TestMethod]
        public void UpdateGlobalLeaderboardReturnsFalseWithNullCatchData()
        {
            var result = LeaderboardController.UpdateGlobalLeaderboard(null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UpdateGlobalLeaderboardReturnsFalseWhenCatchIsNotNewRecord()
        {
            var db = ConnectionManager.CurrentConnection;
            var entry = db.FishingLeaderboard.Read().First();
            var catchData = new Catch()
            {
                Fish = entry.Fish,
                Weight = entry.Weight - 1
            };
            var result = LeaderboardController.UpdateGlobalLeaderboard(catchData);
            var leaderboard = LeaderboardController.GetLeaderboard();
            Assert.IsFalse(result);
            Assert.AreNotEqual(catchData.Weight, leaderboard.First().Weight);
        }

        [TestMethod]
        public void CatchFishUpdatesLeaderboardWhileTournamentActive()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            var callbackMock = new Mock<LeaderboardEventHandler>();
            LeaderboardController.NewGlobalRecord += callbackMock.Object;
            var leaderboard = db.FishingLeaderboard.Read().ToList();
            foreach (var entry in leaderboard)
            {
                db.FishingLeaderboard.Delete(entry);
            }
            db.Commit();
            TournamentController.StartTournament();
            fisher.Hooked = db.FishData.Read().First();
            var catchData = FishingController.CatchFish(fisher);
            db.Commit();
            Assert.IsNotNull(catchData);
            Assert.AreEqual(1, db.FishingLeaderboard.Read().Count());
            callbackMock.Verify(x => x(It.IsAny<LeaderboardEntry>()), Times.Once);
        }
    }
}

