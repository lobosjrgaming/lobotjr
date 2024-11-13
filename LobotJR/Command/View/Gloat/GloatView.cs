using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.Gloat;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Model.Pets;
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
            "A brutal commander of the Wolfpack!",
            "Decorated Chieftain of the Wolfpack!",
            "A WarChieftain of the Wolfpack!",
            "A sacred Wolfpack Justicar",
            "Demigod of the Wolfpack!",
            "A legendary Wolfpack demigod veteran!",
            "The Ultimate Wolfpack God Rank. A truly dedicated individual."
        };

        private readonly GloatController GloatController;
        private readonly LeaderboardController LeaderboardController;
        private readonly PetController PetController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Gloat";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public GloatView(GloatController gloatController, LeaderboardController leaderboardController, PetController petController)
        {
            GloatController = gloatController;
            LeaderboardController = leaderboardController;
            PetController = petController;
            Commands = new CommandHandler[]
            {
                new CommandHandler("GloatLevel", this, CommandMethod.GetInfo(GloatLevel), "gloat", "gloatlevel", "levelgloat"),
                new CommandHandler("GloatPet", this, CommandMethod.GetInfo<int>(GloatPet), "gloatpet", "petgloat"),
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

        public CommandResult GloatPet(User user, int index = 0)
        {
            if (GloatController.CanGloatPet(user))
            {
                if (PetController.GetStableForUser(user).Any())
                {
                    Stable gloatRecord;
                    if (index == 0)
                    {
                        gloatRecord = PetController.GetActivePet(user);
                    }
                    else
                    {
                        var records = PetController.GetStableForUser(user).OrderBy(x => x.PetId).ToList();
                        if (index <= records.Count)
                        {
                            gloatRecord = records.ElementAt(index - 1);
                        }
                        else
                        {
                            return new CommandResult($"Invalid index, please use a number between 1 and {records.Count}.");
                        }
                    }
                    if (gloatRecord != null)
                    {
                        var cost = GloatController.GetPetCost();
                        var success = GloatController.PetGloat(user, gloatRecord);
                        if (success)
                        {
                            return new CommandResult(true, $"{user.Username} watches proudly as their level {gloatRecord.Level} {PetView.GetPetName(gloatRecord)} named {gloatRecord.Name} struts around!")
                            {
                                Responses = new List<string>() { $"You spent {cost} Wolfcoins to brag about {gloatRecord.Name}." }
                            };
                        }
                    }
                    return new CommandResult("You don't have an active pet to show off! Activate one with !summon {id}, or use !gloatpet {id} to gloat about a specific pet.");
                }
                return new CommandResult("You don't have any pets! Run dungeons to find a pet. If you don't have a group, you can use !queue to find one.");
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
