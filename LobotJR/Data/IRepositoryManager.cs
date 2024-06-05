using LobotJR.Command;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System;

namespace LobotJR.Data
{
    /// <summary>
    /// Collection of dynamic data repositories.
    /// </summary>
    public interface IRepositoryManager : IDisposable
    {
        IRepository<Metadata> Metadata { get; }
        IRepository<AppSettings> AppSettings { get; }
        IRepository<DataTimer> DataTimers { get; }
        IRepository<User> Users { get; }
        IRepository<AccessGroup> AccessGroups { get; }
        IRepository<Enrollment> Enrollments { get; }
        IRepository<Restriction> Restrictions { get; }
        IRepository<Catch> Catches { get; }
        IRepository<LeaderboardEntry> FishingLeaderboard { get; }
        IRepository<TournamentResult> TournamentResults { get; }
        IRepository<TournamentEntry> TournamentEntries { get; }
        IRepository<PlayerCharacter> PlayerCharacters { get; }
        IRepository<Inventory> Inventories { get; }
        IRepository<Stable> Stables { get; }
        IRepository<DungeonLockout> DungeonLockouts { get; }
    }
}
