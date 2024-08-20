using LobotJR.Command.Model.Pets;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.Pets
{
    /// <summary>
    /// Module containing commands for fixing stables in invalid states,
    /// debugging pet functionality, and providing pets to players.
    /// </summary>
    public class PetAdmin : ICommandModule
    {
        private readonly Random Random = new Random();
        private readonly PetModule PetModule;
        private readonly PetController PetSystem;
        private readonly UserController UserSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Pets.Admin";
        /// <summary>
        /// This module does not issue any push notifications. Notifications
        /// for pets being granted is handled by the PetModule.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public PetAdmin(PetModule petModule, PetController petSystem, UserController userSystem)
        {
            PetModule = petModule;
            PetSystem = petSystem;
            UserSystem = userSystem;
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
            var user = UserSystem.GetUserByName(target);
            if (user != null)
            {
                return PetModule.ListPets(user);
            }
            return CreateDefaultResult(target);
        }

        public CommandResult GrantPet(User user, int rarity = -1)
        {
            PetRarity rarityToGrant;
            var rarities = PetSystem.GetRarities();
            if (rarity == -1)
            {
                rarity = Random.Next(0, rarities.Count());
            }
            rarityToGrant = rarities.ElementAtOrDefault(rarity);
            if (rarityToGrant != null)
            {
                PetSystem.GrantPet(user, rarityToGrant);
            }
            return new CommandResult(true);
        }

        public CommandResult ClearPets(User user)
        {
            var stables = PetSystem.GetStableForUser(user).ToList();
            foreach (var stable in stables)
            {
                PetSystem.DeletePet(stable);
            }
            return new CommandResult("Pets cleared.");
        }

        public CommandResult SetHunger(User user, int index, int hunger)
        {
            var stable = PetSystem.GetStableForUser(user);
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
            return CreateDefaultResult(user.Username);
        }
    }
}
