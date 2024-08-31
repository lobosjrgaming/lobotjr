using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.View;
using LobotJR.Command.View.Fishing;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.Fishing
{
    [TestClass]
    public class TournamentModuleTests
    {
        private TournamentResultsResponse ResultsFromCompact(string compact)
        {
            var data = compact.Substring(0, compact.Length - 1).Split('|').ToArray();
            return new TournamentResultsResponse()
            {
                Ended = DateTime.Parse(data[0]),
                Participants = int.Parse(data[1]),
                Winner = data[2],
                WinnerPoints = int.Parse(data[3]),
                Rank = int.Parse(data[4]),
                UserPoints = int.Parse(data[5])
            };
        }

        private TournamentRecordsResponse RecordsFromCompact(string compact)
        {
            var data = compact.Substring(0, compact.Length - 1).Split('|').ToArray();
            return new TournamentRecordsResponse()
            {
                TopRank = int.Parse(data[0]),
                TopRankScore = int.Parse(data[1]),
                TopScore = int.Parse(data[2]),
                TopScoreRank = int.Parse(data[3])
            };
        }

        private void ClearTournaments(IDatabase db)
        {
            db.TournamentResults.Delete();
            db.Commit();
        }

        private IConnectionManager ConnectionManager;
        private UserController UserController;
        private TournamentController TournamentController;
        private TournamentView TournamentView;
        private PlayerController PlayerController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
            TournamentController = AutofacMockSetup.Container.Resolve<TournamentController>();
            TournamentView = AutofacMockSetup.Container.Resolve<TournamentView>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetFishingRecords();
            PlayerController.AwardsEnabled = true;
            TournamentController.CurrentTournament = null;
        }

        [TestMethod]
        public void PushesNotificationOnTournamentStart()
        {
            var db = ConnectionManager.CurrentConnection;
            var handlerMock = new Mock<PushNotificationHandler>();
            TournamentView.PushNotification += handlerMock.Object;
            TournamentController.StartTournament();
            handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.IsTrue(result.Messages.Any(x => x.Contains("!cast")));
        }

        [TestMethod]
        public void CalculatesResultsOnTournamentEnd()
        {
            var db = ConnectionManager.CurrentConnection;
            var firstFisher = db.Users.Read().First();
            var secondFisher = UserController.GetUserById(firstFisher.TwitchId);
            TournamentController.StartTournament();
            TournamentController.CurrentTournament.Entries.Add(new TournamentEntry(firstFisher.TwitchId, 100));
            TournamentController.CurrentTournament.Entries.Add(new TournamentEntry(secondFisher.TwitchId, 200));
            TournamentController.EndTournament();
            db.Commit();
            var results = db.TournamentResults.Read().OrderByDescending(x => x.Date).First();
            Assert.AreEqual(secondFisher.TwitchId, results.Winner.UserId);
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEnd()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var handlerMock = new Mock<PushNotificationHandler>();
            TournamentController.StartTournament();
            TournamentView.PushNotification += handlerMock.Object;
            TournamentController.CurrentTournament.Entries.Add(new TournamentEntry(user.TwitchId, 100));
            TournamentController.EndTournament();
            db.Commit();
            handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.IsTrue(result.Messages.Any(x => x.Contains("end")));
            Assert.IsTrue(result.Messages.Any(x => x.Contains(user.Username)));
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEndWithNoParticipants()
        {
            var db = ConnectionManager.CurrentConnection;
            var handlerMock = new Mock<PushNotificationHandler>();
            TournamentController.StartTournament();
            TournamentView.PushNotification += handlerMock.Object;
            TournamentController.EndTournament();
            db.Commit();
            handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.IsTrue(result.Messages.Any(x => x.Contains("end")));
            Assert.IsFalse(result.Messages.Any(x => x.Contains("participants", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEndByStreamStopping()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var handlerMock = new Mock<PushNotificationHandler>();

            TournamentController.StartTournament();
            TournamentView.PushNotification += handlerMock.Object;
            TournamentController.CurrentTournament.Entries.Add(new TournamentEntry(user.TwitchId, 100));
            PlayerController.AwardsEnabled = false;
            TournamentController.EndTournament();
            db.Commit();
            handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.IsTrue(result.Messages.Any(x => x.Contains("offline")));
            Assert.IsTrue(result.Messages.Any(x => x.Contains(user.Username, StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEndByStreamStoppingWithNoParticipants()
        {
            var db = ConnectionManager.CurrentConnection;
            var handlerMock = new Mock<PushNotificationHandler>();
            TournamentController.StartTournament();
            TournamentView.PushNotification += handlerMock.Object;
            PlayerController.AwardsEnabled = false;
            TournamentController.EndTournament();
            db.Commit();
            handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
            var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
            Assert.IsTrue(result.Messages.Any(x => x.Contains("offline")));
            Assert.IsFalse(result.Messages.Any(x => x.Contains("winner", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void TournamentResultsGetsLatestTournamentWithParticipation()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
            var results = command.Executor.Execute(UserController.GetUserByName("Foo"), "");
            Assert.IsNotNull(results.Responses);
            Assert.AreEqual(3, results.Responses.Count);
            Assert.IsTrue(results.Responses.Any(x => x.Contains("30 seconds")));
            Assert.IsTrue(results.Responses.Any(x => x.Contains("Fizz") && x.Contains("30")));
            Assert.IsTrue(results.Responses.Any(x => x.Contains("3rd") && x.Contains("10")));
        }

        [TestMethod]
        public void TournamentResultsGetsLatestTournamentWithoutParticipation()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
            var results = command.Executor.Execute(UserController.GetUserByName("Buzz"), "");
            Assert.IsNotNull(results.Responses);
            Assert.AreEqual(2, results.Responses.Count);
            Assert.IsTrue(results.Responses.Any(x => x.Contains("30 seconds")));
            Assert.IsTrue(results.Responses.Any(x => x.Contains("Fizz") && x.Contains("30")));
        }

        [TestMethod]
        public void TournamentResultsGetsLatestTournamentForWinner()
        {
            var db = ConnectionManager.CurrentConnection;
            var all = db.TournamentResults.Read().OrderByDescending(x => x.Date).ToList();
            var any = db.TournamentResults.Read().First();
            var latest = db.TournamentResults.Read().OrderByDescending(x => x.Date).First();
            var winner = UserController.GetUserById(latest.Winner.UserId);
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
            var results = command.Executor.Execute(winner, "");
            var elapsed = Math.Floor((DateTime.Now - latest.Date).TotalSeconds);
            Assert.IsNotNull(results.Responses);
            Assert.AreEqual(2, results.Responses.Count);
            Assert.IsTrue(results.Responses.Any(x => x.Contains($"{elapsed} seconds")));
            Assert.IsFalse(results.Responses.Any(x => x.Contains(winner.Username)));
            Assert.IsTrue(results.Responses.Any(x => x.Contains("You") && x.Contains("30")));
        }

        [TestMethod]
        public void TournamentResultsGetsErrorMessageWhenNoTournamentHasCompleted()
        {
            var db = ConnectionManager.CurrentConnection;
            ClearTournaments(db);
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
            var results = command.Executor.Execute(UserController.GetUserByName("Foo"), "");
            Assert.IsNotNull(results.Responses);
            Assert.AreEqual(1, results.Responses.Count);
        }

        [TestMethod]
        public void TournamentResultsCompactGetsLatestTournament()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
            var results = command.CompactExecutor.Execute(UserController.GetUserByName("Buzz"), "");
            var resultObject = ResultsFromCompact(results.ToCompact().First());
            var latest = db.TournamentResults.Read().OrderByDescending(x => x.Date).First();
            var winner = db.Users.Read(x => x.TwitchId.Equals(latest.Winner.UserId)).First();
            Assert.IsNotNull(resultObject);
            Assert.AreEqual(winner.Username, resultObject.Winner);
            Assert.AreEqual(latest.Entries.Count(), resultObject.Participants);
            Assert.AreEqual(latest.Winner.Points, resultObject.WinnerPoints);
            Assert.AreEqual(0, resultObject.Rank);
            Assert.AreEqual(0, resultObject.UserPoints);
        }

        [TestMethod]
        public void TournamentResultsCompactIncludesUserData()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
            var latest = db.TournamentResults.Read().OrderByDescending(x => x.Date).First();
            var winner = UserController.GetUserById(latest.Winner.UserId);
            var loser = UserController.GetUserById(latest.Entries.OrderByDescending(x => x.Points).Last().UserId);
            var entry = latest.Entries.First(x => x.UserId.Equals(loser.TwitchId));
            var rank = latest.Entries.OrderByDescending(x => x.Points).ToList().IndexOf(entry);
            var results = command.CompactExecutor.Execute(loser, "");
            var resultObject = ResultsFromCompact(results.ToCompact().First());
            Assert.IsNotNull(resultObject);
            Assert.AreEqual(winner.Username, resultObject.Winner);
            Assert.AreEqual(latest.Entries.Count(), resultObject.Participants);
            Assert.AreEqual(latest.Winner.Points, resultObject.WinnerPoints);
            Assert.AreEqual(rank + 1, resultObject.Rank);
            Assert.AreEqual(entry.Points, resultObject.UserPoints);
        }

        [TestMethod]
        public void TournamentResultsCompactReturnsNullIfNoTournamentsHaveTakenPlace()
        {
            var db = ConnectionManager.CurrentConnection;
            ClearTournaments(db);
            var leftovers = db.TournamentResults.Read().ToArray();
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
            var results = command.CompactExecutor.Execute(UserController.GetUserByName("Buzz"), "");
            Assert.IsNull(results);
        }

        [TestMethod]
        public void TournamentRecordsGetsUsersRecords()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
            var results = command.Executor.Execute(UserController.GetUserByName("Foo"), "");
            Assert.IsNotNull(results.Responses);
            Assert.AreEqual(2, results.Responses.Count);
            Assert.IsTrue(results.Responses.Any(x => x.Contains("1st") && x.Contains("35 points")));
            Assert.IsTrue(results.Responses.Any(x => x.Contains("2nd") && x.Contains("40 points")));
        }

        [TestMethod]
        public void TournamentRecordsGetsErrorWhenUserHasNotCompetedInAnyTournaments()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
            var results = command.Executor.Execute(UserController.GetUserByName("Buzz"), "");
            Assert.IsNotNull(results.Responses);
            Assert.AreEqual(1, results.Responses.Count);
        }

        [TestMethod]
        public void TournamentRecordsCompactGetsUserRecords()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
            var results = command.CompactExecutor.Execute(UserController.GetUserByName("Foo"), "");
            var resultObject = RecordsFromCompact(results.ToCompact().First());
            Assert.IsNotNull(resultObject);
            Assert.AreEqual(1, resultObject.TopRank);
            Assert.AreEqual(35, resultObject.TopRankScore);
            Assert.AreEqual(40, resultObject.TopScore);
            Assert.AreEqual(2, resultObject.TopScoreRank);
        }

        [TestMethod]
        public void TournamentRecordsCompactGetsNullIfUserHasNeverEntered()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = TournamentView.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
            var results = command.CompactExecutor.Execute(UserController.GetUserByName("Buzz"), "");
            Assert.IsNull(results);
        }
    }
}
