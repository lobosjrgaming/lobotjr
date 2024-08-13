namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// A dungeon paired with the mode it will be run in.
    /// </summary>
    public class DungeonRun
    {
        /// <summary>
        /// The data for the dungeon to run through.
        /// </summary>
        public Dungeon Dungeon { get; private set; }
        /// <summary>
        /// The mode to run through the dungeon in.
        /// </summary>
        public DungeonMode Mode { get; private set; }

        public DungeonRun(Dungeon dungeon, DungeonMode mode)
        {
            Dungeon = dungeon;
            Mode = mode;
        }
    }
}
