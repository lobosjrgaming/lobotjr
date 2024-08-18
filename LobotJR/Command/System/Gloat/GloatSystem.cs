using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Fishing;
using LobotJR.Command.System.Pets;
using LobotJR.Command.System.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Gloat
{
    /// <summary>
    /// Runs the tournament logic for the fishing system.
    /// </summary>
    public class GloatSystem : ISystemProcess
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly PlayerSystem PlayerSystem;
        private readonly PetSystem PetSystem;
        private readonly LeaderboardSystem LeaderboardSystem;

        public GloatSystem(IConnectionManager connectionManager,
            SettingsManager settingsManager,
            PlayerSystem playerSystem,
            PetSystem petSystem,
            LeaderboardSystem leaderboardSystem)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            PlayerSystem = playerSystem;
            PetSystem = petSystem;
            LeaderboardSystem = leaderboardSystem;
        }

        /// <summary>
        /// Gets the cost to gloat about the player's level.
        /// </summary>
        /// <returns>The cost to gloat.</returns>
        public int GetLevelCost()
        {
            return SettingsManager.GetGameSettings().LevelGloatCost;
        }

        /// <summary>
        /// Checks if the user has enough coins to gloat about their level.
        /// </summary>
        /// <param name="user">The Twitch object for the user attempting to
        /// gloat.</param>
        /// <returns>True if the user has the coins to gloat.</returns>
        public bool CanGloatLevel(User user)
        {
            return PlayerSystem.GetPlayerByUser(user).Currency >= GetLevelCost();
        }

        /// <summary>
        /// Deducts the cost of gloating about level and returns the user's
        /// player object.
        /// </summary>
        /// <param name="user">The user to gloat about.</param>
        /// <returns>The player object for the user.</returns>
        public PlayerCharacter LevelGloat(User user)
        {
            var cost = GetLevelCost();
            var player = PlayerSystem.GetPlayerByUser(user);
            player.Currency -= cost;
            return player;
        }

        /// <summary>
        /// Gets the cost to gloat about a pet.
        /// </summary>
        /// <returns>The cost to gloat.</returns>
        public int GetPetCost()
        {
            return SettingsManager.GetGameSettings().PetGloatCost;
        }

        /// <summary>
        /// Checks if the user has enough coins to gloat about their current
        /// pet. This does not check to make sure they have an active pet, just
        /// if they can afford to gloat.
        /// </summary>
        /// <param name="user">The Twitch object for the user trying to gloat
        /// about their pet.</param>
        /// <returns>True if the user has the coins to gloat.</returns>
        public bool CanGloatPet(User user)
        {
            return PlayerSystem.GetPlayerByUser(user).Currency >= GetPetCost();
        }

        /// <summary>
        /// Deducts the cost of gloating about a pet and returns the current
        /// active pet. If no pet is currently active, no currency will be
        /// deducted.
        /// </summary>
        /// <param name="user">The user that owns the pet to gloat about.</param>
        /// <returns>The stable object for the user's active pet, or null if
        /// no pet is active.</returns>
        public Stable PetGloat(User user)
        {
            var cost = GetPetCost();
            var player = PlayerSystem.GetPlayerByUser(user);
            var pet = PetSystem.GetActivePet(user);
            if (pet != null)
            {
                player.Currency -= cost;
            }
            return pet;
        }

        /// <summary>
        /// Gets the cost to gloat about fishing records.
        /// </summary>
        /// <returns>The cost to gloat.</returns>
        public int GetFishCost()
        {
            return SettingsManager.GetGameSettings().FishingGloatCost;
        }

        /// <summary>
        /// Checks if the user has the coins to gloat about a fishing record.
        /// </summary>
        /// <param name="user">The Twitch object for the user attempting to
        /// gloat.</param>
        /// <returns>True if the user has the coins to gloat.</returns>
        public bool CanGloatFishing(User user)
        {
            return PlayerSystem.GetPlayerByUser(user).Currency >= GetFishCost();
        }

        /// <summary>
        /// Attempts to gloat about a specific fishing record.
        /// </summary>
        /// <param name="user">The Twitch object for the user attempting to
        /// gloat.</param>
        /// <param name="index">The id of the fish to gloat about.</param>
        /// <returns>The details of the record to gloat about.</returns>
        public Catch FishingGloat(User user, int index)
        {
            var cost = GetFishCost();
            var player = PlayerSystem.GetPlayerByUser(user);
            var fish = LeaderboardSystem.GetPersonalLeaderboard(user);
            if (index >= 0 && index < fish.Count())
            {
                player.Currency -= cost;
                return fish.ElementAt(index);
            }
            return null;
        }

        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
