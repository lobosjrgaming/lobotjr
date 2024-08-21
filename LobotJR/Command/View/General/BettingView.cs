using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Player;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.General
{
    /// <summary>
    /// There was code in Program.cs for a betting system, but most of the code
    /// was commented out.
    /// This class, combined with the matching controller, has the functionality of
    /// that code fully implemented, but is not registered with autofac so it
    /// won't be loaded.
    /// </summary>
    public class BettingView : ICommandView
    {
        private static readonly IEnumerable<string> YesVotes = new List<string>() { "y", "yes", "t", "true", "1", "succeed" };
        private static readonly IEnumerable<string> NoVotes = new List<string>() { "n", "no", "f", "false", "2", "fail" };

        private readonly BettingController BettingController;
        private readonly PlayerController PlayerController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Betting";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public BettingView(BettingController bettingController, PlayerController playerController)
        {
            BettingController = bettingController;
            PlayerController = playerController;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("PlaceBet", this, CommandMethod.GetInfo<int, string>(PlaceBet), "bet"),

                // These are admin commands and will need to be moved into
                // their own view for access control if this gets implemented
                // I just put admin. in front as a hacky way to indicate this
                new CommandHandler("Admin.StartBet", this, CommandMethod.GetInfo<string>(StartBet), "startbet"),
                new CommandHandler("Admin.CloseBet", this, CommandMethod.GetInfo(CloseBet), "closebet"),
                new CommandHandler("Admin.ResolveBet", this, CommandMethod.GetInfo<string>(ResolveBet), "resolvebet", "endbet"),
                new CommandHandler("Admin.CancelBet", this, CommandMethod.GetInfo(CancelBet), "cancelbet"),
            };
        }

        public CommandResult PlaceBet(User user, int amount, string vote)
        {
            if (BettingController.IsActive)
            {
                if (BettingController.IsOpen)
                {
                    bool? voteBool = null;
                    if (YesVotes.Any(x => x.Equals(vote, StringComparison.OrdinalIgnoreCase)))
                    {
                        voteBool = true;
                    }
                    else if (NoVotes.Any(x => x.Equals(vote, StringComparison.OrdinalIgnoreCase)))
                    {
                        voteBool = false;
                    }
                    if (voteBool.HasValue)
                    {
                        var player = PlayerController.GetPlayerByUser(user);
                        if (BettingController.PlaceBet(player, amount, voteBool.Value))
                        {
                            return new CommandResult($"You bet {amount} Wolfcoins on \"{(voteBool.Value ? "succeed" : "fail")}\".");
                        }
                        if (player.Currency < amount)
                        {
                            return new CommandResult($"You can't bet {amount} because you only have {player.Currency} Wolfcoins!");
                        }
                        return new CommandResult($"You have already placed a bet!");
                    }
                    return new CommandResult("Invalid vote, use \"succeed\" for success or \"fail\" for failure");
                }
                return new CommandResult("Betting has already closed!");
            }
            return new CommandResult("There is no bet currently active.");
        }

        public CommandResult StartBet(string message)
        {
            BettingController.StartBet();
            return new CommandResult(true, $"New bet started: {message} Type '!bet succeed {{amount}}' or '!bet fail {{amount}}' to bet.");
        }

        public CommandResult CloseBet()
        {
            BettingController.CloseBet();
            return new CommandResult(true, "Bets are now closed! Good luck FrankerZ");
        }

        public CommandResult ResolveBet(string vote)
        {
            if (BettingController.IsActive)
            {
                bool? voteBool = null;
                if (YesVotes.Any(x => x.Equals(vote, StringComparison.OrdinalIgnoreCase)))
                {
                    voteBool = true;
                }
                else if (NoVotes.Any(x => x.Equals(vote, StringComparison.OrdinalIgnoreCase)))
                {
                    voteBool = false;
                }
                if (voteBool.HasValue)
                {
                    BettingController.Resolve(voteBool.Value);
                }
                return new CommandResult("Invalid outcome, use \"succeed\" for success or \"fail\" for failure");
            }
            return new CommandResult("There is no bet currently active.");
        }

        public CommandResult CancelBet()
        {
            if (BettingController.CancelBet())
            {
                return new CommandResult(true, "The current bet has ben canceled, all Wolfcoins wagered have been refunded.");
            }
            return new CommandResult("There is no bet currently active.");
        }
    }
}
