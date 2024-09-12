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
        public int DungeonId { get; private set; } = -1;
        /// <summary>
        /// The mode to run through the dungeon in.
        /// </summary>
        public int ModeId { get; private set; } = -1;

        public DungeonRun(Dungeon dungeon, DungeonMode mode)
        {
            DungeonId = dungeon.Id;
            ModeId = mode.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is DungeonRun other)
            {
                return other.DungeonId.Equals(DungeonId) && other.ModeId.Equals(ModeId);
            }
            return false;
        }

        public override int GetHashCode()
        {
            var prime1 = 108301;
            var prime2 = 150151;
            var hash = prime1;
            hash = (hash * prime2) ^ DungeonId.GetHashCode();
            hash = (hash * prime2) ^ ModeId.GetHashCode();
            return hash;
        }
    }
}
