using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Dungeons
{
    /// <summary>
    /// View containing commands for the group finder.
    /// </summary>
    public class GroupFinderView : ICommandView, IPushNotifier
    {
        private readonly GroupFinderController GroupFinderController;
        private readonly DungeonController DungeonController;
        private readonly PartyController PartyController;
        private readonly PlayerController PlayerController;
        private readonly UserController UserController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "GroupFinder";
        /// <summary>
        /// Invoked to notify players of group invitations, group chat, and
        /// dungeon progress.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public GroupFinderView(GroupFinderController groupFinderController, DungeonController dungeonController, PartyController partyController, PlayerController playerController, UserController userController)
        {
            GroupFinderController = groupFinderController;
            DungeonController = dungeonController;
            PartyController = partyController;
            PlayerController = playerController;
            UserController = userController;
            GroupFinderController.PartyFound += GroupFinderController_PartyFound;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DailyStatus", this, CommandMethod.GetInfo(DailyStatus), "daily"),
                new CommandHandler("EnterQueue", this, CommandMethod.GetInfo<string>(QueueForDungeonFinder), "queue"),
                new CommandHandler("LeaveQueue", this, CommandMethod.GetInfo(LeaveQueue), "leavequeue"),
                new CommandHandler("QueueTime", this, CommandMethod.GetInfo(GetQueueTime), "queuetime"),
            };
        }

        private void GroupFinderController_PartyFound(Party party)
        {
            var users = party.Members.Select(x => UserController.GetUserById(x));
            foreach (var member in users)
            {
                var partyNames = string.Join(", ", users.Where(x => !x.TwitchId.Equals(member.TwitchId)).Select(x => DungeonView.GetPlayerName(party, x)));
                PushNotification?.Invoke(member, new CommandResult($"You've been matched for {DungeonController.GetDungeonName(party.DungeonId, party.ModeId)} with: {partyNames}. {DungeonController.GetDungeonName(party.DungeonId, party.ModeId)} will begin shortly."));
            }
            PushNotification?.Invoke(UserController.GetUserById(party.Leader), new CommandResult("You are the party leader."));
        }

        public CommandResult DailyStatus(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            var remaining = GroupFinderController.GetLockoutTime(player);
            if (remaining.TotalMilliseconds > 0)
            {
                return new CommandResult($"Your daily Group Finder reward resets in {(TimeSpan.FromSeconds(remaining.TotalSeconds)).ToReadableTime()}.");
            }
            return new CommandResult("You are eligible for daily Group Finder rewards! Go queue up!");
        }

        public CommandResult QueueForDungeonFinder(User user, string dungeonIds = "")
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (!PlayerController.IsFlaggedForRespec(player))
            {
                if (player.Level >= PlayerController.MinLevel)
                {
                    if (player.CharacterClass.CanPlay)
                    {
                        var party = PartyController.GetCurrentGroup(player);
                        if (party == null)
                        {
                            var cost = DungeonController.GetDungeonCost(player);
                            if (player.Currency >= cost)
                            {
                                var dungeons = dungeonIds.Split(',')
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .Select(x => DungeonController.ParseDungeonId(x.Trim()))
                                    .Where(x => x != null);
                                var list = dungeons.ToList();
                                if (!dungeons.Any())
                                {
                                    dungeons = DungeonController.GetEligibleDungeons(player);
                                }
                                if (!GroupFinderController.IsPlayerQueued(player))
                                {
                                    if (GroupFinderController.QueuePlayer(player, dungeons))
                                    {
                                        return new CommandResult(true);
                                    }
                                    return new CommandResult("You have been placed in the Group Finder queue.");
                                }
                                return new CommandResult("You are already queued in the Group Finder! Type !queuetime for more information.");
                            }
                            return new CommandResult($"You don't have enough money! It will cost you {DungeonController.GetDungeonCost(player)} Wolfcoins to run a dungeon.");
                        }
                        if (party.PendingInvites.Any(x => x.Equals(player.UserId)))
                        {
                            return new CommandResult("You currently have an outstanding invite to another party. Couldn't create new party!");
                        }
                        return new CommandResult($"You already have a party created! {DungeonView.PartyDescriptions[party.State]}");
                    }
                    return new CommandResult("You must select a class before you can queue in the Group Finder. Type !classhelp for details.");
                }
                return new CommandResult($"You must be level {PlayerController.MinLevel} to queue in the Group Finder. (Current level: {player.Level})");
            }
            return new CommandResult("You can't queue in the Group Finder with an outstanding respec. Choose a class or cancel your respec first.");
        }

        public CommandResult LeaveQueue(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (GroupFinderController.DequeuePlayer(player))
            {
                return new CommandResult("You were removed from the Group Finder.");
            }
            return new CommandResult("You are not queued in the Group Finder.");
        }

        public CommandResult GetQueueTime(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            var entry = GroupFinderController.GetPlayerQueueEntry(player);
            if (entry != null)
            {
                var lastFormed = "No groups have been formed recently.";
                if (GroupFinderController.LastGroupFormed != null)
                {
                    lastFormed = $"The last group was formed {(DateTime.Now - GroupFinderController.LastGroupFormed.Value).ToReadableTime()} ago.";
                }
                return new CommandResult(
                    $"You are queued for {entry.Dungeons.Count()} dungeons and have been waiting {(DateTime.Now - entry.QueueTime).ToReadableTime()}.",
                    lastFormed);
            }
            return new CommandResult("You are not queued in the Group Finder.");
        }
    }
}
