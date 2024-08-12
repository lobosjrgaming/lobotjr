namespace LobotJR.Command.Model.Dungeons
{
    public class DungeonRun
    {
        public Dungeon Dungeon { get; private set; }
        public DungeonMode Mode { get; private set; }

        public DungeonRun(Dungeon dungeon, DungeonMode mode)
        {
            Dungeon = dungeon;
            Mode = mode;
        }
    }
}
