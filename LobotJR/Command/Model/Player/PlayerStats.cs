namespace LobotJR.Command.Model.Player
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
        float ItemFind { get; set; }
        /// <summary>
        /// The amount of coins earned.
        /// </summary>
        float CoinBonus { get; set; }
        /// <summary>
        /// The experience earned.
        /// </summary>
        float XpBonus { get; set; }
        /// <summary>
        /// The chance for a player to avoid death after failing a dungeon.
        /// </summary>
        float PreventDeathBonus { get; set; }
    }
}
