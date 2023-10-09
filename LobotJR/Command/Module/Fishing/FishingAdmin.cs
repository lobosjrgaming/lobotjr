﻿using LobotJR.Command.Model.Fishing;
using LobotJR.Command.System.Fishing;
using LobotJR.Twitch.Model;
using System.Collections.Generic;

namespace LobotJR.Command.Module.Fishing
{
    public class FishingAdmin : ICommandModule
    {
        private readonly FishingSystem FishingSystem;
        private readonly TournamentSystem TournamentSystem;

        public string Name => "Fishing.Admin";

        public event PushNotificationHandler PushNotification;

        public IEnumerable<CommandHandler> Commands { get; private set; }

        public IEnumerable<ICommandModule> SubModules => null;

        public FishingAdmin(FishingSystem fishingSystem, TournamentSystem tournamentSystem)
        {
            FishingSystem = fishingSystem;
            TournamentSystem = tournamentSystem;
            Commands = new List<CommandHandler>(new CommandHandler[]
            {
                new CommandHandler("DebugTournament", DebugTournament, "debugtournament", "debug-tournament"),
                new CommandHandler("DebugCatch", DebugCatch, "debugcatch", "debug-catch")
            }); ;
        }

        public CommandResult DebugTournament(string data, User user)
        {
            TournamentSystem.StartTournament();
            return new CommandResult() { Sender = user, Processed = true };
        }

        public CommandResult DebugCatch(string data, User user)
        {
            var fisher = new Fisher() { User = new User("", "") };
            var output = new List<string>();
            for (var i = 0; i < 50; i++)
            {
                FishingSystem.HookFish(fisher);
                var fish = FishingSystem.CalculateFishSizes(fisher);
                output.Add($"{fish.Fish.Name} ({fish.Fish.Rarity.Name}) caght.");
            }
            return new CommandResult() { Sender = user, Processed = true, Debug = output };
        }
    }
}
