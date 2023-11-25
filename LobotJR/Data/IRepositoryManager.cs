using LobotJR.Command;
using LobotJR.Command.Model.Fishing;
using LobotJR.Twitch;

namespace LobotJR.Data
{
    /// <summary>
    /// Collection of repositories for data access.
    /// </summary>
    public interface IRepositoryManager
    {
        IRepository<Metadata> Metadata { get; }
        IRepository<AppSettings> AppSettings { get; }
        IRepository<DataTimer> DataTimers { get; }
        IRepository<Twitch.Model.User> Users { get; }
        IRepository<AccessGroup> UserRoles { get; }
        IRepository<Catch> Catches { get; }
        IRepository<LeaderboardEntry> FishingLeaderboard { get; }
        IRepository<TournamentResult> TournamentResults { get; }
        IRepository<TournamentEntry> TournamentEntries { get; }
    }
}
