using LobotJR.Data;

namespace LobotJR.Command.Model.Dungeons
{
    /// <summary>
    /// The difficulty rating for a given encounter in a specific dungeon mode.
    /// </summary>
    public class EncounterLevel : TableObject
    {
        /// <summary>
        /// The foreign key id of the encounter this difficulty rating is for.
        /// </summary>
        public int EncounterId { get; set; }
        /// <summary>
        /// The encounter this level sets the difficulty for.
        /// </summary>
        public virtual Encounter Encounter { get; set; }
        /// <summary>
        /// The foreign key id of the dungeon mode this difficulty rating is
        /// for.
        /// </summary>
        public int ModeId { get; set; }
        /// <summary>
        /// The dungeon mode this level sets the difficulty for.
        /// </summary>
        public virtual DungeonMode Mode { get; set; }
        /// <summary>
        /// The difficulty rating of this encounter, used to determine success
        /// chance. This is the base %chance for the encounter to end in
        /// success.
        /// </summary>
        public float Difficulty { get; set; }
    }
}
