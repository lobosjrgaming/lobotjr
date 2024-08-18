using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Dungeons;
using LobotJR.Command.System.General;
using LobotJR.Command.System.Player;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.Dungeons
{
    /// <summary>
    /// Module containing commands for forming groups and running dungeons.
    /// </summary>
    public class DungeonModule : ICommandModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Random Random = new Random();
        public static readonly Dictionary<PartyState, string> PartyDescriptions = new Dictionary<PartyState, string>()
        {
            { PartyState.Forming, "Party is currently forming. Add members with '!add <username>'" },
            { PartyState.Full, "Party is filled and ready to adventure! Type '!start' to begin!" },
            { PartyState.Started, "Your party is currently on an adventure!" },
            { PartyState.Complete, "Your party just finished an adventure!" },
            { PartyState.Failed, "Your party just finished an adventure!" },
        };

        private readonly DungeonSystem DungeonSystem;
        private readonly PartySystem PartySystem;
        private readonly GroupFinderSystem GroupFinderSystem;
        private readonly PlayerSystem PlayerSystem;
        private readonly UserSystem UserSystem;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Dungeons";
        /// <summary>
        /// Invoked to notify players of group invitations, group chat, and
        /// dungeon progress.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public DungeonModule(DungeonSystem dungeonSystem, PartySystem partySystem, GroupFinderSystem groupFinderSystem, PlayerSystem playerSystem, UserSystem userSystem, ConfirmationSystem confirmationSystem, SettingsManager settingsManager)
        {
            DungeonSystem = dungeonSystem;
            GroupFinderSystem = groupFinderSystem;
            PlayerSystem = playerSystem;
            UserSystem = userSystem;
            SettingsManager = settingsManager;
            confirmationSystem.Confirmed += ConfirmationSystem_Confirmed;
            confirmationSystem.Canceled += ConfirmationSystem_Canceled;
            DungeonSystem.DungeonProgress += DungeonSystem_DungeonProgress;
            DungeonSystem.DungeonFailure += DungeonSystem_DungeonFailure;
            DungeonSystem.PlayerDeath += DungeonSystem_PlayerDeath;
            DungeonSystem.DungeonComplete += DungeonSystem_DungeonComplete;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DungeonList", this, CommandMethod.GetInfo(ListDungeons), "dungeonlist"),
                new CommandHandler("DungeonDetails", this, CommandMethod.GetInfo<string>(DungeonDetails), "dungeon"),

                new CommandHandler("CreateParty", this, CommandMethod.GetInfo(CreateParty), "createparty"),
                new CommandHandler("AddPlayer", this, CommandMethod.GetInfo<string>(AddPlayer), "add", "invite"),
                new CommandHandler("RemovePlayer", this, CommandMethod.GetInfo<string>(RemovePlayer), "kick"),
                new CommandHandler("SetLeader", this, CommandMethod.GetInfo<string>(PromotePlayer), "promote"),
                new CommandHandler("LeaveParty", this, CommandMethod.GetInfo(LeaveParty), "leaveparty", "exitparty"),
                new CommandHandler("PartyData", this, CommandMethod.GetInfo(PartyData), "partydata"),
                new CommandHandler("PartyChat", new CommandExecutor(this, CommandMethod.GetInfo<string>(PartyChat), true), "p", "party"),

                new CommandHandler("Ready", this, CommandMethod.GetInfo(SetReady), "ready"),
                new CommandHandler("Unready", this, CommandMethod.GetInfo(UnsetReady), "unready"),
                new CommandHandler("Start", this, CommandMethod.GetInfo<string>(StartDungeon), "start"),
            };
        }

        private void DungeonSystem_DungeonComplete(PlayerCharacter player, int experience, int currency, Item loot, bool groupFinderBonus)
        {
            var user = PlayerSystem.GetUserByPlayer(player);
            var messages = new List<string>()
            {
                "Dungeon complete. Your party remains intact."
            };
            if (groupFinderBonus)
            {
                messages.Add("You earned double rewards for completing a daily Group Finder dungeon! Queue up again in 24 hours to receive the 2x bonus again! (You can whisper me '!daily' for a status.)");
            }
            messages.Add($"{user.Username}, you've earned {experience} XP and {currency} Wolfcoins for completing the dungeon!");
            if (loot != null)
            {
                messages.Add($"You looted {loot.Name}!");
            }
            if (groupFinderBonus)
            {
                messages.Add("You completed a group finder dungeon. Type !queue to join another group!");
            }
            PushNotification?.Invoke(user, new CommandResult(messages.ToArray()));
        }

        private void DungeonSystem_DungeonFailure(Party party, IEnumerable<PlayerCharacter> deceased)
        {
            var deadUsers = deceased.Select(x => PlayerSystem.GetUserByPlayer(x));
            PushToParty(party, $"In the chaos, {string.Join(", and ", deadUsers.Select(x => x.Username))} lost their life. Seek vengeance in their honor!", deceased.ToArray());
            PushToParty(party, "It's a sad thing your adventure has ended here. No XP or Coins have been awarded.");
        }

        private void DungeonSystem_PlayerDeath(PlayerCharacter player, int experienceLost, int currencyLost)
        {
            PushNotification?.Invoke(PlayerSystem.GetUserByPlayer(player), new CommandResult($"Sadly, you have died. You lost {experienceLost} XP and {currencyLost} Coins."));
        }

        private void DungeonSystem_DungeonProgress(Party party, string result)
        {
            PushToParty(party, result);
        }

        private void ConfirmationSystem_Confirmed(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = PartySystem.GetCurrentGroup(player);
            if (party != null)
            {
                if (PartySystem.AcceptInvite(party, player))
                {
                    var members = party.Members.Where(x => !x.Equals(player)).Select(x => PlayerSystem.GetUserByPlayer(x).Username);
                    PushNotification?.Invoke(user, new CommandResult($"You successfully joined a party with the following members: {string.Join(", ", members)}"));
                    var settings = SettingsManager.GetGameSettings();
                    PushToParty(party, $"{user.Username}, level {player.Level} {player.CharacterClass.Name} has joined your party ({party.Members.Count}/{settings.DungeonPartySize})", player);
                    if (party.State == PartyState.Full)
                    {
                        PushNotification?.Invoke(PlayerSystem.GetUserByPlayer(party.Leader), new CommandResult($"You've reached {settings.DungeonPartySize} party members! You're ready to dungeon!"));
                    }
                    Logger.Info("{user} accepted invite to group with {members}", user.Username, string.Join(", ", members));
                }
            }
        }

        private void ConfirmationSystem_Canceled(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = PartySystem.GetCurrentGroup(player);
            if (party != null)
            {
                PartySystem.DeclineInvite(party, player);
                var leader = PlayerSystem.GetUserByPlayer(party.Leader);
                PushNotification?.Invoke(user, new CommandResult($"You declined {leader.Username}'s invite."));
                PushNotification?.Invoke(leader, new CommandResult($"{user.Username} has declined your party invite."));
                Logger.Info("{user} delcined party invite from {leader}.", user.Username, leader.Username);
            }
        }

        private bool CanExecuteCommand(User user, bool requiresLeader, out PlayerCharacter player, out Party party, out string errorMessage)
        {
            errorMessage = "Party commands are not available while in the dungeon finder queue. Use !leavequeue to exit the queue.";
            party = null;
            player = PlayerSystem.GetPlayerByUser(user);
            if (!GroupFinderSystem.IsPlayerQueued(player))
            {
                party = PartySystem.GetCurrentGroup(player);
                if (party != null)
                {
                    if (!requiresLeader || party.Leader.Equals(player))
                    {
                        return true;
                    }
                    errorMessage = "You are not the party leader.";
                }
                errorMessage = "You are not in a party.";
            }
            return false;
        }

        private void PushToParty(Party party, string message, params PlayerCharacter[] skip)
        {
            foreach (var member in party.Members.Except(skip))
            {
                PushNotification?.Invoke(PlayerSystem.GetUserByPlayer(member), new CommandResult(message));
            }
        }

        public CommandResult ListDungeons()
        {
            return new CommandResult("List of Wolfpack RPG Adventures: http://tinyurl.com/WolfpackAdventureList");
        }

        public CommandResult DungeonDetails(string id)
        {
            var dungeonData = DungeonSystem.ParseDungeonId(id);
            if (dungeonData != null)
            {
                var name = DungeonSystem.GetDungeonName(dungeonData);
                var range = dungeonData.Dungeon.LevelRanges.FirstOrDefault(x => x.ModeId == dungeonData.Mode.Id);
                if (range != null)
                {
                    return new CommandResult($"{name} (Levels {range.Minimum} - {range.Maximum}) -- {dungeonData.Dungeon.Description}");
                }
                return new CommandResult($"This dungeon is missing level range data, please contact the streamer to let them know.");
            }
            return new CommandResult($"Invalid Dungeon ID provided.");
        }

        public CommandResult CreateParty(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            if (!GroupFinderSystem.IsPlayerQueued(player))
            {
                var currentParty = PartySystem.GetCurrentGroup(player);
                if (currentParty == null)
                {
                    var party = PartySystem.CreateParty(false, player);
                    Logger.Info("Party Created by {user}", user.Username);
                    Logger.Info("Total number of parties: {count}", PartySystem.PartyCount);
                    return new CommandResult("Party created! Use '!add <username>' to invite party members.");
                }
                else if (currentParty.Leader.Equals(player))
                {
                    return new CommandResult($"You already have a party created! {PartyDescriptions[currentParty.State]}");
                }

                var leader = UserSystem.GetUserById(currentParty.Leader.UserId)?.Username;
                if (currentParty.PendingInvites.Contains(player))
                {
                    return new CommandResult($"You currently have an outstanding invite to another party from {leader}. Couldn't create new party!");
                }
                return new CommandResult($"You already have a party created! Your party leader is {leader}, Use !p <message> to talk to them.");
            }
            return new CommandResult("Can't create a party while queued with the Group Finder. Message me '!leavequeue' to exit.");
        }

        public CommandResult AddPlayer(User user, string playerName)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            if (!GroupFinderSystem.IsPlayerQueued(player))
            {
                var targetUser = UserSystem.GetUserByName(playerName);
                if (targetUser != null)
                {
                    if (!targetUser.Equals(user))
                    {
                        var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                        if (targetPlayer.Level >= 3)
                        {
                            if (targetPlayer.CharacterClass.CanPlay)
                            {
                                if (!GroupFinderSystem.IsPlayerQueued(targetPlayer) && PartySystem.GetCurrentGroup(targetPlayer) == null)
                                {
                                    var currentParty = PartySystem.GetCurrentGroup(player);
                                    if (currentParty == null)
                                    {
                                        currentParty = PartySystem.CreateParty(false, player);
                                    }
                                    if (currentParty.Leader.Equals(player))
                                    {
                                        var settings = SettingsManager.GetGameSettings();
                                        if (currentParty.State == PartyState.Forming || currentParty.State == PartyState.Full)
                                        {
                                            if (PartySystem.InvitePlayer(currentParty, targetPlayer))
                                            {
                                                PushNotification?.Invoke(targetUser, new CommandResult($"{user.Username}, Level {player.Level} {player.CharacterClass.Name}, has invited you to join a party. Accept? (!y/!n)"));
                                                PushToParty(currentParty, $"{targetUser.Username} was invited to your group.", player);
                                                return new CommandResult($"You invited {targetUser.Username} to your group.");
                                            }
                                            return new CommandResult($"You can't have more than {settings.DungeonPartySize} party members in your group, including pending invites.");
                                        }
                                        return new CommandResult("Your party is unable to add members at this time.");
                                    }
                                    return new CommandResult($"Only the party leader ({UserSystem.GetUserById(currentParty.Leader.UserId).Username}) can invite members.");
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
                    if (party.State != PartyState.Started)
                    {
                        var targetUser = UserSystem.GetUserByName(playerName);
                        if (targetUser != null)
                        {
                            var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                            if (party.Members.Contains(targetPlayer))
                            {
                                PushNotification.Invoke(targetUser, new CommandResult($"You were removed from {user.Username}'s party."));
                                party.Members.Remove(targetPlayer);
                                if (party.Members.Count > 1)
                                {
                                    PushToParty(party, $"{targetUser.Username} was removed from the party.", player);
                                    return new CommandResult($"{targetUser.Username} was removed from the party.");
                                }
                                PartySystem.DisbandParty(party);
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
                    var targetUser = UserSystem.GetUserByName(playerName);
                    if (targetUser != null)
                    {
                        var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                        if (party.Members.Contains(targetPlayer))
                        {
                            PartySystem.SetLeader(party, targetPlayer);
                            PushNotification?.Invoke(targetUser, new CommandResult($"{user.Username} has promoted you to Party Leader."));
                            PushToParty(party, $"{user.Username} has promoted {targetUser.Username} to Party Leader.", player, targetPlayer);
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
                if (party.State != PartyState.Started)
                {
                    var wasLeader = party.Leader.Equals(player);
                    PartySystem.RemovePlayer(party, player);
                    if (party.Members.Count > 1)
                    {
                        if (wasLeader)
                        {
                            var newLeader = PlayerSystem.GetUserByPlayer(party.Leader);
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
                        PartySystem.DisbandParty(party);
                    }
                    return new CommandResult($"You left the party.");
                }
                return new CommandResult("You can't leave your party while a dungeon is in progress!");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult PartyData(User user)
        {
            var canExecute = CanExecuteCommand(user, false, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                return new CommandResult(true, $"{user.Username} request their Party Data. Members: {string.Join(", ", party.Members.Select(x => PlayerSystem.GetUserByPlayer(x).Username))}; Status: {party.State}");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult PartyChat(User user, string message)
        {
            var canExecute = CanExecuteCommand(user, false, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    PushToParty(party, $"{user.Username} says: \"{message}\"", player);
                    return new CommandResult($"You whisper: \"{message}\"");
                }
                return new CommandResult();
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult SetReady(User user)
        {
            var canExecute = CanExecuteCommand(user, true, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                var success = PartySystem.SetReady(party);
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
            var canExecute = CanExecuteCommand(user, true, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                var success = PartySystem.UnsetReady(party);
                if (success)
                {
                    return new CommandResult("Party 'Ready' status has been revoked.");
                }
                return new CommandResult("Your party status can't be changed at this time.");
            }
            return new CommandResult(errorMessage);
        }

        public CommandResult StartDungeon(User user, string dungeonId = "")
        {
            var canExecute = CanExecuteCommand(user, true, out var player, out var party, out var errorMessage);
            if (canExecute)
            {
                DungeonRun run;
                if (string.IsNullOrWhiteSpace(dungeonId))
                {
                    if (party.Run != null)
                    {
                        run = party.Run;
                    }
                    else
                    {
                        run = Random.RandomElement(DungeonSystem.GetEligibleDungeons(party));
                    }
                }
                else
                {
                    run = DungeonSystem.ParseDungeonId(dungeonId);
                }
                if (run != null)
                {
                    var success = DungeonSystem.TryStartDungeon(party, run, out var broke);
                    if (success)
                    {
                        PushToParty(party, $"Successfully initiated {DungeonSystem.GetDungeonName(party.Run)}! Wolfcoins deducted.");
                        var members = party.Members.Select(x => $"{PlayerSystem.GetUserByPlayer(x)} (Level {x.Level} {x.CharacterClass.Name})");
                        PushToParty(party, $"Your party consists of: {string.Join(", ", members)}");
                        return new CommandResult();
                    }
                    if (broke.Any())
                    {
                        PushToParty(party, $"The following party members do not have enough money to run a dungeon: {string.Join(", ", broke.Select(x => PlayerSystem.GetUserByPlayer(x).Username))}");
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
