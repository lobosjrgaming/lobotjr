using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Dungeons
{
    /// <summary>
    /// System for managing the group finder.
    /// </summary>
    public class GroupFinderSystem : ISystem
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly List<QueueEntry> GroupFinderQueue = new List<QueueEntry>();

        public DateTime LastGroupFormed { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// Event handler for events related to the dungeon finder queue.
        /// </summary>
        /// <param name="player">The player object for the user that the event
        /// happened to.</param>
        public delegate void DungeonQueueHandler(PlayerCharacter player);
        /// <summary>
        /// Event fired when a player is added to a group through the dungeon
        /// finder queue.
        /// </summary>
        public event DungeonQueueHandler PartyFound;

        public GroupFinderSystem(ConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        public TimeSpan GetLockoutTime(PlayerCharacter player, string lockoutName)
        {
            var dailyTimer = ConnectionManager.CurrentConnection.DungeonTimerData.FirstOrDefault(x => x.Name.Equals(lockoutName));
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
            throw new global::System.NotImplementedException();
        }
    }
}
