using LobotJR.Command.Model.Player;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Model.Dungeons
{
    public enum PartyState
    {
        /// <summary>
        /// Party has one or fewer members and will be deleted when dungeons are processed.
        /// </summary>
        Disbanded,
        /// <summary>
        /// Party has fewer than the maximum number of players.
        /// </summary>
        Forming,
        /// <summary>
        /// Party has the maximum number of players, but is not in a dungeon.
        /// </summary>
        Full,
        /// <summary>
        /// Party is going through a dungeon.
        /// </summary>
        Started,
        /// <summary>
        /// Party has just completed a dungeon.
        /// </summary>
        Complete,
        /// <summary>
        /// Appears to be an alias of Full
        /// </summary>
        Ready,
    }

    public class Party
    {
        private int MaxSize;

        public List<PlayerCharacter> Members { get; private set; } = new List<PlayerCharacter>();
        public List<PlayerCharacter> PendingInvites { get; private set; } = new List<PlayerCharacter>();
        public PartyState State { get; set; }
        public bool IsQueueGroup { get; set; }

        public Party(int size, bool isQueueGroup, params PlayerCharacter[] players)
        {
            MaxSize = size;
            IsQueueGroup = isQueueGroup;
            Members.AddRange(players);
            State = PartyState.Forming;
        }

        public PlayerCharacter Leader
        {
            get
            {
                return Members.FirstOrDefault();
            }
        }

        public void SetLeader(PlayerCharacter leader)
        {
            if (Members.Any(x => x.UserId.Equals(leader.UserId)))
            {
                Members.Remove(leader);
                Members.Insert(0, leader);
            }
        }

        public void AcceptInvite(PlayerCharacter player)
        {
            if (PendingInvites.Any(x => x.UserId.Equals(player.UserId)))
            {
                PendingInvites.Remove(player);
                AddMember(player);
            }
        }

        public void DeclineInvite(PlayerCharacter player)
        {
            if (PendingInvites.Any(x => x.UserId.Equals(player.UserId)))
            {
                PendingInvites.Remove(player);
            }
        }

        public bool AddMember(PlayerCharacter player)
        {
            if (State == PartyState.Forming)
            {
                if (Members.Count < MaxSize)
                {
                    Members.Add(player);
                    if (Members.Count == MaxSize)
                    {
                        State = PartyState.Full;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool RemoveMember(PlayerCharacter player)
        {
            if (State != PartyState.Started && State != PartyState.Complete)
            {
                Members.Remove(player);
                if (Members.Count <= 1)
                {
                    State = PartyState.Disbanded;
                }
                else
                {
                    State = PartyState.Forming;
                }
                return true;
            }
            return false;
        }

        public bool SetReady()
        {
            if (State == PartyState.Forming && PendingInvites.Count == 0)
            {
                State = PartyState.Ready;
                return true;
            }
            return false;
        }

        public bool UnsetReady()
        {
            if (State == PartyState.Ready && Members.Count < MaxSize)
            {
                State = PartyState.Forming;
                return true;
            }
            return false;
        }
    }
}
