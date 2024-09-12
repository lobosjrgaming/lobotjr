using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Controller.Dungeons
{
    /// <summary>
    /// Controller for managing the group finder.
    /// </summary>
    public class GroupFinderController
    {
        public static string DailyTimerName { get; private set; } = "Daily Dungeon";
        private readonly Random random = new Random();
        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly PlayerController PlayerController;
        private readonly PartyController PartyController;
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

        public GroupFinderController(IConnectionManager connectionManager, SettingsManager settingsManager, PlayerController playerController, PartyController partyController)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            PlayerController = playerController;
            PartyController = partyController;
        }

        private bool IsViableParty(IEnumerable<QueueEntry> players)
        {
            var distribution = players.Select(x => x.UserId).GroupBy(x => PlayerController.GetPlayerByUserId(x).CharacterClass);
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
                for (var skip = 0; skip <= GroupFinderQueue.Count - settings.DungeonPartySize; skip++)
                {
                    var group = GroupFinderQueue.Take(settings.DungeonPartySize - 1);
                    var next = GroupFinderQueue.Skip(group.Count() + skip).Take(1);
                    group = group.Concat(next);
                    if (IsViableParty(group))
                    {
                        var dungeons = GetGroupDungeons(group);
                        if (dungeons.Any())
                        {
                            var newParty = PartyController.CreateParty(true, group.Select(x => x.UserId).ToArray());
                            newParty.SetQueueTimes(group.ToDictionary(x => x.UserId, x => (int)Math.Floor((DateTime.Now - x.QueueTime).TotalSeconds)));
                            var toRun = random.RandomElement(dungeons);
                            newParty.DungeonId = toRun.DungeonId;
                            newParty.ModeId = toRun.ModeId;
                            party = newParty;
                            party.State = PartyState.Full;
                            var leader = group.OrderByDescending(x => x.QueueTime).First();
                            PartyController.SetLeader(party, leader.UserId);
                            foreach (var entry in group.ToList())
                            {
                                GroupFinderQueue.Remove(entry);
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Resets the group finder queue, removing all queue entries.
        /// </summary>
        public void ResetQueue()
        {
            GroupFinderQueue.Clear();
        }

        /// <summary>
        /// Gets the time until the next Group Finder bonus is available for a
        /// given player.
        /// </summary>
        /// <param name="player">The player to get the lockout time for.</param>
        /// <returns>The time until the player can receive the Group Finder
        /// bonus.</returns>
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

        /// <summary>
        /// Sets the lockout time for a player to the current time.
        /// </summary>
        /// <param name="player">The user id of the player to update.</param>
        public void SetLockout(PlayerCharacter player)
        {
            var dailyTimer = ConnectionManager.CurrentConnection.DungeonTimerData.FirstOrDefault(x => x.Name.Equals(DailyTimerName));
            if (dailyTimer != null)
            {
                var lockout = ConnectionManager.CurrentConnection.DungeonLockouts.FirstOrDefault(x => x.UserId.Equals(player.UserId) && x.TimerId.Equals(dailyTimer.Id))
                    ?? ConnectionManager.CurrentConnection.DungeonLockouts.Create(new DungeonLockout()
                    {
                        UserId = player.UserId,
                        Timer = dailyTimer,
                    });
                lockout.Time = DateTime.Now;
            }
        }

        /// <summary>
        /// Gets the queue entry for a player, or null if they are not in the
        /// queue.
        /// </summary>
        /// <param name="player">The player to get the queue entry for.</param>
        /// <returns>A queue entry object for that player.</returns>
        public QueueEntry GetPlayerQueueEntry(PlayerCharacter player)
        {
            return GroupFinderQueue.FirstOrDefault(x => x.UserId.Equals(player.UserId));
        }

        /// <summary>
        /// Checks if a player is in the queue.
        /// </summary>
        /// <param name="player">The player to check queue status of.</param>
        /// <returns>True if the player is in the group finder queue.</returns>
        public bool IsPlayerQueued(PlayerCharacter player)
        {
            return GetPlayerQueueEntry(player) != null;
        }

        /// <summary>
        /// Adds a player to the group finder queue.
        /// </summary>
        /// <param name="player">The player to add.</param>
        /// <param name="dungeons">A collection of dungeons to queue the player for.</param>
        /// <returns>True if the player was placed into a party with other
        /// queued players.</returns>
        public bool QueuePlayer(PlayerCharacter player, IEnumerable<DungeonRun> dungeons)
        {
            GroupFinderQueue.Add(new QueueEntry(player, dungeons.ToList()));
            if (TryCreateParty(out var party))
            {
                PartyFound?.Invoke(party);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a player from the group finder queue.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        /// <returns>True if the player was removed from the queue. False if
        /// they were not in the queue.</returns>
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

        /// <summary>
        /// Gets all group finder queue entries.
        /// </summary>
        /// <returns>A collection of queue entries.</returns>
        public IEnumerable<QueueEntry> GetQueueEntries()
        {
            return GroupFinderQueue.ToList();
        }
    }
}
