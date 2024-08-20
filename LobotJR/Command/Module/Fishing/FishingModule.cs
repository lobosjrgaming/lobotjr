using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Model.Fishing;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.Fishing
{
    /// <summary>
    /// Module containing commands used to fish.
    /// </summary>
    public class FishingModule : ICommandModule
    {
        private readonly FishingController FishingSystem;
        private readonly TournamentController TournamentSystem;
        private readonly LeaderboardController LeaderboardSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Fishing";
        /// <summary>
        /// Invoked to notify users of fish being hooked or getting away, and
        /// for notifying chat when a user sets a new record.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public FishingModule(FishingController fishingSystem, TournamentController tournamentSystem, LeaderboardController leaderboardSystem)
        {
            FishingSystem = fishingSystem;
            FishingSystem.FishHooked += FishingSystem_FishHooked;
            FishingSystem.FishGotAway += FishingSystem_FishGotAway;
            TournamentSystem = tournamentSystem;
            LeaderboardSystem = leaderboardSystem;
            Commands = new CommandHandler[]
            {
                new CommandHandler("CancelCast", this, CommandMethod.GetInfo(CancelCast), "cancelcast", "cancel-cast"),
                new CommandHandler("CatchFish", this, CommandMethod.GetInfo(CatchFish), "catch", "reel"),
                new CommandHandler("CastLine", this, CommandMethod.GetInfo(Cast), "cast"),
            };
        }

        private void FishingSystem_FishHooked(Fisher fisher)
        {
            var hookMessage = $"{fisher.Hooked.SizeCategory.Message} Type !catch to reel it in!";
            PushNotification?.Invoke(fisher.User, new CommandResult(fisher.User, hookMessage));
        }

        private void FishingSystem_FishGotAway(Fisher fisher)
        {
            PushNotification?.Invoke(fisher.User, new CommandResult(fisher.User, "Heck! The fish got away. Maybe next time..."));
        }

        public CommandResult CancelCast(User user)
        {
            var fisher = FishingSystem.GetFisherByUser(user);
            if (fisher.IsFishing)
            {
                FishingSystem.UnhookFish(fisher);
                return new CommandResult("You reel in the empty line.");
            }
            return new CommandResult("Your line has not been cast.");
        }

        public CommandResult CatchFish(User user)
        {
            var fisher = FishingSystem.GetFisherByUser(user);
            if (fisher.IsFishing)
            {
                var catchData = FishingSystem.CatchFish(fisher);
                if (catchData == null)
                {
                    return new CommandResult("Nothing is biting yet! To reset your cast, use !cancelcast");
                }

                if (TournamentSystem.IsRunning)
                {
                    var record = LeaderboardSystem.GetUserRecordForFish(user, catchData.Fish);
                    var responses = new List<string>();
                    if (record.Weight == catchData.Weight)
                    {
                        responses.Add($"This is the biggest {catchData.Fish.Name} you've ever caught!");
                    }
                    var userEntry = TournamentSystem.CurrentTournament.Entries.Where(x => x.UserId.Equals(user.TwitchId)).FirstOrDefault();
                    var sorted = TournamentSystem.CurrentTournament.Entries.OrderByDescending(x => x.Points).ToList().IndexOf(userEntry) + 1;
                    responses.Add($"You caught a {catchData.Length} inch, {catchData.Weight} pound {catchData.Fish.Name} worth {catchData.Points} points! You are in {sorted.ToOrdinal()} place with {userEntry.Points} total points.");
                    return new CommandResult(responses.ToArray());
                }
                else
                {
                    return new CommandResult($"Congratulations! You caught a {catchData.Length} inch, {catchData.Weight} pound {catchData.Fish.Name}!");
                }
            }
            return new CommandResult($"Your line has not been cast. Use !cast to start fishing");
        }

        public CommandResult Cast(User user)
        {
            var fisher = FishingSystem.GetFisherByUser(user);
            if (fisher.Hooked != null)
            {
                return new CommandResult("Something's already bit your line! Quick, type !catch to snag it!");
            }
            if (fisher.IsFishing)
            {
                return new CommandResult("Your line is already cast! I'm sure a fish'll be along soon...");
            }
            FishingSystem.Cast(user);
            return new CommandResult("You cast your line out into the water.");
        }
    }
}
