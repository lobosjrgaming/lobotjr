using LobotJR.Command.Model.Fishing;
using LobotJR.Command.System.Fishing;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;

namespace LobotJR.Command.Module.Fishing
{
    /// <summary>
    /// Module containing commands for debugging fishing and fishing
    /// tournaments.
    /// </summary>
    public class FishingAdmin : ICommandModule
    {
        private readonly FishingSystem FishingSystem;
        private readonly TournamentSystem TournamentSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Fishing.Admin";
        /// <summary>
        /// Invoked to notify users when they level up.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public FishingAdmin(FishingSystem fishingSystem, TournamentSystem tournamentSystem)
        {
            FishingSystem = fishingSystem;
            TournamentSystem = tournamentSystem;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DebugTournament", this, CommandMethod.GetInfo(DebugTournament), "debugtournament", "debug-tournament"),
                new CommandHandler("DebugCatch", this, CommandMethod.GetInfo(DebugCatch), "debugcatch", "debug-catch")
            };
        }

        public CommandResult DebugTournament(IDatabase database)
        {
            TournamentSystem.StartTournament(database);
            return new CommandResult(true);
        }

        public CommandResult DebugCatch(IDatabase database)
        {
            var settings = SettingsManager.GetGameSettings(database);
            var fisher = new Fisher() { User = new User("", "") };
            var output = new List<string>();
            for (var i = 0; i < 50; i++)
            {
                FishingSystem.HookFish(database, fisher, settings.FishingUseNormalRarity);
                var fish = FishingSystem.CalculateFishSizes(fisher, settings.FishingUseNormalSizes);
                output.Add($"{fish.Fish.Name} ({fish.Fish.Rarity.Name}) caght.");
            }
            return new CommandResult(true) { Debug = output };
        }
    }
}
