using LobotJR.Command.Model.General;
using LobotJR.Command.Model.Player;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Controller.General
{
    /// <summary>
    /// There was code in Program.cs for a betting system, but most of the code
    /// was commented out.
    /// This class, with the combined interface, has the functionality of that
    /// code fully implemented, but is not registered with autofac so it won't
    /// be loaded.
    /// </summary>
    public class BettingController
    {
        private readonly List<Bet> BetList = new List<Bet>();
        public bool IsActive { get; private set; }
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Adds a player's wager to the current active bet.
        /// </summary>
        /// <param name="player">The player who's betting.</param>
        /// <param name="amount">The amount to wager.</param>
        /// <param name="voteYes">True if voting for success.</param>
        /// <returns>True if the bet was placed. False if the player has
        /// already placed a bet or does not have enough currency to cover the
        /// wager amount.</returns>
        public bool PlaceBet(PlayerCharacter player, int amount, bool voteYes)
        {
            if (!BetList.Any(x => x.Player.Equals(player)))
            {
                if (player.Currency >= amount)
                {
                    player.Currency -= amount;
                    BetList.Add(new Bet()
                    {
                        Player = player,
                        Amount = amount,
                        VoteSuccess = voteYes
                    });
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the bet active and opens betting.
        /// </summary>
        public void StartBet()
        {
            IsActive = true;
            IsOpen = true;
            BetList.Clear();
        }

        /// <summary>
        /// Closes an active bet, stopping new wagers from being placed.
        /// </summary>
        public void CloseBet()
        {
            if (IsActive)
            {
                IsOpen = false;
            }
        }

        private void ResetBet()
        {
            BetList.Clear();
            IsActive = false;
            IsOpen = false;
        }

        /// <summary>
        /// Resolves a bet. This pays out all winners and clears the active
        /// bet.
        /// </summary>
        /// <param name="didSucceed">True if the payout should go to those who
        /// voted for success.</param>
        public void Resolve(bool didSucceed)
        {
            var winners = BetList.Where(x => x.VoteSuccess == didSucceed).ToList();
            var totalBet = BetList.Sum(x => x.Amount);
            var totalWin = winners.Sum(x => x.Amount);
            foreach (var better in BetList.Where(x => x.VoteSuccess == didSucceed))
            {
                better.Player.Currency += better.Amount / totalWin * totalBet;
            }
            ResetBet();
        }

        /// <summary>
        /// Cancels the current bet and refunds all players.
        /// </summary>
        /// <returns>True if a bet was canceled. False if no bet was active.</returns>
        public bool CancelBet()
        {
            if (IsActive)
            {
                foreach (var better in BetList)
                {
                    better.Player.Currency += better.Amount;
                }
                ResetBet();
                return true;
            }
            return false;
        }
    }
}
