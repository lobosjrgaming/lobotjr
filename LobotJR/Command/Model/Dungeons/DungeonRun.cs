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

        public override bool Equals(object obj)
        {
            if (obj is DungeonRun other)
            {
                return other.Dungeon.Equals(Dungeon) && other.Mode.Equals(Mode);
            }
            return false;
        }

        public override int GetHashCode()
        {
            var prime1 = 108301;
            var prime2 = 150151;
            var hash = prime1;
            hash = (hash * prime2) ^ Dungeon.GetHashCode();
            hash = (hash * prime2) ^ Mode.GetHashCode();
            return hash;
        }
    }
}
