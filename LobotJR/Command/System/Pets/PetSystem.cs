using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Pets
{
    public class PetSystem : ISystem
    {
        private readonly IConnectionManager ConnectionManager;
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

        public PetSystem(IConnectionManager connectionManager, SettingsManager settingsManager)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
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

        /// <summary>
        /// Gets all active pets for a given user. Only one pet is allowed to
        /// be active at a time, but this method returns a collection in case
        /// that rule is somehow violated.
        /// </summary>
        /// <param name="user">The user to get pets for.</param>
        /// <returns>A collection of all active pets for the user.</returns>
        public IEnumerable<Stable> GetActivePet(User user)
        {
            return ConnectionManager.CurrentConnection.Stables.Read(x => x.UserId.Equals(user.TwitchId) && x.IsActive);
        }

        /// <summary>
        /// Activates a pet, and dismisses all other pets.
        /// </summary>
        /// <param name="user">The user who owns the stable.</param>
        /// <param name="stable">The stable record to activate.</param>
        /// <returns>A collection of previously active pets.</returns>
        public IEnumerable<Stable> ActivatePet(User user, Stable stable)
        {
            var active = DeactivatePet(user);
            stable.IsActive = true;
            return active;
        }

        /// <summary>
        /// Deactivates all active pets for a user.
        /// </summary>
        /// <param name="user">The user to deactivate pets for.</param>
        /// <returns>A collection of pets that were deactivated.</returns>
        public IEnumerable<Stable> DeactivatePet(User user)
        {
            var active = GetActivePet(user).ToList();
            foreach (var activePet in active)
            {
                activePet.IsActive = false;
            }
            return active;
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
                if (stable.Experience > settings.PetExperienceToLevel && stable.Level < settings.PetLevelMax)
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
            PetRarity output = null;
            var petRarities = ConnectionManager.CurrentConnection.PetRarityData.Read().OrderBy(x => x.DropRate);
            var roll = Random.Next(1, 2000);
            foreach (var petRarity in petRarities)
            {
                if (roll <= petRarity.DropRate)
                {
                    output = petRarity;
                    break;
                }
            }
            return output;
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
            var availablePets = ConnectionManager.CurrentConnection.PetData.Read(x => x.Rarity.Equals(rarity));
            var stable = GetStableForUser(user).Select(x => x.Pet);
            var unownedPets = availablePets.Except(stable);
            if (unownedPets.Any())
            {
                var toGrant = unownedPets.ElementAt(Random.Next(0, unownedPets.Count()));
                var toAdd = new Stable()
                {
                    UserId = user.TwitchId,
                    Pet = toGrant,
                    IsSparkly = Random.Next(0, 100) == 0
                };
                ConnectionManager.CurrentConnection.Stables.Create(toAdd);
                PetFound?.Invoke(user, toAdd);
                return toAdd;
            }
            return null;
        }

        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
