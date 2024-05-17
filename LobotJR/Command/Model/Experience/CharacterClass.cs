using LobotJR.Data;

namespace LobotJR.Command.Model.Experience
{
    /// <summary>
    /// A character class available for players to choose.
    /// </summary>
    public class CharacterClass : TableObject, PlayerStats
    {
        /// <summary>
        /// The name of the class as shown to the players.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Whether or not players with this class can join groups and run
        /// dungeons or otherwise play the game. Used by the starter class to
        /// restrict play until level 3.
        /// </summary>
        public bool CanPlay { get; set; }
        /// <summary>
        /// The base chance for to succeed at a dungeon.
        /// </summary>
        public float SuccessChance { get; set; }
        /// <summary>
        /// The base item find bonus.
        /// </summary>
        public int ItemFind { get; set; }
        /// <summary>
        /// The bonus to wolfcoins earned in dungeons.
        /// </summary>
        public int CoinBonus { get; set; }
        /// <summary>
        /// The bonus to experience earned in dungeons.
        /// </summary>
        public int XpBonus { get; set; }
        /// <summary>
        /// The chance to avoid player death when a dungeon is failed.
        /// </summary>
        public float PreventDeathBonus { get; set; }

        public CharacterClass() { }

        public CharacterClass(string name, float successChance, int itemFind, int coinBonus, int xpBonus, float preventDeathBonus)
        {
            Name = name;
            SuccessChance = successChance;
            ItemFind = itemFind;
            CoinBonus = coinBonus;
            XpBonus = xpBonus;
            PreventDeathBonus = preventDeathBonus;
        }
    }
}
