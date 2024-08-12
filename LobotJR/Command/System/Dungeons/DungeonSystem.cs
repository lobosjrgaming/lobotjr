using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Player;
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
        private readonly PlayerSystem PlayerSystem;

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

        public DungeonSystem(IConnectionManager connectionManager, SettingsManager settingsManager, UserSystem userSystem, PlayerSystem playerSystem)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            UserSystem = userSystem;
            PlayerSystem = playerSystem;
        }

        public DungeonRun ParseDungeonId(string dungeonId)
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
                return new DungeonRun(dungeon, mode);
            }
            return null;
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

        public bool IsPlayerQueued(PlayerCharacter player)
        {
            return DungeonFinderQueue.Contains(player);
        }

        public Party GetCurrentGroup(PlayerCharacter player)
        {
            return DungeonGroups.FirstOrDefault(x => x.Members.Contains(player) || x.PendingInvites.Contains(player));
        }

        public Party CreateParty(bool isQueueGroup, params PlayerCharacter[] players)
        {
            var party = new Party(isQueueGroup, players);
            DungeonGroups.Add(party);
            return party;
        }

        public void DisbandParty(Party party)
        {
            DungeonGroups.Remove(party);
        }

        public bool IsLeader(Party party, PlayerCharacter player)
        {
            return party.Leader.Equals(player);
        }

        public bool SetLeader(Party party, PlayerCharacter player)
        {
            if (party.Members.Any(x => x.Equals(player)))
            {
                party.Members.Remove(player);
                party.Members.Insert(0, player);
                return true;
            }
            return false;
        }

        public bool InvitePlayer(Party party, PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            if (!party.Members.Contains(player)
                && !party.PendingInvites.Contains(player)
                && party.Members.Count + party.PendingInvites.Count < settings.DungeonPartySize)
            {
                party.PendingInvites.Add(player);
                return true;
            }
            return false;
        }

        public bool AcceptInvite(Party party, PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            if (party.PendingInvites.Contains(player)
                && party.Members.Count + party.PendingInvites.Count < settings.DungeonPartySize)
            {
                party.PendingInvites.Remove(player);
                AddPlayer(party, player);
                return true;
            }
            return false;
        }

        public bool DeclineInvite(Party party, PlayerCharacter player)
        {
            if (party.PendingInvites.Contains(player))
            {
                party.PendingInvites.Remove(player);
                return true;
            }
            return false;
        }

        public bool AddPlayer(Party party, PlayerCharacter player)
        {
            if (party.State == PartyState.Forming)
            {
                var settings = SettingsManager.GetGameSettings();
                if (party.Members.Count < settings.DungeonPartySize)
                {
                    party.Members.Add(player);
                    if (party.Members.Count == settings.DungeonPartySize)
                    {
                        party.State = PartyState.Full;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool RemovePlayer(Party party, PlayerCharacter player)
        {
            if (party.Members.Contains(player))
            {
                if (party.State != PartyState.Started && party.State != PartyState.Complete)
                {
                    party.Members.Remove(player);
                    if (party.Members.Count <= 0)
                    {
                        party.State = PartyState.Disbanded;
                    }
                    else
                    {
                        party.State = PartyState.Forming;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool SetReady(Party party)
        {
            if (party.State == PartyState.Forming && party.PendingInvites.Count == 0)
            {
                party.State = PartyState.Full;
                return true;
            }
            return false;
        }

        public bool UnsetReady(Party party)
        {
            var settings = SettingsManager.GetGameSettings();
            if (party.State == PartyState.Full && party.Members.Count < settings.DungeonPartySize)
            {
                party.State = PartyState.Forming;
                return true;
            }
            return false;
        }

        public bool CanStartDungeon(Party party)
        {
            return party.State == PartyState.Full;
        }

        public bool TryStartDungeon(Party party, DungeonRun run, out IEnumerable<PlayerCharacter> playersWithoutCoins)
        {
            if (run != null)
            {
                if (CanStartDungeon(party))
                {
                    var settings = SettingsManager.GetGameSettings();
                    var costs = party.Members.ToDictionary(x => x, x => settings.DungeonBaseCost + (x.Level - PlayerSystem.MinLevel) * settings.DungeonLevelCost);
                    playersWithoutCoins = costs.Where(x => x.Key.Currency < x.Value).Select(x => x.Key);
                    if (!playersWithoutCoins.Any())
                    {
                        foreach (var pair in costs)
                        {
                            pair.Key.Currency -= pair.Value;
                        }
                        party.Run = run;
                        party.State = PartyState.Started;
                        return true;
                    }
                }
            }
            playersWithoutCoins = Array.Empty<PlayerCharacter>();
            return false;
        }

        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
