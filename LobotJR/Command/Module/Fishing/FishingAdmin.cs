using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Controller.Fishing;
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
        private readonly FishingController FishingSystem;
        private readonly TournamentController TournamentSystem;
        private readonly SettingsManager SettingsManager;

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

        public FishingAdmin(FishingController fishingSystem, TournamentController tournamentSystem, SettingsManager settingsManager)
        {
            FishingSystem = fishingSystem;
            TournamentSystem = tournamentSystem;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DebugTournament", this, CommandMethod.GetInfo(DebugTournament), "debugtournament", "debug-tournament"),
                new CommandHandler("DebugCatch", this, CommandMethod.GetInfo(DebugCatch), "debugcatch", "debug-catch")
            };
        }

        public CommandResult DebugTournament()
        {
            TournamentSystem.StartTournament();
            return new CommandResult(true);
        }

        public CommandResult DebugCatch()
        {
            var settings = SettingsManager.GetGameSettings();
            var fisher = new Fisher() { User = new User("", "") };
            var output = new List<string>();
            for (var i = 0; i < 50; i++)
            {
                FishingSystem.HookFish(fisher, settings.FishingUseNormalRarity);
                var fish = FishingSystem.CalculateFishSizes(fisher, settings.FishingUseNormalSizes);
                output.Add($"{fish.Fish.Name} ({fish.Fish.Rarity.Name}) caght.");
            }
            return new CommandResult(true) { Debug = output };
        }
    }
}
