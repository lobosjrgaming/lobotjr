using LobotJR.Command.Controller.Dungeons;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Dungeons
{
    /// <summary>
    /// View containing commands for managing the group finder.
    /// </summary>
    public class GroupFinderAdmin : ICommandView
    {
        private readonly GroupFinderController GroupFinderController;
        private readonly DungeonController DungeonController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "GroupFinder.Admin";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public GroupFinderAdmin(GroupFinderController groupFinderController, DungeonController dungeonController)
        {
            GroupFinderController = groupFinderController;
            DungeonController = dungeonController;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("QueueStatus", this, CommandMethod.GetInfo(QueueStatus), "queuestatus"),
            };
        }

        public CommandResult QueueStatus()
        {
            var entries = GroupFinderController.GetQueueEntries();
            var responses = new List<string>
            {
                $"There are {entries.Count()} players in queue."
            };
            var runs = entries.SelectMany(x => x.Dungeons).GroupBy(x => DungeonController.GetDungeonName(x));
            responses.AddRange(runs.Select(x => $"{x.Key}: {x.Count()}"));
            return new CommandResult(responses.ToArray());
        }
    }
}
