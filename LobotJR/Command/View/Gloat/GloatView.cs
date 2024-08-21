using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Gloat;
using LobotJR.Command.View.Pets;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Gloat
{
    /// <summary>
    /// View containing commands that allow players to gloat about their
    /// achievements.
    /// </summary>
    public class GloatView : ICommandView
    {
        private readonly IEnumerable<string> CheerMessages = new List<string>()
        {
            "Just a baby! lobosMindBlank",
            "Scrubtastic!",
            "Pretty weak!",
            "Not too shabby.",
            "They can hold their own!",
            "Getting pretty strong Kreygasm",
            "A formidable opponent!",
            "A worthy adversary!",
            "A most powerful combatant!",
            "A seasoned war veteran!",
            "A fearsome champion of the Wolfpack!",
            "A vicious pack leader!",
            "A famed Wolfpack Captain!",
            "A brutal commandef of the Wolfpack!",
            "Decorated Chieftain of the Wolfpack!",
            "A WarChieftain of the Wolfpack!",
            "A sacred Wolfpack Justicar",
            "Demigod of the Wolfpack!",
            "A legendary Wolfpack demigod veteran!",
            "The Ultimate Wolfpack God Rank. A truly dedicated individual."
        };

        private readonly GloatController GloatController;
        private readonly LeaderboardController LeaderboardController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Gloat";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public GloatView(GloatController gloatController, LeaderboardController leaderboardController)
        {
            GloatController = gloatController;
            LeaderboardController = leaderboardController;
            Commands = new CommandHandler[]
            {
                new CommandHandler("GloatLevel", this, CommandMethod.GetInfo(GloatLevel), "gloat", "gloatlevel", "levelgloat"),
                new CommandHandler("GloatPet", this, CommandMethod.GetInfo(GloatPet), "gloatpet", "petgloat"),
                new CommandHandler("GloatFish", this, CommandMethod.GetInfo<int>(GloatFish), "gloatfish", "fishgloat", "gloat-fish")
            };
        }

        public CommandResult GloatLevel(User user)
        {
            var cost = GloatController.GetLevelCost();
            if (GloatController.CanGloatLevel(user))
            {
                var player = GloatController.LevelGloat(user);
                var levelWithPrestige = $"Level {player.Level}";
                if (player.Prestige > 0)
                {
                    levelWithPrestige += $", Prestige Level {player.Prestige}";
                }
                var cheer = CheerMessages.ElementAtOrDefault(player.Level - 1) ?? "Something broke though...";
                return new CommandResult(true, $"{user.Username} has spent {cost} Wolfcoins to show off that they are {levelWithPrestige}! {cheer}");
            }
            return new CommandResult($"You don't have enough coins to gloat (Cost: {cost} Wolfcoins)");
        }

        public CommandResult GloatPet(User user)
        {
            if (GloatController.CanGloatPet(user))
            {
                var cost = GloatController.GetPetCost();
                var pet = GloatController.PetGloat(user);
                if (pet != null)
                {
                    return new CommandResult(true, $"{user.Username} watches proudly as their level {pet.Level} {PetView.GetPetName(pet)} named {pet.Name} struts around!")
                    {
                        Responses = new List<string>() { $"You spent {cost} Wolfcoins to brag about {pet.Name}." }
                    };
                }
                return new CommandResult("You don't have an active pet to show off! Activate one with !summon {id}");
            }
            return new CommandResult("You don't have enough coins to gloat!");
        }

        public CommandResult GloatFish(User user, int index)
        {
            if (GloatController.CanGloatFishing(user))
            {
                var records = LeaderboardController.GetPersonalLeaderboard(user);
                var cost = GloatController.GetFishCost();
                var max = records.Count();
                if (max > 0)
                {
                    if (index > 0 && index <= max)
                    {
                        var record = GloatController.FishingGloat(user, index - 1);
                        if (record != null)
                        {
                            return new CommandResult($"You spent {cost} wolfcoins to brag about your biggest {record.Fish.Name}.")
                            {
                                Messages = new string[] { $"{user.Username} gloats about the time they caught a {record.Length} in. long, {record.Weight} pound {record.Fish.Name} lobosSmug" }
                            };
                        }
                        return new CommandResult($"An error occurred trying to fetch the fish at index {index}");
                    }
                    return new CommandResult($"Invalid index. Please use a number between 1 and {max}");
                }
                return new CommandResult("You don't have any fish! Type !cast to try and fish for some!");
            }
            return new CommandResult("You don't have enough coins to gloat!");
        }
    }
}
