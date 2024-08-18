using LobotJR.Command.Model.General;
using LobotJR.Command.Model.Player;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.General
{
    /// <summary>
    /// There was code in Program.cs for a betting system, but most of the code
    /// was commented out.
    /// This class, with the combine module, has the functionality of that code
    /// fully implemented, but is not registered with autofac so it won't be
    /// loaded.
    /// </summary>
    public class BettingSystem : ISystemProcess
    {
        private readonly List<Bet> BetList = new List<Bet>();
        public bool IsActive { get; private set; }
        public bool IsOpen { get; private set; }

        public BettingSystem()
        {
        }

        public bool PlaceBet(PlayerCharacter player, int amount, bool voteYes)
        {
            if (!BetList.Any(x => x.Player.Equals(player)))
            {
                if (player.Currency >= amount)
                {
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

        public void StartBet()
        {
            IsActive = true;
            IsOpen = true;
        }

        public void CloseBet()
        {
            if (IsActive)
            {
                IsOpen = false;
            }
        }

        public void Resolve(bool didSucceed)
        {
            var winners = BetList.Where(x => x.VoteSuccess == didSucceed).ToList();
            var totalBet = BetList.Sum(x => x.Amount);
            var totalWin = winners.Sum(x => x.Amount);
            foreach (var better in BetList.Where(x => x.VoteSuccess == didSucceed))
            {
                better.Player.Currency += better.Amount / totalWin * totalBet;
            }
            BetList.Clear();
            IsActive = false;
            IsOpen = false;
        }

        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
