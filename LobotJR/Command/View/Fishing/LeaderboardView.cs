using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Fishing;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Fishing
{
    /// <summary>
    /// View containing commands for retrieving leaderboards and managing
    /// personal fishing records.
    /// </summary>
    public class LeaderboardView : ICommandView, IPushNotifier
    {
        private readonly LeaderboardController TournamentController;
        private readonly UserController UserController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Fishing.Leaderboard";
        /// <summary>
        /// Invoke to send personal and global leaderboard update messages.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public LeaderboardView(LeaderboardController tournamentController, UserController userController)
        {
            TournamentController = tournamentController;
            tournamentController.NewGlobalRecord += Controller_NewGlobalRecord;
            UserController = userController;
            Commands = new CommandHandler[]
            {
                new CommandHandler("PlayerLeaderboard", this, CommandMethod.GetInfo<int>(PlayerLeaderboard), CommandMethod.GetInfo<int>(PlayerLeaderboardCompact), "fish"),
                new CommandHandler("GlobalLeaderboard", this, CommandMethod.GetInfo(GlobalLeaderboard), CommandMethod.GetInfo(GlobalLeaderboardCompact), "fishleaders", "leaderboards", "fish-leaders"),
                new CommandHandler("ReleaseFish", this, CommandMethod.GetInfo<int>(ReleaseFish), "releasefish", "release-fish")
            };
        }

        private void Controller_NewGlobalRecord(LeaderboardEntry catchData)
        {
            var recordMessage = $"{UserController.GetUserById(catchData.UserId).Username} just caught the heaviest {catchData.Fish.Name} ever! It weighs {catchData.Weight} pounds!";
            PushNotification?.Invoke(null, new CommandResult() { Messages = new string[] { recordMessage } });
        }

        public CompactCollection<Catch> PlayerLeaderboardCompact(User user, int index = -1)
        {
            string selectFunc(Catch x) => $"{x.Fish.Name}|{x.Length}|{x.Weight};";
            var records = TournamentController.GetPersonalLeaderboard(user);
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

        public CommandResult PlayerLeaderboard(User user, int index = -1)
        {
            var compact = PlayerLeaderboardCompact(user, index);
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
                    var count = TournamentController.GetPersonalLeaderboard(user).Count();
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

        public CompactCollection<LeaderboardEntry> GlobalLeaderboardCompact()
        {
            return new CompactCollection<LeaderboardEntry>(TournamentController.GetLeaderboard(), x => $"{x.Fish.Name}|{x.Length}|{x.Weight}|{UserController.GetUserById(x.UserId).Username};");
        }

        public CommandResult GlobalLeaderboard()
        {
            var compact = GlobalLeaderboardCompact();
            return new CommandResult(compact.Items.Select(x => $"Largest {x.Fish.Name} caught by {UserController.GetUserById(x.UserId).Username} at {x.Weight} lbs., {x.Length} in.").ToArray());
        }

        public CommandResult ReleaseFish(User user, int index)
        {
            var records = TournamentController.GetPersonalLeaderboard(user);
            if (records == null || !records.Any())
            {
                return new CommandResult("You don't have any fish! Type !cast to try and fish for some!");
            }
            var count = records.Count();
            if (index > 0 && index <= count)
            {
                var fishName = records.ElementAtOrDefault(index - 1).Fish.Name;
                TournamentController.DeleteFish(user, index - 1);
                return new CommandResult($"You released your {fishName}. Bye bye!");
            }
            return new CommandResult($"That fish doesn't exist. Fish # must be between 1 and {count}");
        }
    }
}
