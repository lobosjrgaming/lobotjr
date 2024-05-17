namespace LobotJR.Command.Model.Experience
{
    /// <summary>
    /// The stats that describe a player character's abilities.
    /// </summary>
    public interface PlayerStats
    {
        /// <summary>
        /// The chance of successfully completing a dungeon.
        /// </summary>
        float SuccessChance { get; set; }
        /// <summary>
        /// The chance for items to drop.
        /// </summary>
        int ItemFind { get; set; }
        /// <summary>
        /// The amount of coins earned.
        /// </summary>
        int CoinBonus { get; set; }
        /// <summary>
        /// The experience earned.
        /// </summary>
        int XpBonus { get; set; }
        /// <summary>
        /// The chance for a player to avoid death after failing a dungeon.
        /// </summary>
        float PreventDeathBonus { get; set; }
    }
}
