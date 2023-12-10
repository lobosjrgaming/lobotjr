using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Fishing
{
    /// <summary>
    /// Runs the tournament logic for the fishing system.
    /// </summary>
    public class TournamentSystem : ISystem
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IRepository<TournamentResult> TournamentResults;
        private readonly FishingSystem FishingSystem;
        private readonly LeaderboardSystem LeaderboardSystem;
        private readonly AppSettings Settings;

        /// <summary>
        /// Event handler for the start of a tournament.
        /// </summary>
        /// <param name="end">The timestamp for when the tournament ends.</param>
        public delegate void TournamentStartHandler(DateTime end);
        /// <summary>
        /// Event handler for the end of a tournament.
        /// </summary>
        /// <param name="result">The results of the tournament.</param>
        /// <param name="next">The timestamp for when the next tournament starts.</param>
        public delegate void TournamentEndHandler(TournamentResult result, DateTime? next);

        /// <summary>
        /// Event fired when a tournament starts.
        /// </summary>
        public event TournamentStartHandler TournamentStarted;
        /// <summary>
        /// Event fired when a tournament ends.
        /// </summary>
        public event TournamentEndHandler TournamentEnded;

        /// <summary>
        /// The current tournament, if one is running.
        /// </summary>
        public TournamentResult CurrentTournament { get; set; }
        /// <summary>
        /// The date and time of the next scheduled tournament.
        /// </summary>
        public DateTime? NextTournament { get; set; }
        /// <summary>
        /// Whether or not a tournament is currently running.
        /// </summary>
        public bool IsRunning { get { return CurrentTournament != null; } }

        public TournamentSystem(
            FishingSystem fishingSystem,
            LeaderboardSystem leaderboardSystem,
            IRepositoryManager repositoryManager)
        {
            FishingSystem = fishingSystem;
            LeaderboardSystem = leaderboardSystem;
            TournamentResults = repositoryManager.TournamentResults;

            Settings = repositoryManager.AppSettings.Read().First();
            NextTournament = DateTime.Now.AddMinutes(Settings.FishingTournamentInterval);
            fishingSystem.FishCaught += FishingSystem_FishCaught;
        }

        private void FishingSystem_FishCaught(Fisher fisher, Catch catchData)
        {
            if (IsRunning)
            {
                Logger.Debug("User {userName} ({userId}) caught a fish worth {points} points.", fisher?.User.Username, fisher?.User.TwitchId, catchData?.Points);
                LeaderboardSystem.UpdatePersonalLeaderboard(fisher.User, catchData);
                LeaderboardSystem.UpdateGlobalLeaderboard(catchData);
                AddTournamentPoints(fisher.User, catchData.Points);
            }
        }

        /// <summary>
        /// Retrieves the most recent tournament results.
        /// </summary>
        /// <returns>The result data from the most recent tournament.</returns>
        public TournamentResult GetLatestResults()
        {
            return TournamentResults.Read().OrderByDescending(x => x.Date).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all tournament results that contain an entry for a specific user.
        /// </summary>
        /// <param name="user">The user object of the user to check for.</param>
        /// <returns>An enumerable collection of all tournament results where that user participated.</returns>
        public IEnumerable<TournamentResult> GetResultsForUser(User user)
        {
            return TournamentResults.Read(x => x.GetEntryByUser(user) != null);
        }

        /// <summary>
        /// Adds points to a user in a tournament. If this is their first catch
        /// of the tournament, it will add an entry for them as well.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <param name="points">The amount of points to add.</param>
        /// <returns>The user's current point total.</returns>
        public int AddTournamentPoints(User user, int points)
        {
            if (CurrentTournament != null)
            {
                var entry = CurrentTournament.Entries.Where(x => x.UserId.Equals(user.TwitchId)).FirstOrDefault();
                if (entry == null)
                {
                    entry = new TournamentEntry(user.TwitchId, 0);
                    CurrentTournament.Entries.Add(entry);
                }
                entry.Points += points;
                return entry.Points;
            }
            return -1;
        }

        /// <summary>
        /// Starts a new tournament.
        /// </summary>
        public void StartTournament()
        {
            if (CurrentTournament == null)
            {
                FishingSystem.ResetFishers();
                FishingSystem.CastTimeMinimum = Settings.FishingTournamentCastMinimum;
                FishingSystem.CastTimeMaximum = Settings.FishingTournamentCastMaximum;

                CurrentTournament = new TournamentResult
                {
                    Date = DateTime.Now.AddMinutes(Settings.FishingTournamentDuration)
                };
                NextTournament = null;
                Logger.Debug("Tournament started at {start}", DateTime.Now.ToString("G"));
                TournamentStarted?.Invoke(CurrentTournament.Date);
            }
        }

        /// <summary>
        /// Ends the current tournament, saves the results, and schedules the next one.
        /// </summary>
        public void EndTournament(bool broadcasting)
        {
            if (CurrentTournament != null)
            {
                CurrentTournament.SortResults();
                TournamentResults.Create(CurrentTournament);
                TournamentResults.Commit();
                FishingSystem.CastTimeMinimum = Settings.FishingCastMinimum;
                FishingSystem.CastTimeMaximum = Settings.FishingCastMaximum;
                DateTime? next;
                if (broadcasting)
                {
                    next = CurrentTournament.Date.AddMinutes(Settings.FishingTournamentDuration + Settings.FishingTournamentInterval);
                }
                else
                {
                    next = null;
                }
                NextTournament = next;
                Logger.Debug("Tournament ended at {end} with {count} entrants, won by {userId}", DateTime.Now.ToString("G"), CurrentTournament.Entries?.Count(), CurrentTournament.Winner?.UserId);
                foreach (var entry in CurrentTournament.Entries.OrderBy(x => x.Points))
                {
                    Logger.Debug("{userId} scored {points} points", entry?.UserId, entry?.Points);
                }
                TournamentEnded?.Invoke(CurrentTournament, next);
                CurrentTournament = null;
            }
        }

        /// <summary>
        /// Processes the tournament system, starting or ending the tournament as necessary.
        /// </summary>
        public Task Process(bool broadcasting)
        {
            if (!broadcasting)
            {
                if (CurrentTournament != null)
                {
                    Logger.Debug("Tournament active when broadcasting ended.");
                    EndTournament(broadcasting);
                }
                NextTournament = null;
            }
            else
            {
                if (CurrentTournament != null && DateTime.Now >= CurrentTournament.Date)
                {
                    Logger.Debug("Tournament time expired.");
                    EndTournament(broadcasting);
                }
                else if (CurrentTournament == null && DateTime.Now >= NextTournament)
                {
                    Logger.Debug("Tournament start time arrived.");
                    StartTournament();
                }
            }
            return Task.CompletedTask;
        }
    }
}
