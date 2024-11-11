using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Controller.Pets
{
    /// <summary>
    /// Controller for managing pets.
    /// </summary>
    public class PetController
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly PlayerController PlayerController;
        private readonly SettingsManager SettingsManager;
        private readonly Dictionary<string, int> PendingRelease = new Dictionary<string, int>();
        private readonly Random Random = new Random();

        /// <summary>
        /// Event handler for events related to finding a pet.
        /// </summary>
        /// <param name="user">The user that found the pet.</param>
        /// <param name="stable">The stable record for the pet that was found.</param>
        public delegate void PetFoundHandler(User user, Stable stable);
        /// <summary>
        /// Event fired when a user finds a pet.
        /// </summary>
        public event PetFoundHandler PetFound;

        /// <summary>
        /// Event handler for pet death events.
        /// </summary>
        /// <param name="user">The user that owns the stable.</param>
        /// <param name="stable">The stable entry for the pet that died.</param>
        public delegate void PetDeathHandler(User user, Stable stable);
        /// <summary>
        /// Event fired when a pet dies.
        /// </summary>
        public event PetDeathHandler PetDeath;

        /// <summary>
        /// Event handler for low pet hunger warnings.
        /// </summary>
        /// <param name="user">The user that owns the stable.</param>
        /// <param name="stable">The stable entry for the pet that is hungry.</param>
        public delegate void PetWarningHandler(User user, Stable stable);
        /// <summary>
        /// Event fired when a pet's hunger is low.
        /// </summary>
        public event PetWarningHandler PetWarning;

        public PetController(IConnectionManager connectionManager, PlayerController playerController, SettingsManager settingsManager)
        {
            ConnectionManager = connectionManager;
            PlayerController = playerController;
            SettingsManager = settingsManager;
        }

        /// <summary>
        /// Clears all pending releases.
        /// </summary>
        public void ClearPendingReleases()
        {
            PendingRelease.Clear();
        }

        /// <summary>
        /// Gets all stable entries for a given user.
        /// </summary>
        /// <param name="user">The user to get stables for.</param>
        /// <returns>A collection of stables for the given user.</returns>
        public IEnumerable<Stable> GetStableForUser(User user)
        {
            return ConnectionManager.CurrentConnection.Stables.Read(x => x.UserId.Equals(user.TwitchId)).OrderBy(x => x.PetId);
        }

        private Stable GetActivePet(string userId)
        {
            return ConnectionManager.CurrentConnection.Stables.Read(x => x.UserId.Equals(userId) && x.IsActive).FirstOrDefault();
        }

        /// <summary>
        /// Gets the active pet for a given user.
        /// </summary>
        /// <param name="user">The user to get pets for.</param>
        /// <returns>The active pet for the user.</returns>
        public Stable GetActivePet(User user)
        {
            return GetActivePet(user.TwitchId);
        }

        /// <summary>
        /// Gets the active pet for a given player.
        /// </summary>
        /// <param name="player">The player to get pets for.</param>
        /// <returns>The active pet for the player.</returns>
        public Stable GetActivePet(PlayerCharacter player)
        {
            return GetActivePet(player.UserId);
        }

        /// <summary>
        /// Activates a pet, and dismisses all other pets.
        /// </summary>
        /// <param name="user">The user who owns the stable.</param>
        /// <param name="stable">The stable record to activate.</param>
        /// <returns>The previously active pet.</returns>
        public Stable ActivatePet(User user, Stable stable)
        {
            var active = DeactivatePet(user);
            stable.IsActive = true;
            return active;
        }

        /// <summary>
        /// Deactivates all active pets for a user.
        /// </summary>
        /// <param name="user">The user to deactivate pets for.</param>
        /// <returns>The pet that was deactivated.</returns>
        public Stable DeactivatePet(User user)
        {
            var active = GetActivePet(user);
            if (active != null)
            {
                active.IsActive = false;
            }
            return active;
        }

        private Stable CreateStable(User user, Pet pet, bool isSparkly)
        {
            var max = SettingsManager.GetGameSettings().PetHungerMax;
            return new Stable()
            {
                Name = pet.Name,
                UserId = user.TwitchId,
                Pet = pet,
                IsSparkly = isSparkly,
                Hunger = max,
                Level = 1
            };
        }

        /// <summary>
        /// Creates a new stable record and adds it to the database.
        /// </summary>
        /// <param name="user">The user to add the record for.</param>
        /// <param name="pet">The pet to add.</param>
        /// <param name="isSparkly">Whether the pet is sparkly.</param>
        /// <returns>The stable record that was added.</returns>
        public Stable AddStableRecord(User user, Pet pet, bool isSparkly = false)
        {
            var stable = CreateStable(user, pet, isSparkly);
            ConnectionManager.CurrentConnection.Stables.Create(stable);
            return stable;
        }

        /// <summary>
        /// Deletes a pet, permanently removing it from a player's stable.
        /// </summary>
        /// <param name="stable">The stable record to delete.</param>
        public void DeletePet(Stable stable)
        {
            ConnectionManager.CurrentConnection.Stables.Delete(stable);
        }

        /// <summary>
        /// Checks if a pet is hungry and can be fed.
        /// </summary>
        /// <param name="stable">The stable record to check.</param>
        /// <returns>True if the pet in the stable record can be fed.</returns>
        public bool IsHungry(Stable stable)
        {
            var settings = SettingsManager.GetGameSettings();
            return stable.Hunger < settings.PetHungerMax;
        }

        /// <summary>
        /// Feeds a pet in a player's stable.
        /// </summary>
        /// <param name="player">The player that owns the stable.</param>
        /// <param name="stable">The stable record to feed.</param>
        /// <returns>True if the feed operation succeeded.</returns>
        public bool Feed(PlayerCharacter player, Stable stable)
        {
            var settings = SettingsManager.GetGameSettings();
            if (player.Currency >= settings.PetFeedingCost)
            {
                player.Currency -= settings.PetFeedingCost;
                stable.Experience += (settings.PetHungerMax - stable.Hunger);
                if (stable.Experience >= settings.PetExperienceToLevel && stable.Level < settings.PetLevelMax)
                {
                    stable.Level++;
                    stable.Experience -= settings.PetExperienceToLevel;
                }
                stable.Affection += settings.PetFeedingAffection;
                stable.Hunger = settings.PetHungerMax;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds hunger and processes other effects triggered when a pet is
        /// taken through a dungeon.
        /// </summary>
        /// <param name="player">The player that own the stable entry.</param>
        /// <param name="stable">The stable entry for the pet.</param>
        /// <returns></returns>
        public void AddHunger(PlayerCharacter player, Stable stable)
        {
            int hungerToLose = Random.Next(5, 5 + 6);
            stable.Hunger -= hungerToLose;
            stable.Affection = Math.Max(0, stable.Affection - 1);
            var settings = SettingsManager.GetGameSettings();

            if (stable.Hunger <= 0)
            {
                DeletePet(stable);
                PetDeath?.Invoke(PlayerController.GetUserByPlayer(player), stable);
            }
            else if (stable.Hunger <= settings.PetHungerMax * 0.25f)
            {
                PetWarning?.Invoke(PlayerController.GetUserByPlayer(player), stable);
            }
        }

        /// <summary>
        /// Flags a pet for release.
        /// </summary>
        /// <param name="user">The user triggering the release.</param>
        /// <param name="stable">The stable record of the pet to release.</param>
        /// <returns>True if the release was flagged, false if the user is
        /// already trying to release a pet.</returns>
        public bool FlagForDelete(User user, Stable stable)
        {
            if (PendingRelease.ContainsKey(user.TwitchId))
            {
                return false;
            }
            PendingRelease.Add(user.TwitchId, stable.Id);
            return true;
        }

        /// <summary>
        /// Removes all pending delete flags for a given user.
        /// </summary>
        /// <param name="user">The user to unflag.</param>
        public void UnflagForDelete(User user)
        {
            if (PendingRelease.ContainsKey(user.TwitchId))
            {
                PendingRelease.Remove(user.TwitchId);
            }
        }

        /// <summary>
        /// Checks if a user is trying to release a pet.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <returns>The stable object of the pet the user is trying to
        /// release, or null if the user is not trying to release a pet.</returns>
        public Stable IsFlaggedForDelete(User user)
        {
            if (PendingRelease.TryGetValue(user.TwitchId, out var stableId))
            {
                return ConnectionManager.CurrentConnection.Stables.ReadById(stableId);
            }
            return null;
        }

        /// <summary>
        /// Gets the data for all available pets.
        /// </summary>
        /// <returns>A collection of pet data.</returns>
        public IEnumerable<Pet> GetPets()
        {
            return ConnectionManager.CurrentConnection.PetData.Read();
        }

        /// <summary>
        /// Gets a collection of all possible pet rarities.
        /// </summary>
        /// <returns>A collection of pet rarity data.</returns>
        public IEnumerable<PetRarity> GetRarities()
        {
            return ConnectionManager.CurrentConnection.PetRarityData.Read().OrderBy(x => x.DropRate);
        }

        /// <summary>
        /// Rolls the random chance for a user to receive a pet, and returns
        /// the rarity of the pet they should receive. If the roll determines
        /// no pet should be given, then it will return null.
        /// </summary>
        /// <returns>The rarity of pet to be given, or null if no pet should be
        /// given.</returns>
        public PetRarity RollForRarity()
        {
            var petRarities = ConnectionManager.CurrentConnection.PetRarityData.Read().OrderBy(x => x.DropRate);
            var roll = Random.NextDouble();
            return petRarities.FirstOrDefault(x => roll < x.DropRate);
        }

        /// <summary>
        /// Grants a pet to a user of the given rarity. If the user already has
        /// all pets of the specified rarity, then no pet will be granted.
        /// </summary>
        /// <param name="user">The user to grant a pet to.</param>
        /// <param name="rarity">The rarity of pet to grant</param>
        /// <returns>The stable record of the newly granted pet.</returns>
        public Stable GrantPet(User user, PetRarity rarity)
        {
            var availablePets = ConnectionManager.CurrentConnection.PetData.Read(x => x.RarityId.Equals(rarity.Id)).ToList();
            var isSparkly = Random.NextDouble() < 0.01f;
            var stable = GetStableForUser(user).Where(x => x.IsSparkly == isSparkly).Select(x => x.Pet).ToList();
            var unownedPets = availablePets.Except(stable);
            var max = SettingsManager.GetGameSettings().PetHungerMax;
            if (unownedPets.Any())
            {
                var toGrant = unownedPets.ElementAt(Random.Next(0, unownedPets.Count()));
                var toAdd = CreateStable(user, toGrant, isSparkly);
                ConnectionManager.CurrentConnection.Stables.Create(toAdd);
                PetFound?.Invoke(user, toAdd);
                return toAdd;
            }
            return null;
        }
    }
}
