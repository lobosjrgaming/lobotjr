using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Pets;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Pets
{
    /// <summary>
    /// View containing commands for fixing stables in invalid states,
    /// debugging pet functionality, and providing pets to players.
    /// </summary>
    public class PetAdmin : ICommandView
    {
        private readonly Random Random = new Random();
        private readonly PetView PetView;
        private readonly PetController PetController;
        private readonly UserController UserController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Pets.Admin";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public PetAdmin(PetView petView, PetController petController, UserController userController)
        {
            PetView = petView;
            PetController = petController;
            UserController = userController;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("CheckPets", this, CommandMethod.GetInfo<string>(CheckPets), "checkpets"),
                new CommandHandler("GrantPet", this, CommandMethod.GetInfo<int>(GrantPet), "grantpet"),
                new CommandHandler("ClearPets", this, CommandMethod.GetInfo(ClearPets), "clearpets"),
                new CommandHandler("SetHunger", this, CommandMethod.GetInfo<int, int>(SetHunger), "SetHunger"),
            };
        }

        private CommandResult CreateDefaultResult(string user)
        {
            return new CommandResult($"Unable to find stable records for user {user}.");
        }

        public CommandResult CheckPets(string target)
        {
            var user = UserController.GetUserByName(target);
            if (user != null)
            {
                return PetView.ListPets(user);
            }
            return CreateDefaultResult(target);
        }

        public CommandResult GrantPet(User user, int rarity = -1)
        {
            PetRarity rarityToGrant;
            var rarities = PetController.GetRarities();
            if (rarity == -1)
            {
                rarity = Random.Next(0, rarities.Count()) + 1;
            }
            rarityToGrant = rarities.ElementAtOrDefault(rarity - 1);
            if (rarityToGrant != null)
            {
                PetController.GrantPet(user, rarityToGrant);
                return new CommandResult($"Pet granted to {user.Username} of rarity {rarityToGrant.Name}.");
            }
            return new CommandResult($"Invalid rarity index, please specify a value between 1 and {rarities.Count()}.");
        }

        public CommandResult ClearPets(User user)
        {
            var stables = PetController.GetStableForUser(user).ToList();
            foreach (var stable in stables)
            {
                PetController.DeletePet(stable);
            }
            return new CommandResult("Pets cleared.");
        }

        public CommandResult SetHunger(User user, int index, int hunger)
        {
            var stable = PetController.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    pet.Hunger = hunger;
                    return new CommandResult($"{pet.Name}'s energy set to {hunger}.");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return new CommandResult("You don't have any pets.");
        }
    }
}
