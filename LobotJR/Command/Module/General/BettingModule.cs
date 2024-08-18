using LobotJR.Command.System.General;
using LobotJR.Command.System.Player;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.General
{
    /// <summary>
    /// There was code in Program.cs for a betting system, but most of the code
    /// was commented out.
    /// This class, combined with the matching system, has the functionality of
    /// that code fully implemented, but is not registered with autofac so it
    /// won't be loaded.
    /// </summary>
    public class BettingModule : ICommandModule
    {
        private static readonly IEnumerable<string> YesVotes = new List<string>() { "y", "yes", "t", "true", "1", "succeed" };
        private static readonly IEnumerable<string> NoVotes = new List<string>() { "n", "no", "f", "false", "2", "fail" };

        private readonly BettingSystem BettingSystem;
        private readonly PlayerSystem PlayerSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Betting";
        /// <summary>
        /// Invoked to notify players of group invitations, group chat, and
        /// dungeon progress.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public BettingModule(BettingSystem bettingSystem, PlayerSystem playerSystem)
        {
            BettingSystem = bettingSystem;
            PlayerSystem = playerSystem;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("PlaceBet", this, CommandMethod.GetInfo<int, string>(PlaceBet), "bet"),

                // These are admin commands and will need to be moved into
                // their own module for access control if this gets implemented
                // I just put admin. in front as a hacky way to indicate this
                new CommandHandler("Admin.StartBet", this, CommandMethod.GetInfo<string>(StartBet), "startbet"),
                new CommandHandler("Admin.CloseBet", this, CommandMethod.GetInfo(CloseBet), "closebet"),
                new CommandHandler("Admin.ResolveBet", this, CommandMethod.GetInfo<string>(ResolveBet), "resolvebet", "endbet"),
            };
        }

        public CommandResult PlaceBet(User user, int amount, string vote)
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
                var player = PlayerSystem.GetPlayerByUser(user);
                BettingSystem.PlaceBet(player, amount, voteBool.Value);
                return new CommandResult($"You bet {amount} Wolfcoins on \"{(voteBool.Value ? "succeed" : "fail")}\".");
            }
            return new CommandResult("Invalid vote, use \"succeed\" for success or \"fail\" for failure");
        }

        public CommandResult StartBet(string message)
        {
            BettingSystem.StartBet();
            return new CommandResult(true, $"New bet started: {message} Type '!bet succeed {{amount}}' or '!bet fail {{amount}}' to bet.");
        }

        public CommandResult CloseBet()
        {
            BettingSystem.CloseBet();
            return new CommandResult(true, "Bets are now closed! Good luck FrankerZ");
        }

        public CommandResult ResolveBet(string vote)
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
                BettingSystem.Resolve(voteBool.Value);
            }
            return new CommandResult("Invalid outcome, use \"succeed\" for success or \"fail\" for failure");
        }
    }
}
