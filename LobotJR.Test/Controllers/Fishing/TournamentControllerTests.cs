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
    public class TournamentControllerTests
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
            PlayerController.AwardsEnabled = true;
        }

        public void Cleanup()
        {
            AutofacMockSetup.ResetFishingRecords();
        }

        [TestMethod]
        public void AddsTournamentPoints()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            TournamentSystem.CurrentTournament = new TournamentResult();
            TournamentSystem.CurrentTournament.Entries.Add(new TournamentEntry(user.TwitchId, 10));
            var points = TournamentSystem.AddTournamentPoints(user, 10);
            Assert.AreEqual(20, points);
            Assert.AreEqual(1, TournamentSystem.CurrentTournament.Entries.Count);
            Assert.AreEqual(points, TournamentSystem.CurrentTournament.Entries[0].Points);
        }

        [TestMethod]
        public void AddTournamentPointsAddsUserIfNoEntryExists()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            TournamentSystem.CurrentTournament = new TournamentResult();
            var points = TournamentSystem.AddTournamentPoints(user, 10);
            Assert.AreEqual(10, points);
            Assert.AreEqual(1, TournamentSystem.CurrentTournament.Entries.Count);
            Assert.AreEqual(user.TwitchId, TournamentSystem.CurrentTournament.Entries[0].UserId);
            Assert.AreEqual(points, TournamentSystem.CurrentTournament.Entries[0].Points);
        }

        [TestMethod]
        public void AddTournamentPointsDoesNothingIfNoTournamentRunning()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            TournamentSystem.CurrentTournament = null;
            var points = TournamentSystem.AddTournamentPoints(user, 10);
            Assert.AreEqual(-1, points);
            Assert.IsNull(TournamentSystem.CurrentTournament);
        }

        [TestMethod]
        public void StartsTournament()
        {
            var db = ConnectionManager.CurrentConnection;
            var callbackMock = new Mock<TournamentStartHandler>();
            TournamentSystem.TournamentStarted += callbackMock.Object;
            TournamentSystem.CurrentTournament = null;
            TournamentSystem.StartTournament();
            Assert.IsNotNull(TournamentSystem.CurrentTournament);
            Assert.IsNull(TournamentSystem.NextTournament);
            callbackMock.Verify(x => x(It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void StartTournamentDoesNothingIfTournamentAlreadyRunning()
        {
            var tournament = new TournamentResult() { Id = 123 };
            TournamentSystem.CurrentTournament = tournament;
            TournamentSystem.StartTournament();
            Assert.AreEqual(tournament.Id, TournamentSystem.CurrentTournament.Id);
        }

        [TestMethod]
        public void StartTournamentCancelsFishingUsers()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            FishingSystem.Cast(user);
            FishingSystem.HookFish(fisher, false);
            TournamentSystem.CurrentTournament = null;
            TournamentSystem.StartTournament();
            Assert.IsNotNull(TournamentSystem.CurrentTournament);
            Assert.IsFalse(fisher.IsFishing);
            Assert.IsNull(fisher.Hooked);
            Assert.IsNull(fisher.HookedTime);
        }

        [TestMethod]
        public void StartTournamentUpdatesCastTimes()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingSystem.GetFisherByUser(user);
            var settings = SettingsManager.GetGameSettings();
            fisher.IsFishing = false;
            TournamentSystem.CurrentTournament = null;
            TournamentSystem.StartTournament();
            FishingSystem.Cast(fisher.User);
            Assert.IsTrue(fisher.IsFishing);
            Assert.IsTrue(fisher.HookedTime >= DateTime.Now.AddSeconds(settings.FishingTournamentCastMinimum));
            Assert.IsTrue(fisher.HookedTime <= DateTime.Now.AddSeconds(settings.FishingTournamentCastMaximum));
        }

        [TestMethod]
        public void EndsTournament()
        {
            var db = ConnectionManager.CurrentConnection;
            var tournament = new TournamentResult() { Id = 123 };
            var callbackMock = new Mock<TournamentEndHandler>();
            TournamentSystem.TournamentEnded += callbackMock.Object;
            TournamentSystem.CurrentTournament = tournament;
            TournamentSystem.NextTournament = null;
            TournamentSystem.EndTournament();
            db.Commit();
            Assert.IsNull(TournamentSystem.CurrentTournament);
            Assert.IsNotNull(TournamentSystem.NextTournament);
            Assert.IsTrue(db.TournamentResults.Read(x => x.Id == tournament.Id).Any());
            callbackMock.Verify(x => x(It.IsAny<TournamentResult>(), It.IsAny<DateTime>()));
        }

        [TestMethod]
        public void EndTournamentDoesNothingIfNoTournamentRunning()
        {
            var db = ConnectionManager.CurrentConnection;
            var resultCount = db.TournamentResults.Read().Count();
            TournamentSystem.NextTournament = null;
            TournamentSystem.EndTournament();
            Assert.IsNull(TournamentSystem.CurrentTournament);
            Assert.AreEqual(resultCount, db.TournamentResults.Read().Count());
        }

        [TestMethod]
        public void EndTournamentResetsCastTimes()
        {
            var db = ConnectionManager.CurrentConnection;
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

        [TestMethod]
        public void GetsTournamentResults()
        {
            var db = ConnectionManager.CurrentConnection;
            var expectedResults = db.TournamentResults.Read().OrderByDescending(x => x.Date).First();
            var actualResults = TournamentSystem.GetLatestResults();
            Assert.AreEqual(expectedResults.Id, actualResults.Id);
        }

        [TestMethod]
        public void GetResultForUser()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var expectedResults = db.TournamentResults.Read(x => x.Entries.Any(y => y.UserId.Equals(user.TwitchId)));
            var actualResults = TournamentSystem.GetResultsForUser(user);
            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
        }

        [TestMethod]
        public void ProcessStartsTournamentOnTimer()
        {
            TournamentSystem.NextTournament = DateTime.Now;
            TournamentSystem.Process();
            Assert.IsNotNull(TournamentSystem.CurrentTournament);
            Assert.IsNull(TournamentSystem.NextTournament);
        }

        [TestMethod]
        public void ProcessEndsTournamentOnTimer()
        {
            TournamentSystem.CurrentTournament = new TournamentResult()
            {
                Date = DateTime.Now
            };
            TournamentSystem.NextTournament = null;
            TournamentSystem.Process();
            Assert.IsNull(TournamentSystem.CurrentTournament);
            Assert.IsNotNull(TournamentSystem.NextTournament);
        }

        [TestMethod]
        public void ProcessCancelsTournamentWhenBroadcastingEnds()
        {
            PlayerController.AwardsEnabled = false;
            TournamentSystem.CurrentTournament = new TournamentResult();
            TournamentSystem.Process();
            Assert.IsNull(TournamentSystem.CurrentTournament);
            Assert.IsNull(TournamentSystem.NextTournament);
        }
    }
}
