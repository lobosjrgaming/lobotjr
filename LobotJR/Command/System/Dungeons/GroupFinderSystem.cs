using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Dungeons
{
    /// <summary>
    /// System for managing the group finder.
    /// </summary>
    public class GroupFinderSystem : ISystemProcess
    {
        private readonly string DailyTimerName = "Daily Dungeon";
        private readonly Random random = new Random();
        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly PartySystem PartySystem;
        private readonly List<QueueEntry> GroupFinderQueue = new List<QueueEntry>();

        public DateTime LastGroupFormed { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// Event handler for events related to the dungeon finder queue.
        /// </summary>
        /// <param name="party">The newly created party.</param>
        public delegate void DungeonQueueHandler(Party party);
        /// <summary>
        /// Event fired when a player is added to a group through the dungeon
        /// finder queue.
        /// </summary>
        public event DungeonQueueHandler PartyFound;

        public GroupFinderSystem(ConnectionManager connectionManager, SettingsManager settingsManager, PartySystem partySystem)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            PartySystem = partySystem;
        }

        private bool IsViableParty(IEnumerable<QueueEntry> players)
        {
            var distribution = players.Select(x => x.Player).GroupBy(x => x.CharacterClass);
            return !distribution.Any(x => x.Count() > 2);
        }

        private IEnumerable<DungeonRun> GetGroupDungeons(IEnumerable<QueueEntry> players)
        {
            var dungeons = players.First().Dungeons;
            foreach (var player in players.Skip(1))
            {
                dungeons = dungeons.Intersect(player.Dungeons);
            }
            return dungeons;
        }

        private bool TryCreateParty(out Party party)
        {
            party = null;
            var settings = SettingsManager.GetGameSettings();
            if (GroupFinderQueue.Count() >= settings.DungeonPartySize)
            {
                for (var skip = 0; skip < GroupFinderQueue.Count - settings.DungeonPartySize; skip++)
                {
                    var group = GroupFinderQueue.Take(settings.DungeonPartySize - 1);
                    group.Concat(GroupFinderQueue.Skip(group.Count() + skip).Take(1));
                    if (IsViableParty(group))
                    {
                        var dungeons = GetGroupDungeons(group);
                        if (dungeons.Any())
                        {
                            var newParty = PartySystem.CreateParty(true, group.Select(x => x.Player).ToArray());
                            newParty.Run = random.RandomElement(dungeons);
                            party = newParty;
                            var leader = group.OrderByDescending(x => x.QueueTime).First();
                            PartySystem.SetLeader(party, leader.Player);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public TimeSpan GetLockoutTime(PlayerCharacter player)
        {
            var dailyTimer = ConnectionManager.CurrentConnection.DungeonTimerData.FirstOrDefault(x => x.Name.Equals(DailyTimerName));
            if (dailyTimer != null)
            {
                var lockout = ConnectionManager.CurrentConnection.DungeonLockouts.FirstOrDefault(x => x.UserId.Equals(player.UserId) && x.TimerId.Equals(dailyTimer.Id));
                if (lockout != null)
                {
                    if (dailyTimer.BaseTime.HasValue)
                    {
                        var rootTime = dailyTimer.BaseTime.Value;
                        var sinceRoot = DateTime.Now - rootTime;
                        var intervalsElapsed = Math.Floor(sinceRoot.TotalMinutes / dailyTimer.Length);
                        rootTime += TimeSpan.FromMinutes(intervalsElapsed * dailyTimer.Length);
                        if (rootTime < lockout.Time)
                        {
                            return (rootTime + TimeSpan.FromMinutes(dailyTimer.Length)) - lockout.Time;
                        }
                    }
                    else
                    {
                        if (DateTime.Now - lockout.Time < TimeSpan.FromMinutes(dailyTimer.Length))
                        {
                            return TimeSpan.FromMinutes(dailyTimer.Length) - (DateTime.Now - lockout.Time);
                        }
                    }
                }
            }
            return TimeSpan.Zero;
        }

        public QueueEntry GetPlayerQueueEntry(PlayerCharacter player)
        {
            return GroupFinderQueue.FirstOrDefault(x => x.Player.Equals(player));
        }

        public bool IsPlayerQueued(PlayerCharacter player)
        {
            return GetPlayerQueueEntry(player) != null;
        }

        public bool QueuePlayer(PlayerCharacter player, IEnumerable<DungeonRun> dungeons)
        {
            if (!IsPlayerQueued(player))
            {
                GroupFinderQueue.Add(new QueueEntry(player, dungeons));
                if (TryCreateParty(out var party))
                {
                    PartyFound?.Invoke(party);
                }
                return true;
            }
            return false;
        }

        public bool DequeuePlayer(PlayerCharacter player)
        {
            var entry = GetPlayerQueueEntry(player);
            if (entry != null)
            {
                GroupFinderQueue.Remove(entry);
                return true;
            }
            return false;
        }

        public IEnumerable<QueueEntry> GetQueueEntries()
        {
            return GroupFinderQueue.ToList();
        }

        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
