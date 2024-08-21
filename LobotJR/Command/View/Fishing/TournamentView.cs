using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Fishing;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Fishing
{
    /// <summary>
    /// View containing commands related to fishing tournaments.
    /// </summary>
    public class TournamentView : ICommandView, IPushNotifier
    {
        private readonly TournamentController TournamentController;
        private readonly UserController UserController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Fishing.Tournament";
        /// <summary>
        /// Notifications when a tournament starts or ends.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public TournamentView(TournamentController tournamentController, UserController userController)
        {
            TournamentController = tournamentController;
            tournamentController.TournamentStarted += Controller_TournamentStarted;
            tournamentController.TournamentEnded += Controller_TournamentEnded;
            UserController = userController;
            Commands = new CommandHandler[]
            {
                new CommandHandler("TournamentResults", this, CommandMethod.GetInfo(TournamentResults), CommandMethod.GetInfo(TournamentResultsCompact), "TournamentResults", "tournament-results"),
                new CommandHandler("TournamentRecords", this, CommandMethod.GetInfo(TournamentRecords), CommandMethod.GetInfo(TournamentRecordsCompact), "TournamentRecords", "tournament-records"),
                new CommandHandler("NextTournament", this, CommandMethod.GetInfo(NextTournament), CommandMethod.GetInfo(NextTournamentCompact), "NextTournament", "next-tournament"),
            };
        }

        private void Controller_TournamentStarted(DateTime end)
        {
            var duration = end - DateTime.Now;
            var message = $"A fishing tournament has just begun! For the next {Math.Round(duration.TotalMinutes)} minutes, fish can be caught more quickly & will be eligible for leaderboard recognition! Head to https://tinyurl.com/PlayWolfpackRPG and type !cast to play!";
            PushNotification?.Invoke(null, new CommandResult(true, message));
        }

        private void Controller_TournamentEnded(TournamentResult result, DateTime? next)
        {
            string message;
            if (next == null)
            {
                message = "Stream has gone offline, so the fishing tournament was ended early. D:";
                if (result.Entries.Count > 0)
                {
                    message += $" Winner at the time of conclusion: {UserController.GetUserById(result.Winner.UserId).Username} with a score of {result.Winner.Points}.";

                }
            }
            else
            {
                if (result.Entries.Count > 0)
                {
                    message = $"The fishing tournament has ended! Out of {result.Entries.Count} participants, {UserController.GetUserById(result.Winner.UserId).Username} won with {result.Winner.Points} points!";
                }
                else
                {
                    message = "The fishing tournament has ended.";
                }
            }
            PushNotification?.Invoke(null, new CommandResult(true, message));
        }

        public CommandResult TournamentResults(User user)
        {
            var result = TournamentResultsCompact(user);
            if (result == null)
            {
                return new CommandResult("No fishing tournaments have completed.");
            }
            var sinceEnded = DateTime.Now - result.Ended;
            var pluralized = "participant";
            if (result.Participants > 1)
            {
                pluralized += "s";
            }
            var responses = new List<string>(new string[] { $"The most recent tournament ended {sinceEnded.ToCommonString()} ago with {result.Participants} {pluralized}." });
            if (result.Rank > 0)
            {
                if (result.Winner.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
                {
                    responses.Add($"You won the tournament with {result.WinnerPoints} points.");
                }
                else
                {
                    responses.Add($"The tournament was won by {result.Winner} with {result.WinnerPoints} points.");
                    responses.Add($"You placed {result.Rank.ToOrdinal()} with {result.UserPoints} points.");
                }
            }
            else
            {
                responses.Add($"The tournament was won by {result.Winner} with {result.WinnerPoints} points.");
            }
            return new CommandResult(responses.ToArray());
        }

        public TournamentResultsResponse TournamentResultsCompact(User user)
        {
            var tournament = TournamentController.GetLatestResults();
            if (tournament != null)
            {
                var winner = tournament.Winner;
                if (winner != null)
                {
                    var output = new TournamentResultsResponse()
                    {
                        Ended = tournament.Date,
                        Participants = tournament.Entries.Count,
                        Winner = UserController.GetUserById(winner.UserId).Username,
                        WinnerPoints = winner.Points
                    };
                    var userEntry = tournament.GetEntryByUser(user);
                    if (userEntry != null)
                    {
                        output.Rank = tournament.GetRankByUser(user);
                        output.UserPoints = userEntry.Points;
                    }
                    return output;
                }
            }
            return null;
        }

        public CommandResult TournamentRecords(User user)
        {
            var records = TournamentRecordsCompact(user);
            if (records == null)
            {
                return new CommandResult("You have not entered any fishing tournaments.");
            }
            return new CommandResult($"Your highest score in a tournament was {records.TopScore} points, earning you {records.TopScoreRank.ToOrdinal()} place.",
                $"Your best tournament placement was {records.TopRank.ToOrdinal()} place, with {records.TopRankScore} points.");
        }

        public TournamentRecordsResponse TournamentRecordsCompact(User user)
        {
            var output = new Dictionary<string, string>();
            var tournaments = TournamentController.GetResultsForUser(user);
            if (!tournaments.Any())
            {
                return null;
            }
            var topRank = tournaments.OrderBy(x => x.GetRankByUser(user)).First();
            var topRankAndScore = tournaments.Where(x => x.GetRankByUser(user) == topRank.GetRankByUser(user)).OrderByDescending(x => x.GetEntryByUser(user).Points).First();
            var topScore = tournaments.OrderByDescending(x => x.GetEntryByUser(user).Points).First();
            var topScoreAndRank = tournaments.Where(x => x.GetEntryByUser(user).Points == topScore.GetEntryByUser(user).Points).OrderBy(x => x.GetRankByUser(user)).First();
            return new TournamentRecordsResponse()
            {
                TopRank = topRankAndScore.GetRankByUser(user),
                TopRankScore = topRankAndScore.GetEntryByUser(user).Points,
                TopScore = topScoreAndRank.GetEntryByUser(user).Points,
                TopScoreRank = topScoreAndRank.GetRankByUser(user)
            };
        }

        public CommandResult NextTournament()
        {
            var compact = NextTournamentCompact();
            if (compact.Items.Count() == 0)
            {
                return new CommandResult("Stream is offline. Next fishing tournament will begin 15m after the beginning of next stream.");
            }
            var toNext = compact.Items.FirstOrDefault();
            if (toNext.TotalMilliseconds > 0)
            {
                return new CommandResult($"Next fishing tournament begins in {toNext.TotalMinutes} minutes.");
            }
            return new CommandResult($"A fishing tournament is active now! Go catch fish at: https://tinyurl.com/PlayWolfpackRPG !");
        }

        public CompactCollection<TimeSpan> NextTournamentCompact()
        {
            if (TournamentController.NextTournament == null)
            {
                return new CompactCollection<TimeSpan>(new TimeSpan[0], x => x.ToString("c"));
            }
            else
            {
                var toNext = (DateTime)TournamentController.NextTournament - DateTime.Now;
                return new CompactCollection<TimeSpan>(new TimeSpan[] { toNext }, x => x.ToString("c"));
            }
        }
    }

    public class TournamentResultsResponse : ICompactResponse
    {
        public DateTime Ended { get; set; }
        public int Participants { get; set; }
        public string Winner { get; set; }
        public int WinnerPoints { get; set; }
        public int Rank { get; set; }
        public int UserPoints { get; set; }

        public IEnumerable<string> ToCompact()
        {
            return new string[] { $"{Ended}|{Participants}|{Winner}|{WinnerPoints}|{Rank}|{UserPoints};" };
        }
    }

    public class TournamentRecordsResponse : ICompactResponse
    {
        public int TopRank { get; set; }
        public int TopRankScore { get; set; }
        public int TopScore { get; set; }
        public int TopScoreRank { get; set; }

        public IEnumerable<string> ToCompact()
        {
            return new string[] { $"{TopRank}|{TopRankScore}|{TopScore}|{TopScoreRank};" };
        }
    }
}
