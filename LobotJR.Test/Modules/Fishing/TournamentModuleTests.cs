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

        private void ClearTournaments(IDatabase Manager)
        {
            var tournamentResults = Manager.TournamentResults.Read();
            foreach (var entry in tournamentResults)
            {
                Manager.TournamentResults.Delete(entry);
            }
            Manager.TournamentResults.Commit();
        }

        private IConnectionManager ConnectionManager;
        private UserController UserLookup;
        private TournamentController TournamentSystem;
        private TournamentView TournamentModule;
        private PlayerController PlayerController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            UserLookup = AutofacMockSetup.Container.Resolve<UserController>();
            TournamentSystem = AutofacMockSetup.Container.Resolve<TournamentController>();
            TournamentModule = AutofacMockSetup.Container.Resolve<TournamentView>();
        }

        [TestMethod]
        public void PushesNotificationOnTournamentStart()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var handlerMock = new Mock<PushNotificationHandler>();
                TournamentModule.PushNotification += handlerMock.Object;
                TournamentSystem.StartTournament();
                handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.IsTrue(result.Messages.Any(x => x.Contains("!cast")));
            }
        }

        [TestMethod]
        public void CalculatesResultsOnTournamentEnd()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                var firstFisher = db.Users.Read().First();
                var secondFisher = UserLookup.GetUserById(firstFisher.TwitchId);
                TournamentSystem.StartTournament();
                TournamentSystem.CurrentTournament.Entries.Add(new TournamentEntry(firstFisher.TwitchId, 100));
                TournamentSystem.CurrentTournament.Entries.Add(new TournamentEntry(secondFisher.TwitchId, 200));
                TournamentSystem.EndTournament();
                var results = db.TournamentResults.Read().OrderByDescending(x => x.Date).First();
                Assert.AreEqual(secondFisher.TwitchId, results.Winner.UserId);
                PlayerController.AwardsEnabled = false;
            }
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEnd()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                var user = db.Users.Read().First();
                var handlerMock = new Mock<PushNotificationHandler>();
                TournamentSystem.StartTournament();
                TournamentModule.PushNotification += handlerMock.Object;
                TournamentSystem.CurrentTournament.Entries.Add(new TournamentEntry(user.TwitchId, 100));
                TournamentSystem.EndTournament();
                handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.IsTrue(result.Messages.Any(x => x.Contains("end")));
                Assert.IsTrue(result.Messages.Any(x => x.Contains(user.Username)));
                PlayerController.AwardsEnabled = false;
            }
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEndWithNoParticipants()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                var handlerMock = new Mock<PushNotificationHandler>();
                TournamentSystem.StartTournament();
                TournamentModule.PushNotification += handlerMock.Object;
                TournamentSystem.EndTournament();
                handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.IsTrue(result.Messages.Any(x => x.Contains("end")));
                Assert.IsFalse(result.Messages.Any(x => x.Contains("participants", StringComparison.OrdinalIgnoreCase)));
                PlayerController.AwardsEnabled = false;
            }
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEndByStreamStopping()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                var user = db.Users.Read().First();
                var handlerMock = new Mock<PushNotificationHandler>();

                TournamentSystem.StartTournament();
                TournamentModule.PushNotification += handlerMock.Object;
                TournamentSystem.CurrentTournament.Entries.Add(new TournamentEntry(user.TwitchId, 100));
                PlayerController.AwardsEnabled = false;
                TournamentSystem.EndTournament();
                handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.IsTrue(result.Messages.Any(x => x.Contains("offline")));
                Assert.IsTrue(result.Messages.Any(x => x.Contains(user.Username, StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void PushesNotificationOnTournamentEndByStreamStoppingWithNoParticipants()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                PlayerController.AwardsEnabled = true;
                var handlerMock = new Mock<PushNotificationHandler>();
                TournamentSystem.StartTournament();
                TournamentModule.PushNotification += handlerMock.Object;
                PlayerController.AwardsEnabled = false;
                TournamentSystem.EndTournament();
                handlerMock.Verify(x => x(null, It.IsAny<CommandResult>()), Times.Once);
                var result = handlerMock.Invocations[0].Arguments[1] as CommandResult;
                Assert.IsTrue(result.Messages.Any(x => x.Contains("offline")));
                Assert.IsFalse(result.Messages.Any(x => x.Contains("winner", StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void TournamentResultsGetsLatestTournamentWithParticipation()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
                var results = command.Executor.Execute(UserLookup.GetUserByName("Foo"), "");
                Assert.IsNotNull(results.Responses);
                Assert.AreEqual(3, results.Responses.Count);
                Assert.IsTrue(results.Responses.Any(x => x.Contains("30 seconds")));
                Assert.IsTrue(results.Responses.Any(x => x.Contains("Fizz") && x.Contains("30")));
                Assert.IsTrue(results.Responses.Any(x => x.Contains("3rd") && x.Contains("10")));
            }
        }

        [TestMethod]
        public void TournamentResultsGetsLatestTournamentWithoutParticipation()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
                var results = command.Executor.Execute(UserLookup.GetUserByName("Buzz"), "");
                Assert.IsNotNull(results.Responses);
                Assert.AreEqual(2, results.Responses.Count);
                Assert.IsTrue(results.Responses.Any(x => x.Contains("30 seconds")));
                Assert.IsTrue(results.Responses.Any(x => x.Contains("Fizz") && x.Contains("30")));
            }
        }

        [TestMethod]
        public void TournamentResultsGetsLatestTournamentForWinner()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
                var results = command.Executor.Execute(UserLookup.GetUserByName("Fizz"), "");
                Assert.IsNotNull(results.Responses);
                Assert.AreEqual(2, results.Responses.Count);
                Assert.IsTrue(results.Responses.Any(x => x.Contains("30 seconds")));
                Assert.IsFalse(results.Responses.Any(x => x.Contains("Fizz")));
                Assert.IsTrue(results.Responses.Any(x => x.Contains("You") && x.Contains("30")));
            }
        }

        [TestMethod]
        public void TournamentResultsGetsErrorMessageWhenNoTournamentHasCompleted()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                ClearTournaments(db);
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
                var results = command.Executor.Execute(UserLookup.GetUserByName("Foo"), "");
                Assert.IsNotNull(results.Responses);
                Assert.AreEqual(1, results.Responses.Count);
            }
        }

        [TestMethod]
        public void TournamentResultsCompactGetsLatestTournament()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
                var results = command.CompactExecutor.Execute(UserLookup.GetUserByName("Buzz"), "");
                var resultObject = ResultsFromCompact(results.ToCompact().First());
                Assert.IsNotNull(resultObject);
                Assert.AreEqual("Fizz", resultObject.Winner);
                Assert.AreEqual(3, resultObject.Participants);
                Assert.AreEqual(30, resultObject.WinnerPoints);
                Assert.AreEqual(0, resultObject.Rank);
                Assert.AreEqual(0, resultObject.UserPoints);
            }
        }

        [TestMethod]
        public void TournamentResultsCompactIncludesUserData()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
                var results = command.CompactExecutor.Execute(UserLookup.GetUserByName("Foo"), "");
                var resultObject = ResultsFromCompact(results.ToCompact().First());
                Assert.IsNotNull(resultObject);
                Assert.AreEqual("Fizz", resultObject.Winner);
                Assert.AreEqual(3, resultObject.Participants);
                Assert.AreEqual(30, resultObject.WinnerPoints);
                Assert.AreEqual(3, resultObject.Rank);
                Assert.AreEqual(10, resultObject.UserPoints);
            }
        }

        [TestMethod]
        public void TournamentResultsCompactReturnsNullIfNoTournamentsHaveTakenPlace()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                ClearTournaments(db);
                var leftovers = db.TournamentResults.Read().ToArray();
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentResults")).FirstOrDefault();
                var results = command.CompactExecutor.Execute(UserLookup.GetUserByName("Buzz"), "");
                Assert.IsNull(results);
            }
        }

        [TestMethod]
        public void TournamentRecordsGetsUsersRecords()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
                var results = command.Executor.Execute(UserLookup.GetUserByName("Foo"), "");
                Assert.IsNotNull(results.Responses);
                Assert.AreEqual(2, results.Responses.Count);
                Assert.IsTrue(results.Responses.Any(x => x.Contains("1st") && x.Contains("35 points")));
                Assert.IsTrue(results.Responses.Any(x => x.Contains("2nd") && x.Contains("40 points")));
            }
        }

        [TestMethod]
        public void TournamentRecordsGetsErrorWhenUserHasNotCompetedInAnyTournaments()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
                var results = command.Executor.Execute(UserLookup.GetUserByName("Buzz"), "");
                Assert.IsNotNull(results.Responses);
                Assert.AreEqual(1, results.Responses.Count);
            }
        }

        [TestMethod]
        public void TournamentRecordsCompactGetsUserRecords()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
                var results = command.CompactExecutor.Execute(UserLookup.GetUserByName("Foo"), "");
                var resultObject = RecordsFromCompact(results.ToCompact().First());
                Assert.IsNotNull(resultObject);
                Assert.AreEqual(1, resultObject.TopRank);
                Assert.AreEqual(35, resultObject.TopRankScore);
                Assert.AreEqual(40, resultObject.TopScore);
                Assert.AreEqual(2, resultObject.TopScoreRank);
            }
        }

        [TestMethod]
        public void TournamentRecordsCompactGetsNullIfUserHasNeverEntered()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = TournamentModule.Commands.Where(x => x.Name.Equals("TournamentRecords")).FirstOrDefault();
                var results = command.CompactExecutor.Execute(UserLookup.GetUserByName("Buzz"), "");
                Assert.IsNull(results);
            }
        }
    }
}
