using LobotJR.Command.System.Dungeons;
using LobotJR.Command.System.Player;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LobotJR.Command.Module.Dungeons
{
    /// <summary>
    /// Module containing commands for the group finder.
    /// </summary>
    public class GroupFinderModule : ICommandModule
    {
        private readonly GroupFinderSystem GroupFinderSystem;
        private readonly DungeonSystem DungeonSystem;
        private readonly PartySystem PartySystem;
        private readonly PlayerSystem PlayerSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "GroupFinder";
        /// <summary>
        /// Invoked to notify players of group invitations, group chat, and
        /// dungeon progress.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public GroupFinderModule(GroupFinderSystem groupFinderSystem, DungeonSystem dungeonSystem, PartySystem partySystem, PlayerSystem playerSystem)
        {
            GroupFinderSystem = groupFinderSystem;
            DungeonSystem = dungeonSystem;
            PartySystem = partySystem;
            PlayerSystem = playerSystem;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DailyStatus", this, CommandMethod.GetInfo(DailyStatus), "daily"),
                new CommandHandler("EnterQueue", this, CommandMethod.GetInfo<string>(QueueForDungeonFinder), "queue"),
                new CommandHandler("LeaveQueue", this, CommandMethod.GetInfo(LeaveQueue), "leavequeue"),
                new CommandHandler("QueueTime", this, CommandMethod.GetInfo(GetQueueTime), "queuetime"),
                
                //This is an admin command
                new CommandHandler("QueueStatus", this, CommandMethod.GetInfo(QueueStatus), "queuestatus"),
            };
        }

        public CommandResult DailyStatus(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var remaining = GroupFinderSystem.GetLockoutTime(player);
            if (remaining.TotalMilliseconds > 0)
            {
                return new CommandResult($"Your daily Group Finder reward resets in {TimeSpan.FromSeconds(remaining.TotalSeconds).ToString("c")}.");
            }
            return new CommandResult("You are eligible for daily Group Finder rewards! Go queue up!");
        }

        public CommandResult QueueForDungeonFinder(User user, string dungeonIds)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            if (!PlayerSystem.IsFlaggedForRespec(player))
            {
                if (player.Level >= PlayerSystem.MinLevel)
                {
                    if (player.CharacterClass.CanPlay)
                    {
                        var party = PartySystem.GetCurrentGroup(player);
                        if (party == null)
                        {
                            var cost = DungeonSystem.GetDungeonCost(player);
                            if (player.Currency >= cost)
                            {
                                var dungeons = dungeonIds.Split(',').Select(x => DungeonSystem.ParseDungeonId(x.Trim()));
                                if (!dungeons.Any())
                                {
                                    dungeons = DungeonSystem.GetEligibleDungeons(player);
                                }
                                if (GroupFinderSystem.QueuePlayer(player, dungeons))
                                {
                                    return new CommandResult("You have been placed in the Group Finder queue.");
                                }
                                return new CommandResult("You are already queued in the Group Finder! Type !queuetime for more information.");
                            }
                            return new CommandResult($"You don't have enough money!");
                        }
                        if (party.PendingInvites.Any(x => x.Equals(player)))
                        {
                            return new CommandResult("You currently have an outstanding invite to another party. Couldn't create new party!");
                        }
                        return new CommandResult($"You already have a party created! {DungeonModule.PartyDescriptions[party.State]}");
                    }
                    return new CommandResult("You must select a class before you can queue in the Group Finder. Type !classhelp for details.");
                }
                return new CommandResult($"You must be level {PlayerSystem.MinLevel} to queue in the Group Finder. (Current level: {player.Level})");
            }
            return new CommandResult("You can't queue in the Group Finder with an outstanding respec. Choose a class or cancel your respec first.");
        }

        public CommandResult LeaveQueue(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            if (GroupFinderSystem.DequeuePlayer(player))
            {
                return new CommandResult("You were removed from the Group Finder.");
            }
            return new CommandResult("You are not queued in the Group Finder.");
        }

        private string ReadableTime(TimeSpan time)
        {
            if (time.TotalSeconds > 1)
            {
                var sb = new StringBuilder();
                if (time.TotalMinutes > 1)
                {
                    if (time.TotalHours > 1)
                    {
                        sb.Append($"{(int)Math.Floor(time.TotalHours)} hours, ");
                    }
                    sb.Append($"{(int)Math.Floor(time.TotalMinutes)} minutes, and ");
                }
                sb.Append($"{(int)Math.Floor(time.TotalSeconds)} seconds");
                return sb.ToString();
            }
            return "less than 1 second";
        }

        public CommandResult GetQueueTime(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var entry = GroupFinderSystem.GetPlayerQueueEntry(player);
            if (entry != null)
            {
                return new CommandResult(
                    $"You are queued for {entry.Dungeons.Count()} dungeons and have been waiting {ReadableTime(DateTime.Now - entry.QueueTime)}.",
                    $"The last group was formed {ReadableTime(DateTime.Now - GroupFinderSystem.LastGroupFormed)} ago.");
            }
            return new CommandResult("You are not queued in the Group Finder.");
        }

        public CommandResult QueueStatus(User user)
        {
            var entries = GroupFinderSystem.GetQueueEntries();
            var responses = new List<string>
            {
                $"There are {entries.Count()} players in queue."
            };
            var runs = entries.SelectMany(x => x.Dungeons).GroupBy(x => DungeonSystem.GetDungeonName(x));
            responses.AddRange(runs.Select(x => $"{x.Key}: {x.Count()}"));
            return new CommandResult(responses.ToArray());
        }
    }
}
