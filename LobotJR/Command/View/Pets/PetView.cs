using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Pets;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;

namespace LobotJR.Command.View.Pets
{
    /// <summary>
    /// View containing commands for managing player pets.
    /// </summary>

    public class PetView : ICommandView, IPushNotifier
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

        /// <summary>
        /// Gets the display to use for a pet when sending updates about a pet.
        /// This includes all information needed to identify a specific pet in
        /// case of duplicate names.
        /// </summary>
        /// <param name="stable">The stable record to get the pet display for.</param>
        /// <returns>The name and type of pet in a formatted string.</returns>
        public static string GetPetDisplay(Stable stable)
        {
            return $"{stable.Name} the {(stable.IsSparkly ? "sparkly " : "")}{stable.Pet.Name}";
        }

        private readonly PetController PetController;
        private readonly PlayerController PlayerController;
        private readonly SettingsManager SettingsManager;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Pets";
        /// <summary>
        /// Invoked to handle responses to confirmation events for deleting
        /// pets, and to send messages to users and general chat when a pet is
        /// found.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public PetView(PetController petController, PlayerController playerController, ConfirmationController confirmationController, SettingsManager settingsManager)
        {
            PetController = petController;
            PlayerController = playerController;
            PetController.PetFound += PetController_PetFound;
            PetController.PetWarning += PetController_PetWarning;
            PetController.PetDeath += PetController_PetDeath;
            confirmationController.Confirmed += ConfirmationController_Confirmed;
            confirmationController.Canceled += ConfirmationController_Canceled;
            SettingsManager = settingsManager;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("ListPets", this, CommandMethod.GetInfo(ListPets), CommandMethod.GetInfo(ListPetsCompact), "pets", "stable"),
                new CommandHandler("DetailPet", this, CommandMethod.GetInfo<int>(DescribePet), "pet"),
                new CommandHandler("RenamePet", this, CommandMethod.GetInfo<int, string>(RenamePet), "rename"),
                new CommandHandler("FeedPet", this, CommandMethod.GetInfo<int>(FeedPet), "feed"),
                new CommandHandler("ActivatePet", this, CommandMethod.GetInfo<int>(ActivatePet), "summon"),
                new CommandHandler("DeactivatePet", this, CommandMethod.GetInfo(DeactivatePet), "dismiss"),
                new CommandHandler("DeletePet", this, CommandMethod.GetInfo<int>(DeletePet), "release"),
            };
        }

        private void PetController_PetDeath(User user, Stable stable)
        {
            PushNotification?.Invoke(user, new CommandResult($"{GetPetDisplay(stable)} starved to death."));
        }

        private void PetController_PetWarning(User user, Stable stable)
        {
            var message = stable.Hunger <= 10
                ? $"{GetPetDisplay(stable)} is very hungry and will die if you don't feed it soon!"
                : $"{GetPetDisplay(stable)} is hungry! Be sure to !feed them!";
            PushNotification?.Invoke(user, new CommandResult(message));
        }

        private void PetController_PetFound(User user, Stable stable)
        {
            var result = new CommandResult(user);
            var userStable = PetController.GetStableForUser(user).ToList();
            userStable.Add(stable);
            string responseString;
            string messageString;
            var petName = GetPetName(stable);
            if (stable.IsSparkly)
            {
                messageString = $"WOW! {user.Username} just found a SPARKLY pet {petName}! What luck!";
            }
            else
            {
                messageString = $"{user.Username} just found a pet {petName}!";
            }
            if (userStable.Count() == 1)
            {
                responseString = $"You found your first pet! You now have a pet {petName}. Whisper me !pethelp for more info.";
            }
            else
            {
                responseString = $"You found a new pet buddy! You earned a {petName} pet!";
            }
            if (stable.IsSparkly)
            {
                responseString += " WOW! And it's a sparkly version! Lucky you!";
            }
            result.Responses.Add(responseString);
            var allPets = PetController.GetPets().Select(x => x.Id).Distinct();
            var normalPets = userStable.Where(x => !x.IsSparkly).Select(x => x.Id).Distinct();
            var sparklyPets = userStable.Where(x => x.IsSparkly).Select(x => x.Id).Distinct();
            if (!allPets.Except(normalPets).Any() && !allPets.Except(sparklyPets).Any())
            {
                result.Responses.Add("You've collected all of the available pets! Congratulations!");
            }
            result.Messages.Add(messageString);
            PushNotification?.Invoke(user, result);
        }

        private void ConfirmationController_Confirmed(User user)
        {
            var toRelease = PetController.IsFlaggedForDelete(user);
            if (toRelease != null)
            {
                var name = toRelease.Name;
                var display = GetPetDisplay(toRelease);
                PetController.DeletePet(toRelease);
                PushNotification?.Invoke(user, new CommandResult($"You released {display}. Goodbye, {name}!"));
            }
        }

        private void ConfirmationController_Canceled(User user)
        {
            var toRelease = PetController.IsFlaggedForDelete(user);
            if (toRelease != null)
            {
                PetController.UnflagForDelete(user);
                PushNotification?.Invoke(user, new CommandResult($"You decided to keep {toRelease.Name}."));
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

        public CompactCollection<Stable> ListPetsCompact(User user)
        {
            var stable = PetController.GetStableForUser(user);
            return new CompactCollection<Stable>(stable, x => $"{x.PetId}|{x.Pet.Name}|{x.Pet.Description}|{x.Pet.RarityId}|{x.Name}|{(x.IsSparkly ? "S" : "")}|{x.Level}|{x.Experience}|{x.Affection}|{x.Hunger}|{(x.IsActive ? "A" : "")};");
        }

        public CommandResult ListPets(User user)
        {
            var stable = PetController.GetStableForUser(user);
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
            var stable = PetController.GetStableForUser(user);
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
            var stable = PetController.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    if (name.Length <= 16)
                    {
                        var response = $"{GetPetDisplay(pet)} was renamed to {name}!";
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
            var stable = PetController.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    var petLevel = pet.Level;
                    var settings = SettingsManager.GetGameSettings();
                    if (PetController.IsHungry(pet))
                    {
                        var player = PlayerController.GetPlayerByUser(user);
                        if (PetController.Feed(player, pet))
                        {
                            var output = new List<string>() { $"You were charged {settings.PetFeedingCost} wolfcoins to feed {GetPetDisplay(pet)}. They feel refreshed!" };
                            if (pet.Level > petLevel)
                            {
                                output.Add($"{pet.Name} leveled up! They are now level {pet.Level}.");
                            }
                            return new CommandResult(output.ToArray());
                        }
                        return new CommandResult($"You lack the {settings.PetFeedingCost} wolfcoins to feed your pet! Hop in a Lobos stream soon!");
                    }
                    return new CommandResult($"{GetPetDisplay(pet)} is full and doesn't need to eat!");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }

        public CommandResult ActivatePet(User user, int index)
        {
            var stable = PetController.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    if (!pet.IsActive)
                    {
                        var active = PetController.ActivatePet(user, pet);
                        var dismissMessage = "";
                        if (active != null)
                        {
                            dismissMessage = $" and sent {GetPetDisplay(active)} back to the stable";
                        }
                        return new CommandResult($"You summoned {GetPetDisplay(pet)}{dismissMessage}.");
                    }
                    return new CommandResult($"{pet.Name} is already summoned.");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }

        public CommandResult DeactivatePet(User user)
        {
            var active = PetController.DeactivatePet(user);
            if (active != null)
            {
                return new CommandResult($"You dismissed {GetPetDisplay(active)}.");
            }
            return new CommandResult("You do not have a pet summoned.");
        }

        public CommandResult DeletePet(User user, int index)
        {
            var stable = PetController.GetStableForUser(user);
            if (stable.Any())
            {
                var pet = stable.ElementAtOrDefault(index - 1);
                if (pet != null)
                {
                    if (PetController.FlagForDelete(user, pet))
                    {
                        return new CommandResult($"If you release {GetPetDisplay(pet)}, they will be gone forever. Are you sure you want to release them? (!y/!n)");
                    }
                    var flagged = PetController.IsFlaggedForDelete(user);
                    return new CommandResult($"You are already trying to release {GetPetDisplay(flagged)}. Are you sure you want to release them? (!y/!n)");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {stable.Count()}.");
            }
            return CreateDefaultResult();
        }
    }
}
