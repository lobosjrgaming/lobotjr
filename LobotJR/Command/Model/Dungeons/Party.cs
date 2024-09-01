using LobotJR.Command.Model.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// The state of a party.
    /// </summary>
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
        /// Party has just failed a dungeon.
        /// </summary>
        Failed
    }

    /// <summary>
    /// The state of a step being processing during a dungeon. Determines what
    /// action should be taken the next time dungeon progress is processed.
    /// </summary>
    public enum StepState
    {
        /// <summary>
        /// The step should send the the setup text.
        /// </summary>
        Setup,
        /// <summary>
        /// The step should check for success.
        /// </summary>
        Resolving,
        /// <summary>
        /// The step should send the post-encounter text.
        /// </summary>
        Complete
    }

    /// <summary>
    /// A party of players for running through dungeons.
    /// </summary>
    public class Party
    {
        /// <summary>
        /// The players that are currently in the group.
        /// </summary>
        public List<PlayerCharacter> Members { get; private set; } = new List<PlayerCharacter>();
        /// <summary>
        /// The players that have been invited to the group but not yet joined.
        /// </summary>
        public List<PlayerCharacter> PendingInvites { get; private set; } = new List<PlayerCharacter>();
        /// <summary>
        /// The current state of the party.
        /// </summary>
        public PartyState State { get; set; }
        /// <summary>
        /// The dungeon and mode the party is running through.
        /// </summary>
        public DungeonRun Run { get; set; }
        /// <summary>
        /// Whether this group was created automatically by the group finder.
        /// </summary>
        public bool IsQueueGroup { get; set; }
        /// <summary>
        /// Amount of time each player spent in queue for this party. Only
        /// applicable for queue groups.
        /// </summary>
        public Dictionary<PlayerCharacter, int> QueueTimes { get; private set; } = new Dictionary<PlayerCharacter, int>();
        /// <summary>
        /// The timestamp the most recent encounter of the dungeon was
        /// completed.
        /// </summary>
        public DateTime? LastUpdate { get; set; }
        /// <summary>
        /// The index of the current encounter the party is processing.
        /// </summary>
        public int CurrentEncounter { get; set; }
        /// <summary>
        /// Whether the current step is complete or still pending.
        /// </summary>
        public StepState StepState { get; set; }

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

        public void SetQueueTimes(Dictionary<PlayerCharacter, int> queueTimes)
        {
            if (queueTimes != null && queueTimes.Any())
            {
                foreach (var item in queueTimes)
                {
                    QueueTimes.Add(item.Key, item.Value);
                }
            }
        }

        public void Reset()
        {
            State = PartyState.Full;
            CurrentEncounter = 0;
            StepState = StepState.Setup;
        }
    }
}
