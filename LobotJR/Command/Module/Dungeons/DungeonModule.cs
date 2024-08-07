using LobotJR.Command.Model.Dungeons;
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

        private readonly DungeonSystem DungeonSystem;
        private readonly PlayerSystem PlayerSystem;
        private readonly UserSystem UserSystem;
        private readonly SettingsManager SettingsManager;
        private readonly string DailyTimerName = "Daily Dungeon";
        private readonly Dictionary<PartyState, string> PartyDescriptions = new Dictionary<PartyState, string>()
        {
            { PartyState.Forming, "Party is currently forming. Add members with '!add <username>'" },
            { PartyState.Ready, "Party is filled and ready to adventure! Type '!start' to begin!" },
            { PartyState.Started, "Your party is currently on an adventure!" },
            { PartyState.Complete, "Your party just finished an adventure!" },
            { PartyState.Full, "I have no idea the status of your party." }
        };

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

        public DungeonModule(DungeonSystem dungeonSystem, PlayerSystem playerSystem, UserSystem userSystem, ConfirmationSystem confirmationSystem, SettingsManager settingsManager)
        {
            DungeonSystem = dungeonSystem;
            PlayerSystem = playerSystem;
            UserSystem = userSystem;
            SettingsManager = settingsManager;
            confirmationSystem.Confirmed += ConfirmationSystem_Confirmed;
            confirmationSystem.Canceled += ConfirmationSystem_Canceled;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DungeonList", this, CommandMethod.GetInfo(ListDungeons), "dungeonlist"),
                new CommandHandler("DungeonDetails", this, CommandMethod.GetInfo<string>(DungeonDetails), "dungeon"),
                new CommandHandler("DailyStatus", this, CommandMethod.GetInfo(DailyStatus), "daily"),

                new CommandHandler("CreateParty", this, CommandMethod.GetInfo(CreateParty), "createparty"),
                new CommandHandler("AddPlayer", this, CommandMethod.GetInfo<string>(AddPlayer), "add", "invite"),
                new CommandHandler("RemovePlayer", this, CommandMethod.GetInfo<string>(RemovePlayer), "kick"),
                new CommandHandler("SetLeader", this, CommandMethod.GetInfo<string>(PromotePlayer), "promote"),
                new CommandHandler("LeaveParty", this, CommandMethod.GetInfo(LeaveParty), "leaveparty", "exitparty"),
                new CommandHandler("PartyData", this, CommandMethod.GetInfo(PartyData), "partydata"),
                new CommandHandler("PartyChat", new CommandExecutor(this, CommandMethod.GetInfo<string>(PartyChat), true), "p", "party"),

                new CommandHandler("Ready", this, CommandMethod.GetInfo(), "ready"),
                new CommandHandler("Unready", this, CommandMethod.GetInfo(), "unready"),
                new CommandHandler("Start", this, CommandMethod.GetInfo(), "start"),

                new CommandHandler("EnterQueue", this, CommandMethod.GetInfo(), "queue"),
                new CommandHandler("LeaveQueue", this, CommandMethod.GetInfo(), "leavequeue"),
                new CommandHandler("QueueTime", this, CommandMethod.GetInfo(), "queuetime"),
                
                //This is an admin command
                new CommandHandler("QueueStatus", this, CommandMethod.GetInfo(), "queuestatus"),
            };
        }

        private void ConfirmationSystem_Confirmed(User user)
        {
            // User accepted party invite
            throw new global::System.NotImplementedException();
        }

        private void ConfirmationSystem_Canceled(User user)
        {
            // User declined party invite
            throw new global::System.NotImplementedException();
        }

        private void PushToParty(Party party, string message, params PlayerCharacter[] skip)
        {
            foreach (var member in party.Members.Except(skip))
            {
                PushNotification?.Invoke(UserSystem.GetUserById(member.UserId), new CommandResult(message));
            }
        }

        public CommandResult ListDungeons()
        {
            return new CommandResult("List of Wolfpack RPG Adventures: http://tinyurl.com/WolfpackAdventureList");
        }

        public CommandResult DungeonDetails(string id)
        {
            var dungeonData = DungeonSystem.ParseDungeonId(id);
            if (dungeonData.Dungeon != null && dungeonData.Mode != null)
            {
                var modeName = dungeonData.Mode.IsDefault ? "" : $" [{dungeonData.Mode.Name}]";
                var range = dungeonData.Dungeon.LevelRanges.FirstOrDefault(x => x.ModeId == dungeonData.Mode.Id);
                if (range != null)
                {
                    return new CommandResult($"{dungeonData.Dungeon.Name}{modeName} (Levels {range.Minimum} - {range.Maximum}) -- {dungeonData.Dungeon.Description}");
                }
                return new CommandResult($"This dungeon is missing level range data, please contact the streamer to let them know.");
            }
            return new CommandResult($"Invalid Dungeon ID provided.");
        }

        public CommandResult DailyStatus(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var remaining = DungeonSystem.GetLockoutTime(player, DailyTimerName);
            if (remaining.TotalMilliseconds > 0)
            {
                return new CommandResult($"Your daily Group Finder reward resets in {TimeSpan.FromSeconds(remaining.TotalSeconds).ToString("c")}.");
            }
            return new CommandResult("You are eligible for daily Group Finder rewards! Go queue up!");
        }

        public CommandResult CreateParty(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            if (!DungeonSystem.IsPlayerQueued(player))
            {
                var currentParty = DungeonSystem.GetCurrentGroup(player);
                if (currentParty == null)
                {
                    var party = DungeonSystem.CreateParty(false, player);
                    Logger.Info("Party Created by {user}", user.Username);
                    Logger.Info("Total number of parties: {count}", DungeonSystem.PartyCount);
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
            if (!DungeonSystem.IsPlayerQueued(player))
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
                                if (!DungeonSystem.IsPlayerQueued(targetPlayer) && DungeonSystem.GetCurrentGroup(targetPlayer) == null)
                                {
                                    var currentParty = DungeonSystem.GetCurrentGroup(player);
                                    if (currentParty == null)
                                    {
                                        currentParty = DungeonSystem.CreateParty(false, player);
                                    }
                                    if (currentParty.Leader.Equals(player))
                                    {
                                        var settings = SettingsManager.GetGameSettings();
                                        if (currentParty.State == PartyState.Forming || currentParty.State == PartyState.Ready)
                                        {
                                            if (currentParty.Members.Count + currentParty.PendingInvites.Count < settings.DungeonPartySize)
                                            {
                                                currentParty.PendingInvites.Add(targetPlayer);
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
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = DungeonSystem.GetCurrentGroup(player);
            if (party != null)
            {
                if (party.Leader.Equals(player))
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
                                    DungeonSystem.DisbandParty(party);
                                    return new CommandResult("Your party has been disbanded.");
                                }
                            }
                            return new CommandResult("Couldn't find that party member to remove.");
                        }
                        return new CommandResult("You can't kick a party member in the middle of a dungeon!");
                    }
                    return new CommandResult("You can't kick yourself from a group! Do !leaveparty instead.");
                }
                return new CommandResult("You are not the party leader.");
            }
            return new CommandResult("You are not in a party.");
        }

        public CommandResult PromotePlayer(User user, string playerName)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = DungeonSystem.GetCurrentGroup(player);
            if (party != null)
            {
                if (party.Leader.Equals(player))
                {
                    if (!playerName.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        var targetUser = UserSystem.GetUserByName(playerName);
                        if (targetUser != null)
                        {
                            var targetPlayer = PlayerSystem.GetPlayerByUser(targetUser);
                            if (party.Members.Contains(targetPlayer))
                            {
                                party.SetLeader(targetPlayer);
                                PushNotification?.Invoke(targetUser, new CommandResult($"{user.Username} has promoted you to Party Leader."));
                                PushToParty(party, $"{user.Username} has promoted {targetUser.Username} to Party Leader.", player, targetPlayer);
                                return new CommandResult($"You have promoted {targetUser.Username} to Party Leader.");
                            }
                        }
                        return new CommandResult($"Party member '{playerName}' not found. You are still party leader.");
                    }
                    return new CommandResult("You are already the party leader!");
                }
                return new CommandResult("You are not the party leader.");
            }
            return new CommandResult("You are not in a party.");
        }

        public CommandResult LeaveParty(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = DungeonSystem.GetCurrentGroup(player);
            if (party != null)
            {
                if (party.State != PartyState.Started)
                {
                    var wasLeader = party.Leader.Equals(player);
                    party.RemoveMember(player);
                    if (party.Members.Count > 1)
                    {
                        if (wasLeader)
                        {
                            var newLeader = UserSystem.GetUserById(party.Leader.UserId);
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
                        DungeonSystem.DisbandParty(party);
                    }
                    return new CommandResult($"You left the party.");
                }
                return new CommandResult("You can't leave your party while a dungeon is in progress!");
            }
            return new CommandResult("You are not in a party.");
        }

        public CommandResult PartyData(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = DungeonSystem.GetCurrentGroup(player);
            if (party != null)
            {
                return new CommandResult(true, $"{user.Username} request their Party Data. Members: {string.Join(", ", party.Members.Select(x => UserSystem.GetUserById(x.UserId).Username))}; Status: {party.State}");
            }
            return new CommandResult("You are not in a party.");
        }

        public CommandResult PartyChat(User user, string message)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = DungeonSystem.GetCurrentGroup(player);
            if (party != null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    PushToParty(party, $"{user.Username} says: \"{message}\"", player);
                    return new CommandResult($"You whisper: \"{message}\"");
                }
                return new CommandResult();
            }
            return new CommandResult("You are not in a party.");
        }

        public CommandResult SetReady(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = DungeonSystem.GetCurrentGroup(player);
            if (party != null)
            {
                if (party.Leader.Equals(player))
                {
                    var success = party.SetReady();
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
                return new CommandResult("You are not the party leader.");
            }
            return new CommandResult("You are not in a party.");
        }

        public CommandResult UnsetReady(User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            var party = DungeonSystem.GetCurrentGroup(player);
            if (party != null)
            {
                if (party.Leader.Equals(player))
                {
                    var success = party.UnsetReady();
                    if (success)
                    {
                        return new CommandResult("Party 'Ready' status has been revoked.");
                    }
                    return new CommandResult("Your party status can't be changed at this time.");
                }
                return new CommandResult("You are not the party leader.");
            }
            return new CommandResult("You are not in a party.");
        }
    }
}
