﻿using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Controller.Dungeons
{
    /// <summary>
    /// Controller for managing player parties and running dungeons.
    /// </summary>
    public class PartyController
    {
        private readonly SettingsManager SettingsManager;
        private readonly PlayerController PlayerController;
        private readonly List<Party> DungeonGroups = new List<Party>();

        /// <summary>
        /// Gets the number of dungeon groups.
        /// </summary>
        public int PartyCount { get { return DungeonGroups.Count; } }

        public PartyController(SettingsManager settingsManager, PlayerController playerController)
        {
            SettingsManager = settingsManager;
            PlayerController = playerController;
        }

        /// <summary>
        /// Clears all registered groups.
        /// </summary>
        public void ResetGroups()
        {
            DungeonGroups.Clear();
        }

        /// <summary>
        /// Gets the collection of current registered groups.
        /// </summary>
        /// <returns>A collection of Party objects.</returns>
        public IEnumerable<Party> GetAllGroups()
        {
            return DungeonGroups;
        }

        /// <summary>
        /// Gets the party the player is in or has been invited to.
        /// </summary>
        /// <param name="player">The player to get the group for.</param>
        /// <returns>The party, if any, that the player is in.</returns>
        public Party GetCurrentGroup(PlayerCharacter player)
        {
            return DungeonGroups.FirstOrDefault(x => x.Members.Contains(player.UserId) || x.PendingInvites.Contains(player.UserId));
        }

        /// <summary>
        /// Gets the player character objects for the members in a party.
        /// </summary>
        /// <param name="party">The party to get players for.</param>
        /// <returns>A collection of players.</returns>
        public IEnumerable<PlayerCharacter> GetPartyPlayers(Party party)
        {
            return party.Members.Select(x => PlayerController.GetPlayerByUserId(x));
        }

        /// <summary>
        /// Creates a new party.
        /// </summary>
        /// <param name="isQueueGroup">Whether this group was created by the
        /// group finder.</param>
        /// <param name="players">The players to place in the group.</param>
        /// <returns>The party that was created.</returns>
        public Party CreateParty(bool isQueueGroup, params PlayerCharacter[] players)
        {
            var party = new Party(isQueueGroup, players);
            DungeonGroups.Add(party);
            return party;
        }

        /// <summary>
        /// Creates a new party.
        /// </summary>
        /// <param name="isQueueGroup">Whether this group was created by the
        /// group finder.</param>
        /// <param name="players">The players to place in the group.</param>
        /// <returns>The party that was created.</returns>
        public Party CreateParty(bool isQueueGroup, params string[] userIds)
        {
            var party = new Party(isQueueGroup, userIds.Select(x => PlayerController.GetPlayerByUserId(x)).ToArray());
            DungeonGroups.Add(party);
            return party;
        }

        /// <summary>
        /// Removes a party from the list of parties.
        /// </summary>
        /// <param name="party">The party to remove.</param>
        public void DisbandParty(Party party)
        {
            DungeonGroups.Remove(party);
        }

        /// <summary>
        /// Checkes if a player is the leader of a party.
        /// </summary>
        /// <param name="party">The party to check the leader of.</param>
        /// <param name="player">The player to check for leader status.</param>
        /// <returns>True if the player is the leader of that party.</returns>
        public bool IsLeader(Party party, PlayerCharacter player)
        {
            return party.Leader.Equals(player);
        }

        /// <summary>
        /// Checkes if a player is the leader of a party.
        /// </summary>
        /// <param name="party">The party to check the leader of.</param>
        /// <param name="userId">The user id of player to check.</param>
        /// <returns>True if the player is the leader of that party.</returns>
        public bool IsLeader(Party party, string userId)
        {
            return party.Leader.Equals(userId);
        }

        /// <summary>
        /// Sets the leader of a party to a specific player.
        /// </summary>
        /// <param name="party">The party to set the leader of.</param>
        /// <param name="userId">The user id of the player to set as leader.</param>
        /// <returns>True if the player was able to be set as the leader.</returns>
        public bool SetLeader(Party party, string userId)
        {
            if (party.Members.Any(x => x.Equals(userId)) && !IsLeader(party, userId))
            {
                var member = party.Members.First(x => x.Equals(userId));
                party.Members.Remove(member);
                party.Members.Insert(0, member);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Invites a player to a party.
        /// </summary>
        /// <param name="party">The party to invite the player to.</param>
        /// <param name="player">The player to invite.</param>
        /// <returns>True if the invite was able to be sent.</returns>
        public bool InvitePlayer(Party party, PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            if (!party.Members.Contains(player.UserId)
                && !party.PendingInvites.Contains(player.UserId)
                && party.Members.Count + party.PendingInvites.Count < settings.DungeonPartySize)
            {
                party.PendingInvites.Add(player.UserId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Accepts an invite to a party.
        /// </summary>
        /// <param name="party">The party the invite was sent from.</param>
        /// <param name="player">The player that was invited.</param>
        /// <returns>True if the invite was able to be accepted.</returns>
        public bool AcceptInvite(Party party, PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            if (party.PendingInvites.Contains(player.UserId)
                && party.Members.Count + party.PendingInvites.Count <= settings.DungeonPartySize)
            {
                party.PendingInvites.Remove(player.UserId);
                return AddPlayer(party, player);
            }
            return false;
        }

        /// <summary>
        /// Declines a party invite.
        /// </summary>
        /// <param name="party">The party the invite was sent from.</param>
        /// <param name="player">The player that was invited.</param>
        /// <returns>True if the invite was declined.</returns>
        public bool DeclineInvite(Party party, PlayerCharacter player)
        {
            if (party.PendingInvites.Contains(player.UserId))
            {
                party.PendingInvites.Remove(player.UserId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a player to a party.
        /// </summary>
        /// <param name="party">The party to add the player to.</param>
        /// <param name="player">The player to add.</param>
        /// <returns>True if the player was added to the party.</returns>
        public bool AddPlayer(Party party, PlayerCharacter player)
        {
            if (party.State == PartyState.Forming)
            {
                var settings = SettingsManager.GetGameSettings();
                if (party.Members.Count < settings.DungeonPartySize)
                {
                    party.Members.Add(player.UserId);
                    var _ = player.CharacterClass;
                    if (party.Members.Count == settings.DungeonPartySize)
                    {
                        return SetReady(party);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes a player from a party.
        /// </summary>
        /// <param name="party">The party to remove the player from.</param>
        /// <param name="player">The player to remove.</param>
        /// <returns>True if the player was removed.</returns>
        public bool RemovePlayer(Party party, PlayerCharacter player)
        {
            if (party.Members.Contains(player.UserId))
            {
                if (party.State == PartyState.Full || party.State == PartyState.Forming)
                {
                    party.Members.Remove(player.UserId);
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

        /// <summary>
        /// Sets a party to the full state so it can start a dungeon.
        /// </summary>
        /// <param name="party">The party to change the state of.</param>
        /// <returns>True if the state was able to be changed.</returns>
        public bool SetReady(Party party)
        {
            if (party.State == PartyState.Forming && party.PendingInvites.Count == 0)
            {
                party.State = PartyState.Full;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Revokes a party's ready status.
        /// </summary>
        /// <param name="party">The party to change the state of.</param>
        /// <returns>True if the state was able to be changed.</returns>
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
    }
}
