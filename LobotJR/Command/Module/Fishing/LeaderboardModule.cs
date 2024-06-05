using LobotJR.Command.Model.Fishing;
using LobotJR.Command.System.Fishing;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.Fishing
{
    /// <summary>
    /// Module containing commands for retrieving leaderboards and managing
    /// personal fishing records.
    /// </summary>
    public class LeaderboardModule : ICommandModule
    {
        private readonly LeaderboardSystem TournamentSystem;
        private readonly UserSystem UserSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Fishing.Leaderboard";
        /// <summary>
        /// Invoke to send personal and global leaderboard update messages.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public LeaderboardModule(LeaderboardSystem system, UserSystem userSystem)
        {
            TournamentSystem = system;
            system.NewGlobalRecord += System_NewGlobalRecord;
            UserSystem = userSystem;
            Commands = new CommandHandler[]
            {
                new CommandHandler("PlayerLeaderboard", this, CommandMethod.GetInfo<int>(PlayerLeaderboard), CommandMethod.GetInfo<int>(PlayerLeaderboardCompact), "fish"),
                new CommandHandler("GlobalLeaderboard", this, CommandMethod.GetInfo(GlobalLeaderboard), CommandMethod.GetInfo(GlobalLeaderboardCompact), "fishleaders", "leaderboards", "fish-leaders"),
                new CommandHandler("ReleaseFish", this, CommandMethod.GetInfo<int>(ReleaseFish), "releasefish", "release-fish")
            };
        }

        private void System_NewGlobalRecord(IDatabase database, LeaderboardEntry catchData)
        {
            var recordMessage = $"{UserSystem.GetUserById(catchData.UserId).Username} just caught the heaviest {catchData.Fish.Name} ever! It weighs {catchData.Weight} pounds!";
            PushNotification?.Invoke(database, null, new CommandResult() { Messages = new string[] { recordMessage } });
        }

        public CompactCollection<Catch> PlayerLeaderboardCompact(IDatabase database, User user, int index = -1)
        {
            string selectFunc(Catch x) => $"{x.Fish.Name}|{x.Length}|{x.Weight};";
            var records = TournamentSystem.GetPersonalLeaderboard(user);
            if (index == -1)
            {
                if (records != null && records.Any())
                {
                    return new CompactCollection<Catch>(records, selectFunc);
                }
                return new CompactCollection<Catch>(new Catch[0], selectFunc);
            }
            else
            {
                var fish = records.ElementAtOrDefault(index - 1);
                if (fish != null)
                {
                    return new CompactCollection<Catch>(new Catch[] { fish }, selectFunc);
                }
                return null;
            }
        }

        public CommandResult PlayerLeaderboard(IDatabase database, User user, int index = -1)
        {
            var compact = PlayerLeaderboardCompact(database, user, index);
            var items = compact.Items.ToList();
            if (index == -1)
            {
                if (items.Count > 0)
                {
                    var responses = new List<string>
                    {
                        $"You've caught {items.Count} different types of fish: "
                    };
                    responses.AddRange(items.Select((x, i) => $"{i + 1}: {x.Fish.Name}"));
                    return new CommandResult(responses.ToArray());
                }
                else
                {
                    return new CommandResult($"You haven't caught any fish yet!");
                }
            }
            else
            {
                if (compact == null)
                {
                    var count = TournamentSystem.GetPersonalLeaderboard(user).Count();
                    return new CommandResult($"That fish doesn't exist. Fish # must be between 1 and {count}");
                }
                var fishCatch = compact.Items.FirstOrDefault();
                var responses = new List<string>
                {
                    $"Name - {fishCatch.Fish.Name}",
                    $"Length - {fishCatch.Length} in.",
                    $"Weight - {fishCatch.Weight} lbs.",
                    $"Size Category - {fishCatch.Fish.SizeCategory.Name}",
                    $"Description - {fishCatch.Fish.FlavorText}"
                };
                return new CommandResult(responses.ToArray());
            }
        }

        public CompactCollection<LeaderboardEntry> GlobalLeaderboardCompact(IDatabase database)
        {
            return new CompactCollection<LeaderboardEntry>(TournamentSystem.GetLeaderboard(), x => $"{x.Fish.Name}|{x.Length}|{x.Weight}|{UserSystem.GetUserById(x.UserId).Username};");
        }

        public CommandResult GlobalLeaderboard(IDatabase database)
        {
            var compact = GlobalLeaderboardCompact(database);
            return new CommandResult(compact.Items.Select(x => $"Largest {x.Fish.Name} caught by {UserSystem.GetUserById(x.UserId).Username} at {x.Weight} lbs., {x.Length} in.").ToArray());
        }

        public CommandResult ReleaseFish(IDatabase database, User user, int index)
        {
            var records = TournamentSystem.GetPersonalLeaderboard(user);
            if (records == null || !records.Any())
            {
                return new CommandResult("You don't have any fish! Type !cast to try and fish for some!");
            }
            var count = records.Count();
            if (count >= index && index > 0)
            {
                var fishName = records.ElementAtOrDefault(index - 1).Fish.Name;
                TournamentSystem.DeleteFish(user, index - 1);
                return new CommandResult($"You released your {fishName}. Bye bye!");
            }
            return new CommandResult($"That fish doesn't exist. Fish # must be between 1 and {count}");
        }
    }
}
