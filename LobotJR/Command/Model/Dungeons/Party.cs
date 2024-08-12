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
        public List<PlayerCharacter> Members { get; private set; } = new List<PlayerCharacter>();
        public List<PlayerCharacter> PendingInvites { get; private set; } = new List<PlayerCharacter>();
        public PartyState State { get; set; }
        public DungeonRun Run { get; set; }
        public bool IsQueueGroup { get; set; }

        public Party(bool isQueueGroup, params PlayerCharacter[] players)
        {
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
    }
}
