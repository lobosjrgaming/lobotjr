using LobotJR.Command.Model.Pets;
using LobotJR.Command.System.General;
using LobotJR.Command.System.Pets;
using LobotJR.Command.System.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;

namespace LobotJR.Command.Module.Pets
{
    /// <summary>
    /// Module containing commands for managing player pets.
    /// </summary>

    public class PetModule : ICommandModule
    {
        /// <summary>
        /// Gets the name of the type of pet in a stable, properly formatted.
        /// </summary>
        /// <param name="stable">The stable record to get the pet name from.</param>
        /// <returns>The name of the type of pet in stable record.</returns>
        public static string GetPetName(Stable stable)
        {
            return stable.IsSparkly ? $"✨{stable.Pet.Name}✨" : stable.Pet.Name;
        }

        private readonly PetSystem PetSystem;
        private readonly PlayerSystem PlayerSystem;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Pets";
        /// <summary>
        /// Invoked to handle responses to confirmation events for deleting
        /// pets, and to send messages to users and general chat when a pet is
        /// found.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public PetModule(PetSystem petSystem, PlayerSystem playerSystem, ConfirmationSystem confirmationSystem, SettingsManager settingsManager)
        {
            PetSystem = petSystem;
            PlayerSystem = playerSystem;
            PetSystem.PetFound += PetSystem_PetFound;
            PetSystem.PetWarning += PetSystem_PetWarning;
            PetSystem.PetDeath += PetSystem_PetDeath;
            confirmationSystem.Confirmed += ConfirmationSystem_Confirmed;
            confirmationSystem.Canceled += ConfirmationSystem_Canceled;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("ListPets", this, CommandMethod.GetInfo(ListPets), "pets", "stable"),
                new CommandHandler("DetailPet", this, CommandMethod.GetInfo<int>(DescribePet), "pet"),
                new CommandHandler("RenamePet", this, CommandMethod.GetInfo<int, string>(RenamePet), "rename"),
                new CommandHandler("FeedPet", this, CommandMethod.GetInfo<int>(FeedPet), "feed"),
                new CommandHandler("ActivatePet", this, CommandMethod.GetInfo<int>(ActivatePet), "summon"),
                new CommandHandler("DeactivatePet", this, CommandMethod.GetInfo(DeactivatePet), "dismiss"),
                new CommandHandler("DeletePet", this, CommandMethod.GetInfo<int>(DeletePet), "release"),
            };
        }

        private void PetSystem_PetDeath(User user, Stable stable)
        {
            PushNotification?.Invoke(user, new CommandResult($"{stable.Name} starved to death."));
        }

        private void PetSystem_PetWarning(User user, Stable stable)
        {
            var message = stable.Hunger <= 10
                ? $"{stable.Name} is very hungry and will die if you don't feed it soon!"
                : $"{stable.Name} is hungry! Be sure to !feed them!";
            PushNotification?.Invoke(user, new CommandResult(message));
        }

        private void PetSystem_PetFound(User user, Stable stable)
        {
            var result = new CommandResult(user);
            var userStable = PetSystem.GetStableForUser(user);
            string responseString = string.Empty;
            string messageString;
            var petName = GetPetName(stable);
            if (stable.IsSparkly)
            {
                responseString = " WOW! And it's a sparkly version! Lucky you!";
                messageString = $"WOW! {user.Username} just found a SPARKLY pet {petName}! What luck!";
            }
            else
            {
                responseString = string.Empty;
                messageString = $"{user.Username} just found a pet {petName}!";
            }
            if (userStable.Count() == 1)
            {
                responseString = $"You found your first pet! You now have a pet {petName}. Whisper me !pethelp for more info." + responseString;
            }
            else
            {
                responseString = $"You found a new pet buddy! You earned a {petName} pet!" + responseString;
            }
            if (stable.IsSparkly)
            {
            }
            result.Responses.Add(responseString);
            if (userStable.Count() == PetSystem.GetPets().Count())
            {
                result.Responses.Add("You've collected all of the available pets! Congratulations!");
            }
            result.Messages.Add(messageString);
            PushNotification?.Invoke(user, result);
        }

        private void ConfirmationSystem_Confirmed(User user)
        {
            var toRelease = PetSystem.IsFlaggedForDelete(user);
            if (toRelease != null)
            {
                var name = toRelease.Name;
                PetSystem.DeletePet(toRelease);
                PushNotification?.Invoke(user, new CommandResult($"You released {name}. Goodbye, {name}!"));
            }
        }

        private void ConfirmationSystem_Canceled(User user)
        {
            var toRelease = PetSystem.IsFlaggedForDelete(user);
            if (toRelease != null)
            {
                PetSystem.UnflagForDelete(user);
                PushNotification?.Invoke(user, new CommandResult($"You decided to keep {toRelease}."));
            }
        }

        private CommandResult CreateDefaultResult()
        {
            return new CommandResult("You have no pets.");
        }

        private IEnumerable<string> DescribePet(Stable stable, int index, bool includeStats)
        {
            var indexString = $"[{index}]";
            if (stable.IsActive)
            {
                indexString = $"<{indexString}>";
            }
            var nameString = GetPetName(stable);
            var output = new List<string>()
            {
                $"{indexString} {stable.Name} the {nameString} ({stable.Pet.Rarity.Name})"
            };
            if (includeStats)
            {
                output.Add($"Level: {stable.Level} | Affection: {stable.Affection} | Energy: {stable.Hunger}");
                output.Add($"Status: {(stable.IsActive ? "Active" : "Stabled")} | Sparkly?: {(stable.IsSparkly ? "Yes!" : "No")}");
            }
            return output;
        }

        public CommandResult ListPets(User user)
        {
            var stable = PetSystem.GetStableForUser(user);
            if (stable.Any())
            {
                var responses = new List<string>() { $"You have {stable.Count()} pets: " };
                var index = 1;
                foreach (var pet in stable)
                {
                    responses.AddRange(DescribePet(pet, index++, false));
                }
                return new CommandResult(responses.ToArray());
            }
            return CreateDefaultResult();
        }

        public CommandResult DescribePet(User user, int index)
        {
            var stable = PetSystem.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    return new CommandResult(DescribePet(pet, stable.ToList().IndexOf(pet) + 1, true).ToArray());
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }

        public CommandResult RenamePet(User user, int index, string name)
        {
            var stable = PetSystem.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    if (name.Length <= 16)
                    {
                        var response = $"{pet.Name} was renamed to {name}!";
                        pet.Name = name;
                        return new CommandResult(response);
                    }
                    return new CommandResult("Name can only be 16 characters max.");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }

        public CommandResult FeedPet(User user, int index)
        {
            var stable = PetSystem.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    var petLevel = pet.Level;
                    var settings = SettingsManager.GetGameSettings();
                    if (PetSystem.IsHungry(pet))
                    {
                        var player = PlayerSystem.GetPlayerByUser(user);
                        if (PetSystem.Feed(player, pet))
                        {
                            var output = new List<string>() { $"You were charged {settings.PetFeedingCost} wolfcoins to feed {pet.Name}. They feel refreshed!" };
                            if (petLevel > pet.Level)
                            {
                                output.Add($"{pet.Name} leveled up! They are now level {pet.Level}.");
                            }
                            return new CommandResult(output.ToArray());
                        }
                        return new CommandResult($"You lack the {settings.PetFeedingCost} wolfcoins to feed your pet! Hop in a Lobos stream soon!");
                    }
                    return new CommandResult($"{pet.Name} is full and doesn't need to eat!");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }

        public CommandResult ActivatePet(User user, int index)
        {
            var stable = PetSystem.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    if (!pet.IsActive)
                    {
                        var active = PetSystem.ActivatePet(user, pet);
                        var dismissMessage = "";
                        if (active != null)
                        {
                            dismissMessage = $" and sent {active.Name} back to the stable";
                        }
                        var output = $"You summoned {pet.Name}{dismissMessage}.";
                    }
                    return new CommandResult($"{pet.Name} is already summoned.");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }

        public CommandResult DeactivatePet(User user)
        {
            var active = PetSystem.DeactivatePet(user);
            if (active != null)
            {
                return new CommandResult($"You dismissed {active.Name}.");
            }
            return new CommandResult("You do not have a pet summoned.");
        }

        public CommandResult DeletePet(User user, int index)
        {
            var stable = PetSystem.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    if (PetSystem.FlagForDelete(user, pet))
                    {
                        return new CommandResult($"If you release {pet.Name}, they will be gone forever. Are you sure you want to release them? (!y/!n)");
                    }
                    var flagged = PetSystem.IsFlaggedForDelete(user);
                    return new CommandResult($"You are already trying to release {flagged.Name}. Are you sure you want to release them? (!y/!n)");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }
    }
}
