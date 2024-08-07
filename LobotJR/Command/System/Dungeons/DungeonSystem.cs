using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Dungeons
{
    /// <summary>
    /// System for managing player parties and running dungeons.
    /// </summary>
    public class DungeonSystem : ISystem
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly UserSystem UserSystem;

        private readonly List<PlayerCharacter> DungeonFinderQueue = new List<PlayerCharacter>();
        private readonly List<Party> DungeonGroups = new List<Party>();

        /// <summary>
        /// Gets the number of dungeon groups.
        /// </summary>
        public int PartyCount { get { return DungeonGroups.Count; } }

        /// <summary>
        /// Event handler for events related to completing a dungeon.
        /// </summary>
        /// <param name="player">The player object for the user that completed
        /// the dungeon.</param>
        /// <param name="result">The object containing the results of the
        /// dungeon.</param>
        public delegate void DungeonCompleteHandler(PlayerCharacter player, object result);
        /// <summary>
        /// Event fired when a player completes a dungeon, regardless of the
        /// outcome.
        /// </summary>
        public event DungeonCompleteHandler DungeonComplete;
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

        public DungeonSystem(IConnectionManager connectionManager, SettingsManager settingsManager, UserSystem userSystem)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            UserSystem = userSystem;
        }

        public (Dungeon Dungeon, DungeonMode Mode) ParseDungeonId(string dungeonId)
        {
            var modes = ConnectionManager.CurrentConnection.DungeonModeData.Read().ToArray();
            var defaultMode = modes.FirstOrDefault(x => x.IsDefault);
            var selectedMode = modes.FirstOrDefault(x => dungeonId.EndsWith(x.Flag));
            var mode = selectedMode ?? defaultMode;
            var id = dungeonId;
            if (selectedMode != null)
            {
                id = dungeonId.Substring(0, dungeonId.Length - selectedMode.Flag.Length);
            }
            if (int.TryParse(id, out var idNumber) && mode != null)
            {
                var dungeon = ConnectionManager.CurrentConnection.DungeonData.FirstOrDefault(x => x.Id == idNumber);
                return (dungeon, mode);
            }
            return (null, null);
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

        public Party CreateParty(bool isQueueGroup, params PlayerCharacter[] players)
        {
            var party = new Party(SettingsManager.GetGameSettings().DungeonPartySize, isQueueGroup, players);
            DungeonGroups.Add(party);
            return party;
        }

        public void DisbandParty(Party party)
        {
            DungeonGroups.Remove(party);
        }

        public bool IsPlayerQueued(PlayerCharacter player)
        {
            return DungeonFinderQueue.Contains(player);
        }

        public Party GetCurrentGroup(PlayerCharacter player)
        {
            return DungeonGroups.FirstOrDefault(x => x.Members.Contains(player) || x.PendingInvites.Contains(player));
        }

        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
