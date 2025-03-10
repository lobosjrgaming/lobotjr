﻿using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Dungeons
{
    /// <summary>
    /// View containing commands for forming groups and running dungeons.
    /// </summary>
    public class DungeonView : ICommandView, IPushNotifier
    {
        public static string GetPlayerName(Party party, User player)
        {
            return party.Leader.Equals(player.TwitchId) ? $"*{player.Username}*" : player.Username;
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static readonly Dictionary<PartyState, string> PartyDescriptions = new Dictionary<PartyState, string>()
        {
            { PartyState.Forming, "Party is currently forming. Add members with '!add <username>'" },
            { PartyState.Full, "Party is filled and ready to adventure! Type '!start' to begin!" },
            { PartyState.Started, "Your party is currently on an adventure!" },
            { PartyState.Complete, "Your party just finished an adventure!" },
            { PartyState.Failed, "Your party just finished an adventure!" },
        };

        private readonly DungeonController DungeonController;
        private readonly PartyController PartyController;
        private readonly GroupFinderController GroupFinderController;
        private readonly PlayerController PlayerController;
        private readonly UserController UserController;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Dungeons";
        /// <summary>
        /// Invoked to notify players of group invitations, group chat, and
        /// dungeon progress.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public DungeonView(DungeonController dungeonController, GroupFinderController groupFinderController, PlayerController playerController, PartyController partyController, UserController userController, ConfirmationController confirmationController, SettingsManager settingsManager)
        {
            DungeonController = dungeonController;
            GroupFinderController = groupFinderController;
            PlayerController = playerController;
            PartyController = partyController;
            UserController = userController;
            SettingsManager = settingsManager;
            confirmationController.Confirmed += ConfirmationController_Confirmed;
            confirmationController.Canceled += ConfirmationController_Canceled;
            DungeonController.DungeonError += DungeonController_DungeonError;
            DungeonController.DungeonStart += DungeonController_DungeonStart;
            DungeonController.DungeonProgress += DungeonController_DungeonProgress;
            DungeonController.DungeonFailure += DungeonController_DungeonFailure;
            DungeonController.PlayerDeath += DungeonController_PlayerDeath;
            DungeonController.DungeonComplete += DungeonController_DungeonComplete;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DungeonList", this, CommandMethod.GetInfo(ListDungeons), CommandMethod.GetInfo(ListDungeonsCompact), "dungeonlist"),
                new CommandHandler("DungeonDetails", this, CommandMethod.GetInfo<string>(DungeonDetails), "dungeon"),

                new CommandHandler("CreateParty", this, CommandMethod.GetInfo(CreateParty), "createparty"),
                new CommandHandler("AddPlayer", this, CommandMethod.GetInfo<string>(AddPlayer), "add", "invite"),
                new CommandHandler("RemovePlayer", this, CommandMethod.GetInfo<string>(RemovePlayer), "kick"),
                new CommandHandler("SetLeader", this, CommandMethod.GetInfo<string>(PromotePlayer), "promote"),
                new CommandHandler("LeaveParty", this, CommandMethod.GetInfo(LeaveParty), "leaveparty", "exitparty"),
                new CommandHandler("PartyChat", new CommandExecutor(this, CommandMethod.GetInfo<string>(PartyChat), true), "p", "party"),
                new CommandHandler("Members", this, CommandMethod.GetInfo(PartyMembers), "members"),

                new CommandHandler("Ready", this, CommandMethod.GetInfo(SetReady), "ready"),
                new CommandHandler("Unready", this, CommandMethod.GetInfo(UnsetReady), "unready"),
                new CommandHandler("Start", this, CommandMethod.GetInfo<string>(StartDungeon), "start"),
            };
        }

        private void DungeonController_DungeonError(Party party, IEnumerable<string> requeueIds)
        {
            if (party.IsQueueGroup)
            {
                foreach (var member in party.Members)
                {
                    if (requeueIds.Contains(member))
                    {
                        PushNotification?.Invoke(UserController.GetUserById(member), new CommandResult("A member of your party does not have enough Wolfcoins to begin the dungeon. You have been placed back in the queue."));
                    }
                    else
                    {
                        PushNotification?.Invoke(UserController.GetUserById(member), new CommandResult("You do not have enough Wolfcoins to begin the dungeon. You have been removed from the queue."));
                    }
                }
            }
            else
            {
                PushToParty(party, "An unspecified error has occurred while processing the dungeon. Your party has been disbanded.");
            }
        }

        private void DungeonController_DungeonStart(Party party)
        {
            var startMessage = $"Successfully initiated {DungeonController.GetDungeonName(party.DungeonId, party.ModeId)}!";
            var settings = SettingsManager.GetGameSettings();
            var members = party.Members.Select(x => $"{GetPlayerName(party, UserController.GetUserById(x))} ({DescribeClass(PlayerController.GetPlayerByUserId(x))})");
            var memberMessage = $"Your party consists of: {string.Join(", ", members)}";
            foreach (var member in party.Members)
            {
                var memberPlayer = PlayerController.GetPlayerByUserId(member);
                var cost = DungeonController.GetDungeonCost(memberPlayer, settings);
                PushNotification?.Invoke(UserController.GetUserById(member), new CommandResult($"{startMessage} {cost} Wolfcoins deducted ({memberPlayer.Currency} remaining).", memberMessage));
            }
        }

        private void DungeonController_DungeonComplete(PlayerCharacter player, int experience, int currency, Item loot, bool wasQueueGroup, bool groupFinderBonus, bool critBonus)
        {
            var user = PlayerController.GetUserByPlayer(player);
            var messages = new List<string>();
            if (!wasQueueGroup)
            {
                messages.Add("Dungeon complete. Your party remains intact.");
            }
            if (groupFinderBonus)
            {
                messages.Add("You earned double rewards for completing a daily Group Finder dungeon! Queue up again in 24 hours to receive the 2x bonus again! (You can whisper me '!daily' for a status.)");
            }
            if (critBonus)
            {
                var settings = SettingsManager.GetGameSettings();
                var percentBonus = (int)Math.Round(settings.DungeonCritBonus * 100);
                messages.Add($"It was a critical success! You earned a {percentBonus}% bonus to experience!");
            }
            messages.Add($"{user.Username}, you've earned {experience} XP and {currency} Wolfcoins for completing the dungeon!");
            if (loot != null)
            {
                messages.Add($"You looted {loot.Name}!");
            }
            if (wasQueueGroup)
            {
                messages.Add("You completed a group finder dungeon. Type !queue to join another group!");
            }
            PushNotification?.Invoke(user, new CommandResult(messages.ToArray()));
        }

        private void DungeonController_DungeonFailure(Party party, IEnumerable<PlayerCharacter> deceased)
        {
            var deadUsers = deceased.Select(x => PlayerController.GetUserByPlayer(x));
            if (deceased.Any())
            {
                PushToParty(party, $"In the chaos, {string.Join(", and ", deadUsers.Select(x => x.Username))} lost their life. Seek vengeance in their honor!", deceased.Select(x => x.UserId).ToArray());
            }
            PushToParty(party, "It's a sad thing your adventure has ended here. No XP or Coins have been awarded.");
        }

        private void DungeonController_PlayerDeath(PlayerCharacter player, int experienceLost, int currencyLost)
        {
            PushNotification?.Invoke(PlayerController.GetUserByPlayer(player), new CommandResult($"Sadly, you have died. You lost {experienceLost} XP and {currencyLost} Coins."));
        }

        private void DungeonController_DungeonProgress(Party party, string result)
        {
            PushToParty(party, result);
        }

        private void ConfirmationController_Confirmed(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            var party = PartyController.GetCurrentGroup(player);
            if (party != null)
            {
                if (PartyController.AcceptInvite(party, player))
                {
                    var members = party.Members.Where(x => !x.Equals(player)).Select(x => GetPlayerName(party, UserController.GetUserById(x)));
                    PushNotification?.Invoke(user, new CommandResult($"You successfully joined a party with the following members: {string.Join(", ", members)}"));
                    var settings = SettingsManager.GetGameSettings();
                    PushToParty(party, $"{user.Username}, level {player.Level} {player.CharacterClass.Name} has joined your party ({party.Members.Count}/{settings.DungeonPartySize})", player.UserId);
                    if (party.State == PartyState.Full)
                    {
                        PushNotification?.Invoke(UserController.GetUserById(party.Leader), new CommandResult($"You've reached {settings.DungeonPartySize} party members! You're ready to dungeon!"));
                    }
                    Logger.Info("{user} accepted invite to group with {members}", user.Username, string.Join(", ", members));
                }
            }
        }

        private void ConfirmationController_Canceled(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            var party = PartyController.GetCurrentGroup(player);
            if (party != null)
            {
                PartyController.DeclineInvite(party, player);
                var leader = UserController.GetUserById(party.Leader);
                PushNotification?.Invoke(user, new CommandResult($"You declined {leader.Username}'s invite."));
                PushNotification?.Invoke(leader, new CommandResult($"{user.Username} has declined your party invite."));
                Logger.Info("{user} declined party invite from {leader}.", user.Username, leader.Username);
            }
        }

        private bool CanExecuteCommand(User user, bool requiresLeader, out PlayerCharacter player, out Party party, out string errorMessage)
        {
            errorMessage = "Party commands are not available while in the dungeon finder queue. Use !leavequeue to exit the queue.";
            party = null;
            player = PlayerController.GetPlayerByUser(user);
            if (!GroupFinderController.IsPlayerQueued(player))
            {
                party = PartyController.GetCurrentGroup(player);
                if (party != null)
                {
                    if (!requiresLeader || party.Leader.Equals(player.UserId))
                    {
                        return true;
                    }
                    errorMessage = "You are not the party leader.";
                    return false;
                }
                errorMessage = "You are not in a party.";
            }
            return false;
        }

        private void PushToParty(Party party, string message, params string[] skip)
        {
            foreach (var member in party.Members.Except(skip))
            {
                PushNotification?.Invoke(UserController.GetUserById(member), new CommandResult(message));
            }
        }

        public CommandResult ListDungeons()
        {
            return new CommandResult("List of Wolfpack RPG Adventures: http://tinyurl.com/WolfpackAdventureList");
        }

        public CompactCollection<string> ListDungeonsCompact()
        {
            var runs = DungeonController.GetAllDungeons().GroupBy(x => x.DungeonId, x => x.ModeId, (key, group) => $"{DungeonController.GetDungeonById(key).Name}|{key}|{string.Join(",", group.Select(y => DungeonController.GetModeById(y)).Select(y => y.IsDefault ? "" : y.Flag))};");
            return new CompactCollection<string>(runs, x => x);
        }

        public CommandResult DungeonDetails(string id)
        {
            var dungeonData = DungeonController.ParseDungeonId(id);
            if (dungeonData != null)
            {
                var name = DungeonController.GetDungeonName(dungeonData.DungeonId, dungeonData.ModeId);
                var dungeon = DungeonController.GetDungeonById(dungeonData.DungeonId);
                var range = dungeon.LevelRanges.FirstOrDefault(x => x.ModeId == dungeonData.ModeId);
                if (range != null)
                {
                    return new CommandResult($"{name} (Levels {range.Minimum} - {range.Maximum}) -- {dungeon.Description}");
                }
                return new CommandResult($"This dungeon is missing level range data, please contact the streamer to let them know.");
            }
            return new CommandResult($"Invalid Dungeon ID provided.");
        }

        public CommandResult CreateParty(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (!GroupFinderController.IsPlayerQueued(player))
            {
                var currentParty = PartyController.GetCurrentGroup(player);
                if (currentParty == null)
                {
                    if (player.Level >= PlayerController.MinLevel)
                    {
                        if (player.CharacterClass.CanPlay)
                        {
                            PartyController.CreateParty(false, player);
                            Logger.Info("Party Created by {user}", user.Username);
                            Logger.Info("Total number of parties: {count}", PartyController.PartyCount);
                            return new CommandResult("Party created! Use '!add <username>' to invite party members.");
                        }
                        return new CommandResult($"You are high enough level but haven't picked a class! Choose by whispering me one of the following:",
                            string.Join(", ", PlayerController.GetPlayableClasses().Select(x => $"!C{x.Id - 1} ({x.Name})")));
                    }
                    return new CommandResult($"You must be level {PlayerController.MinLevel} or higher to create a party (current level: {player.Level}).");
                }
                else if (currentParty.Leader.Equals(player))
                {
                    return new CommandResult($"You already have a party created! {PartyDescriptions[currentParty.State]}");
                }

                var leader = UserController.GetUserById(currentParty.Leader)?.Username;
                if (currentParty.PendingInvites.Contains(player.UserId))
                {
                    return new CommandResult($"You currently have an outstanding invite to another party from {leader}. Couldn't create new party!");
                }
                return new CommandResult($"You already have a party created! Your party leader is {leader}, Use !p <message> to talk to them.");
            }
            return new CommandResult("Can't create a party while queued with the Group Finder. Message me '!leavequeue' to exit.");
        }

        public CommandResult AddPlayer(User user, string playerName)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (!GroupFinderController.IsPlayerQueued(player))
            {
                var targetUser = UserController.GetUserByName(playerName);
                if (targetUser != null)
                {
                    if (!targetUser.Equals(user))
                    {
                        var targetPlayer = PlayerController.GetPlayerByUser(targetUser);
                        if (targetPlayer.Level >= 3)
                        {
                            if (targetPlayer.CharacterClass.CanPlay)
                            {
                                if (!GroupFinderController.IsPlayerQueued(targetPlayer) && PartyController.GetCurrentGroup(targetPlayer) == null)
                                {
                                    var currentParty = PartyController.GetCurrentGroup(player) ?? PartyController.CreateParty(false, player);
                                    if (currentParty.Leader.Equals(player.UserId))
                                    {
                                        var settings = SettingsManager.GetGameSettings();
                                        if (currentParty.State == PartyState.Forming || currentParty.State == PartyState.Full)
                                        {
                                            if (PartyController.InvitePlayer(currentParty, targetPlayer))
                                            {
                                                PushNotification?.Invoke(targetUser, new CommandResult($"{user.Username}, Level {player.Level} {player.CharacterClass.Name}, has invited you to join a party. Accept? (!y/!n)"));
                                                PushToParty(currentParty, $"{targetUser.Username} was invited to your group.", player.UserId);
                                                return new CommandResult($"You invited {targetUser.Username} to your group.");
                                            }
                                            return new CommandResult($"You can't have more than {settings.DungeonPartySize} party members in your group, including pending invites.");
                                        }
                                        return new CommandResult("Your party is unable to add members at this time.");
                                    }
                                    return new CommandResult($"Only the party leader ({UserController.GetUserById(currentParty.Leader).Username}) can invite members.");
                                }
                                return new CommandResult($"{targetUser.Username} is already in a group, or in the dungeon finder queue.");
                            }
                            PushNotification?.Invoke(targetUser, new CommandResult("Someone tried to invite you to a group, but you haven't picked a class yet! Type !classhelp for details."));
                            return new CommandResult($"{targetUser.Username} is high enough level, but has not picked a class!");
                        }
                        return new CommandResult($"{targetUser.Username} is not high enough level. ({targetPlayer.Level})");
                    }
                    return new CommandResult("You can't invite yourself to a group!");
                }
                return new CommandResult($"Can't find user {playerName}. Please check the spelling and try again.");
            }
            return new CommandResult("Can't create a party while queued with the Group Finder. Message me '!leavequeue' to exit.");
        }

        public CommandResult RemovePlayer(User user, string playerName)
        {
            var canExecute = CanExecuteCommand(user, true, out var player, out var party, out var errorMessage);
            if (canExecute)
            {

                if (!playerName.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
                {
                    if (party.State == PartyState.Forming || party.State == PartyState.Full)
                    {
                        var targetUser = UserController.GetUserByName(playerName);
                        if (targetUser != null)
                        {
                            if (party.Members.Contains(targetUser.TwitchId))
                            {
                                PushNotification.Invoke(targetUser, new CommandResult($"You were removed from {user.Username}'s party."));
                                party.Members.Remove(targetUser.TwitchId);
                                if (party.Members.Count > 1)
                                {
                                    PushToParty(party, $"{targetUser.Username} was removed from the party.", player.UserId);
                                    return new CommandResult($"{targetUser.Username} was removed from the party.");
                                }
                                PartyController.DisbandParty(party);
                                return new CommandResult("Your party has been disbanded.");
                            }
                        }
                        return new CommandResult("Couldn't find that party member to remove.");
                    }
                    return new CommandResult("You can't kick a party member in the middle of a dungeon!");
                }
                return new CommandResult("You can't kick yourself from a group! Do !leaveparty instead.");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult PromotePlayer(User user, string playerName)
        {
            var canExecute = CanExecuteCommand(user, true, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                if (!playerName.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var targetUser = UserController.GetUserByName(playerName);
                    if (targetUser != null)
                    {
                        if (party.Members.Contains(targetUser.TwitchId))
                        {
                            PartyController.SetLeader(party, targetUser.TwitchId);
                            PushNotification?.Invoke(targetUser, new CommandResult($"{user.Username} has promoted you to Party Leader."));
                            PushToParty(party, $"{user.Username} has promoted {targetUser.Username} to Party Leader.", user.TwitchId, targetUser.TwitchId);
                            return new CommandResult($"You have promoted {targetUser.Username} to Party Leader.");
                        }
                    }
                    return new CommandResult($"Party member '{playerName}' not found. You are still party leader.");
                }
                return new CommandResult("You are already the party leader!");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult LeaveParty(User user)
        {
            var canExecute = CanExecuteCommand(user, false, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                if (party.State == PartyState.Forming || party.State == PartyState.Full)
                {
                    var wasLeader = party.Leader.Equals(player.UserId);
                    PartyController.RemovePlayer(party, player);
                    if (party.IsQueueGroup)
                    {
                        var toRequeue = party.QueueEntries.Where(x => !x.UserId.Equals(user.TwitchId));
                        foreach (var member in toRequeue)
                        {
                            PushNotification?.Invoke(UserController.GetUserById(member.UserId), new CommandResult($"{user.Username} has left the party, you have been returned to the queue."));
                            GroupFinderController.QueuePlayer(member);
                        }
                    }
                    else if (party.Members.Count > 1)
                    {
                        if (wasLeader)
                        {
                            var newLeader = UserController.GetUserById(party.Leader);
                            PushNotification?.Invoke(newLeader, new CommandResult($"{user.Username} has left the party, you have been promoted to party leader."));
                            PushToParty(party, $"{user.Username} has left the party.", party.Leader);
                        }
                        else
                        {
                            PushToParty(party, $"{user.Username} has left the party.");
                        }
                    }
                    else
                    {
                        PushToParty(party, $"{user.Username} has left the party, your party has been disbanded.");
                        PartyController.DisbandParty(party);
                    }
                    return new CommandResult($"You left the party.");
                }
                return new CommandResult("You can't leave your party while a dungeon is in progress!");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult PartyChat(User user, string message)
        {
            var canExecute = CanExecuteCommand(user, false, out var _, out var party, out var errorMessage);
            if (canExecute)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    PushToParty(party, $"{user.Username} says: \"{message}\"", user.TwitchId);
                    return new CommandResult($"You whisper: \"{message}\"");
                }
                return new CommandResult();
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult PartyMembers(User user)
        {
            var canExecute = CanExecuteCommand(user, false, out var _, out var party, out var errorMessage);
            if (canExecute)
            {
                var names = party.Members.Where(x => !x.Equals(user.TwitchId)).Select(x => UserController.GetUserById(x).Username);
                if (names.Any())
                {
                    return new CommandResult($"You are in a party with {string.Join(", ", names)}.");
                }
                return new CommandResult("You are in a party by yourself.");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult SetReady(User user)
        {
            var canExecute = CanExecuteCommand(user, true, out _, out var party, out var errorMessage);
            if (canExecute)
            {
                var success = PartyController.SetReady(party);
                if (success)
                {
                    return new CommandResult("Party set to 'Ready'. Be careful adventuring without a full party!");
                }
                else if (party.PendingInvites.Count > 0)
                {
                    return new CommandResult("One or more members have not accepted their invitation.");
                }
                return new CommandResult("Your party can't be readied at this time.");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult UnsetReady(User user)
        {
            var canExecute = CanExecuteCommand(user, true, out _, out var party, out var errorMessage);
            if (canExecute)
            {
                var success = PartyController.UnsetReady(party);
                if (success)
                {
                    return new CommandResult("Party 'Ready' status has been revoked.");
                }
                return new CommandResult("Your party status can't be changed at this time.");
            }
            return new CommandResult(errorMessage);
        }

        private string DescribeClass(PlayerCharacter player)
        {
            return $"Level {player.Level} {player.CharacterClass.Name}";
        }

        public CommandResult StartDungeon(User user, string dungeonId = "")
        {
            var canExecute = CanExecuteCommand(user, true, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                var dungeon = -1;
                var mode = -1;
                if (string.IsNullOrWhiteSpace(dungeonId))
                {
                    DungeonController.SelectDungeon(party, out dungeon, out mode);
                }
                else
                {
                    var run = DungeonController.ParseDungeonId(dungeonId);
                    dungeon = run?.DungeonId ?? -1;
                    mode = run?.ModeId ?? -1;
                }
                if (dungeon > 0 && mode > 0)
                {
                    var success = DungeonController.TryStartDungeon(party, dungeon, mode, out var broke);
                    if (success)
                    {
                        // No need to send a response, the dungeon starting kicks those messages off automatically
                        return new CommandResult();
                    }
                    if (broke.Any())
                    {
                        PushToParty(party, $"The following party members do not have enough money to run a dungeon: {string.Join(", ", broke.Select(x => GetPlayerName(party, PlayerController.GetUserByPlayer(x))))}");
                        return new CommandResult();
                    }
                    return new CommandResult("You don't have enough members to start a dungeon. Use !invite to add players, or !ready to enable dungeons with with a partial party.");
                }
                return new CommandResult("Invalid Dungeon ID provided.");
            }
            return new CommandResult(errorMessage);
        }
    }
}
