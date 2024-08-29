using Autofac;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using static LobotJR.Command.Controller.Fishing.TournamentController;

namespace LobotJR.Test.TournamentSystems.Fishing
{
    [TestClass]
    public class TournamentSystemTests
    {
        private IConnectionManager ConnectionManager;
        private FishingController FishingSystem;
        private TournamentController TournamentSystem;
        private PlayerController PlayerController;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            FishingSystem = AutofacMockSetup.Container.Resolve<FishingController>();
            TournamentSystem = AutofacMockSetup.Container.Resolve<TournamentController>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
        }

        [TestMethod]
        public void AddsTournamentPoints()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                TournamentSystem.CurrentTournament = new TournamentResult();
                TournamentSystem.CurrentTournament.Entries.Add(new TournamentEntry(user.TwitchId, 10));
                var points = TournamentSystem.AddTournamentPoints(user, 10);
                Assert.AreEqual(20, points);
                Assert.AreEqual(1, TournamentSystem.CurrentTournament.Entries.Count);
                Assert.AreEqual(points, TournamentSystem.CurrentTournament.Entries[0].Points);
            }
        }

        [TestMethod]
        public void AddTournamentPointsAddsUserIfNoEntryExists()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                TournamentSystem.CurrentTournament = new TournamentResult();
                var points = TournamentSystem.AddTournamentPoints(user, 10);
                Assert.AreEqual(10, points);
                Assert.AreEqual(1, TournamentSystem.CurrentTournament.Entries.Count);
                Assert.AreEqual(user.TwitchId, TournamentSystem.CurrentTournament.Entries[0].UserId);
                Assert.AreEqual(points, TournamentSystem.CurrentTournament.Entries[0].Points);
            }
        }

        [TestMethod]
        public void AddTournamentPointsDoesNothingIfNoTournamentRunning()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var points = TournamentSystem.AddTournamentPoints(user, 10);
                Assert.AreEqual(-1, points);
                Assert.IsNull(TournamentSystem.CurrentTournament);
            }
        }

        [TestMethod]
        public void StartsTournament()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var callbackMock = new Mock<TournamentStartHandler>();
                TournamentSystem.TournamentStarted += callbackMock.Object;
                TournamentSystem.StartTournament();
                Assert.IsNotNull(TournamentSystem.CurrentTournament);
                Assert.IsNull(TournamentSystem.NextTournament);
                callbackMock.Verify(x => x(It.IsAny<DateTime>()), Times.Once);
            }
        }

        [TestMethod]
        public void StartTournamentDoesNothingIfTournamentAlreadyRunning()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var tournament = new TournamentResult() { Id = 123 };
                TournamentSystem.CurrentTournament = tournament;
                TournamentSystem.StartTournament();
                Assert.AreEqual(tournament.Id, TournamentSystem.CurrentTournament.Id);
            }
        }

        [TestMethod]
        public void StartTournamentCancelsFishingUsers()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingSystem.GetFisherByUser(user);
                FishingSystem.Cast(user);
                FishingSystem.HookFish(fisher, false);
                TournamentSystem.StartTournament();
                Assert.IsNotNull(TournamentSystem.CurrentTournament);
                Assert.IsFalse(fisher.IsFishing);
                Assert.IsNull(fisher.Hooked);
                Assert.IsNull(fisher.HookedTime);
            }
        }

        [TestMethod]
        public void StartTournamentUpdatesCastTimes()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingSystem.GetFisherByUser(user);
                var settings = SettingsManager.GetGameSettings();
                fisher.IsFishing = false;
                TournamentSystem.StartTournament();
                FishingSystem.Cast(fisher.User);
                Assert.IsTrue(fisher.IsFishing);
                Assert.IsTrue(fisher.HookedTime >= DateTime.Now.AddSeconds(settings.FishingTournamentCastMinimum));
                Assert.IsTrue(fisher.HookedTime <= DateTime.Now.AddSeconds(settings.FishingTournamentCastMaximum));
            }
        }

        [TestMethod]
        public void EndsTournament()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var tournament = new TournamentResult() { Id = 123 };
                var callbackMock = new Mock<TournamentEndHandler>();
                TournamentSystem.TournamentEnded += callbackMock.Object;
                TournamentSystem.CurrentTournament = tournament;
                TournamentSystem.NextTournament = null;
                TournamentSystem.EndTournament();
                Assert.IsNull(TournamentSystem.CurrentTournament);
                Assert.IsNotNull(TournamentSystem.NextTournament);
                Assert.IsTrue(db.TournamentResults.Read(x => x.Id == tournament.Id).Any());
                callbackMock.Verify(x => x(It.IsAny<TournamentResult>(), It.IsAny<DateTime>()));
            }
        }

        [TestMethod]
        public void EndTournamentDoesNothingIfNoTournamentRunning()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var resultCount = db.TournamentResults.Read().Count();
                TournamentSystem.NextTournament = null;
                TournamentSystem.EndTournament();
                Assert.IsNull(TournamentSystem.CurrentTournament);
                Assert.AreEqual(resultCount, db.TournamentResults.Read().Count());
            }
        }

        [TestMethod]
        public void EndTournamentResetsCastTimes()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var fisher = FishingSystem.GetFisherByUser(user);
                var settings = SettingsManager.GetGameSettings();
                fisher.IsFishing = false;
                TournamentSystem.StartTournament();
                TournamentSystem.EndTournament();
                FishingSystem.Cast(fisher.User);
                Assert.IsTrue(fisher.IsFishing);
                Assert.IsTrue(fisher.HookedTime >= DateTime.Now.AddSeconds(settings.FishingCastMinimum));
                Assert.IsTrue(fisher.HookedTime <= DateTime.Now.AddSeconds(settings.FishingCastMaximum));
            }
        }

        [TestMethod]
        public void GetsTournamentResults()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var expectedResults = db.TournamentResults.Read().OrderByDescending(x => x.Date).First();
                var actualResults = TournamentSystem.GetLatestResults();
                Assert.AreEqual(expectedResults.Id, actualResults.Id);
            }
        }

        [TestMethod]
        public void GetResultForUser()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var expectedResults = db.TournamentResults.Read(x => x.Entries.Any(y => y.UserId.Equals(user.TwitchId)));
                var actualResults = TournamentSystem.GetResultsForUser(user);
                Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            }
        }

        [TestMethod]
        public void ProcessStartsTournamentOnTimer()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                TournamentSystem.NextTournament = DateTime.Now;
                TournamentSystem.Process();
                Assert.IsNotNull(TournamentSystem.CurrentTournament);
                Assert.IsNull(TournamentSystem.NextTournament);
                PlayerController.AwardsEnabled = false;
            }
        }

        [TestMethod]
        public void ProcessEndsTournamentOnTimer()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                TournamentSystem.CurrentTournament = new TournamentResult()
                {
                    Date = DateTime.Now
                };
                TournamentSystem.NextTournament = null;
                TournamentSystem.Process();
                Assert.IsNull(TournamentSystem.CurrentTournament);
                Assert.IsNotNull(TournamentSystem.NextTournament);
                PlayerController.AwardsEnabled = false;
            }
        }

        [TestMethod]
        public void ProcessCancelsTournamentWhenBroadcastingEnds()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                TournamentSystem.CurrentTournament = new TournamentResult();
                TournamentSystem.Process();
                Assert.IsNull(TournamentSystem.CurrentTournament);
                Assert.IsNull(TournamentSystem.NextTournament);
                PlayerController.AwardsEnabled = false;
            }
        }
    }
}
