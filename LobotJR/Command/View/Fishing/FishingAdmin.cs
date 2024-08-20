using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;

namespace LobotJR.Command.View.Fishing
{
    /// <summary>
    /// View containing commands for debugging fishing and fishing
    /// tournaments.
    /// </summary>
    public class FishingAdmin : ICommandView
    {
        private readonly FishingController FishingController;
        private readonly TournamentController TournamentController;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Fishing.Admin";
        /// <summary>
        /// Invoked to notify users when they level up.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public FishingAdmin(FishingController fishingController, TournamentController tournamentController, SettingsManager settingsManager)
        {
            FishingController = fishingController;
            TournamentController = tournamentController;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("DebugTournament", this, CommandMethod.GetInfo(DebugTournament), "debugtournament", "debug-tournament"),
                new CommandHandler("DebugCatch", this, CommandMethod.GetInfo(DebugCatch), "debugcatch", "debug-catch")
            };
        }

        public CommandResult DebugTournament()
        {
            TournamentController.StartTournament();
            return new CommandResult(true);
        }

        public CommandResult DebugCatch()
        {
            var settings = SettingsManager.GetGameSettings();
            var fisher = new Fisher() { User = new User("", "") };
            var output = new List<string>();
            for (var i = 0; i < 50; i++)
            {
                FishingController.HookFish(fisher, settings.FishingUseNormalRarity);
                var fish = FishingController.CalculateFishSizes(fisher, settings.FishingUseNormalSizes);
                output.Add($"{fish.Fish.Name} ({fish.Fish.Rarity.Name}) caght.");
            }
            return new CommandResult(true) { Debug = output };
        }
    }
}
