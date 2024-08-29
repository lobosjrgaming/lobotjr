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

namespace LobotJR.Test.Systems.Fishing
{
    [TestClass]
    public class LeaderboardSystemTests
    {
        private IConnectionManager ConnectionManager;
        private FishingController FishingSystem;
        private TournamentController TournamentSystem;
        private LeaderboardController LeaderboardSystem;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            FishingSystem = AutofacMockSetup.Container.Resolve<FishingController>();
            TournamentSystem = AutofacMockSetup.Container.Resolve<TournamentController>();
            LeaderboardSystem = AutofacMockSetup.Container.Resolve<LeaderboardController>();
        }

        [TestMethod]
        public void GetsLeaderboard()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var leaderboard = db.FishingLeaderboard.Read();
                var retrieved = LeaderboardSystem.GetLeaderboard();
                Assert.IsNotNull(retrieved);
                Assert.AreEqual(leaderboard.Count(), retrieved.Count());
                for (var i = 0; i < leaderboard.Count(); i++)
                {
                    var lEntry = leaderboard.ElementAt(i);
                    var rEntry = retrieved.ElementAt(i);
                    Assert.IsTrue(lEntry.DeeplyEquals(rEntry));
                }
            }
        }

        [TestMethod]
        public void DeletesFish()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var records = LeaderboardSystem.GetPersonalLeaderboard(user);
                var fish = records.First().FishId;
                LeaderboardSystem.DeleteFish(user, 0);
                Assert.IsFalse(records.Any(x => x.Fish.Id.Equals(fish)));
            }
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnNegativeIndex()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var records = LeaderboardSystem.GetPersonalLeaderboard(user);
                var recordCount = records.Count();
                LeaderboardSystem.DeleteFish(user, -1);
                Assert.AreEqual(recordCount, records.Count());
            }
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnIndexAboveCount()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var records = LeaderboardSystem.GetPersonalLeaderboard(user);
                var recordCount = records.Count();
                LeaderboardSystem.DeleteFish(user, recordCount);
                Assert.AreEqual(recordCount, records.Count());
            }
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnMissingFisher()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var recordCount = db.Catches.Read().Count();
                LeaderboardSystem.DeleteFish(new User("Invalid Id", "-1"), 0);
                Assert.AreEqual(recordCount, db.Catches.Read().Count());
            }
        }

        [TestMethod]
        public void DeleteFishDoesNothingOnNullFisher()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var recordCount = db.Catches.Read().Count();
                LeaderboardSystem.DeleteFish(null, 0);
                Assert.AreEqual(recordCount, db.Catches.Read().Count());
            }
        }

        [TestMethod]
        public void UpdatesPersonalLeaderboardWithNewFishType()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var records = db.Catches.Read(x => x.UserId.Equals(user));
                DataUtils.ClearFisherRecords(db, user);
                var catchData = new Catch()
                {
                    Fish = db.FishData.Read().First(),
                    UserId = user.TwitchId,
                    Weight = 100
                };
                var result = LeaderboardSystem.UpdatePersonalLeaderboard(user, catchData);
                var updatedRecords = db.Catches.Read(x => x.UserId.Equals(user.TwitchId));
                Assert.IsTrue(result);
                Assert.AreEqual(1, updatedRecords.Count());
                Assert.AreEqual(catchData.Fish.Id, updatedRecords.ElementAt(0).Fish.Id);
            }
        }

        [TestMethod]
        public void UpdatesPersonalLeaderboardWithExistingFishType()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fish = db.FishData.Read().First();
                var existing = LeaderboardSystem.GetUserRecordForFish(user, fish);
                var catchData = new Catch()
                {
                    Fish = fish,
                    UserId = user.TwitchId,
                    Weight = existing.Weight + 1
                };
                var result = LeaderboardSystem.UpdatePersonalLeaderboard(user, catchData);
                Assert.IsTrue(result);
                Assert.AreEqual(catchData.Weight, LeaderboardSystem.GetUserRecordForFish(user, fish).Weight);
            }
        }

        [TestMethod]
        public void UpdatePersonalLeaderboardReturnsFalseWithNullFisher()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var result = LeaderboardSystem.UpdatePersonalLeaderboard(null, new Catch());
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void UpdatePersonalLeaderboardReturnsFalseWithNullCatchData()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var result = LeaderboardSystem.UpdatePersonalLeaderboard(user, null);
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void UpdatePersonalLeaderboardReturnsFalseWhenCatchIsNotNewRecord()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var records = LeaderboardSystem.GetPersonalLeaderboard(user);
                var recordCount = records.Count();
                var record = records.FirstOrDefault();
                var catchData = new Catch()
                {
                    UserId = user.TwitchId,
                    Fish = record.Fish,
                    Weight = record.Weight - 0.01f
                };
                var result = LeaderboardSystem.UpdatePersonalLeaderboard(user, catchData);
                Assert.IsFalse(result);
                Assert.AreEqual(recordCount, LeaderboardSystem.GetPersonalLeaderboard(user).Count());
                Assert.AreNotEqual(catchData.Weight, LeaderboardSystem.GetUserRecordForFish(user, record.Fish).Weight);
            }
        }

        [TestMethod]
        public void UpdatesGlobalLeaderboardWithNewFishType()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var userId = db.Users.Read().First().TwitchId;
                var fish = db.FishData.Read().First();
                var entry = db.FishingLeaderboard.Read(x => x.Fish.Id == fish.Id).First();
                db.FishingLeaderboard.Delete(entry);
                db.FishingLeaderboard.Commit();
                var initialCount = db.FishingLeaderboard.Read().Count();
                var catchData = new Catch()
                {
                    Fish = db.FishData.Read().First(),
                    UserId = userId,
                    Weight = 100
                };
                var result = LeaderboardSystem.UpdateGlobalLeaderboard(catchData);
                var leaderboard = LeaderboardSystem.GetLeaderboard();
                Assert.IsTrue(result);
                Assert.AreEqual(initialCount + 1, leaderboard.Count());
                Assert.AreEqual(catchData.Weight, leaderboard.First(x => x.Fish.Id == fish.Id).Weight);
            }
        }

        [TestMethod]
        public void UpdatesGlobalLeaderboardWithExistingFishType()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var fish = db.FishData.Read().First();
                var entry = db.FishingLeaderboard.Read(x => x.Fish.Id == fish.Id).First();
                var newUser = db.Users.Read(x => !x.TwitchId.Equals(entry.UserId)).First();
                var catchData = new Catch()
                {
                    Fish = fish,
                    UserId = newUser.TwitchId,
                    Weight = entry.Weight + 1
                };
                var result = LeaderboardSystem.UpdateGlobalLeaderboard(catchData);
                var leaderboard = LeaderboardSystem.GetLeaderboard();
                Assert.IsTrue(result);
                Assert.AreEqual(catchData.Weight, leaderboard.First().Weight);
                Assert.AreEqual(newUser.TwitchId, leaderboard.First().UserId);
            }
        }

        [TestMethod]
        public void UpdateGlobalLeaderboardReturnsFalseWithNullCatchData()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var result = LeaderboardSystem.UpdateGlobalLeaderboard(null);
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void UpdateGlobalLeaderboardReturnsFalseWhenCatchIsNotNewRecord()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var fish = db.FishData.Read().First();
                var entry = db.FishingLeaderboard.Read(x => x.Fish.Id == fish.Id).First();
                var catchData = new Catch()
                {
                    Fish = fish,
                    Weight = entry.Weight - 1
                };
                var result = LeaderboardSystem.UpdateGlobalLeaderboard(catchData);
                var leaderboard = LeaderboardSystem.GetLeaderboard();
                Assert.IsFalse(result);
                Assert.AreNotEqual(catchData.Weight, leaderboard.First().Weight);
            }
        }

        [TestMethod]
        public void CatchFishUpdatesLeaderboardWhileTournamentActive()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingSystem.GetFisherByUser(user);
                var callbackMock = new Mock<LeaderboardEventHandler>();
                LeaderboardSystem.NewGlobalRecord += callbackMock.Object;
                var leaderboard = db.FishingLeaderboard.Read();
                foreach (var entry in leaderboard)
                {
                    db.FishingLeaderboard.Delete(entry);
                }
                db.FishingLeaderboard.Commit();
                TournamentSystem.StartTournament();
                fisher.Hooked = db.FishData.Read().First();
                var catchData = FishingSystem.CatchFish(fisher);
                Assert.IsNotNull(catchData);
                Assert.AreEqual(1, db.FishingLeaderboard.Read().Count());
                callbackMock.Verify(x => x(It.IsAny<LeaderboardEntry>()), Times.Once);
            }
        }
    }
}

