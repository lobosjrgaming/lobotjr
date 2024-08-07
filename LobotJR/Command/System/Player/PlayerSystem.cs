using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Player
{
    /// <summary>
    /// System for managing player experience and currency.
    /// </summary>
    public class PlayerSystem : ISystem
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly int MaxLevel = 20;

        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly UserSystem UserSystem;

        private readonly List<string> PendingRespec = new List<string>();

        /// <summary>
        /// Event handler for events related to leveling up.
        /// </summary>
        /// <param name="user">The user that leveled up.</param>
        /// <param name="player">A player character object for the user.</param>
        public delegate void LevelUpEventHandler(User user, PlayerCharacter player);
        /// <summary>
        /// Event fired when a player levels up.
        /// </summary>
        public event LevelUpEventHandler LevelUp;
        /// <summary>
        /// Event handler for events related to periodic awards.
        /// </summary>
        /// <param name="experience">The amount of experience given.</param>
        /// <param name="currency">The amount of currency given.</param>
        /// <param name="multiplier">The subscriber multiplier applied.</param>
        public delegate void ExperienceAwardHandler(int experience, int currency, int multiplier);
        /// <summary>
        /// Event fired when periodic experience and currency are awarded.
        /// </summary>
        public event ExperienceAwardHandler ExperienceAwarded;
        /// <summary>
        /// The last time experience was awarded to viewers.
        /// </summary>
        public DateTime? LastAward { get; set; }
        /// <summary>
        /// Multiplier applied to experience and currency awards.
        /// </summary>
        public int CurrentMultiplier { get; set; } = 1;
        /// <summary>
        /// Wether or not the system is currently awarding experience and
        /// currency.
        /// </summary>
        public bool AwardsEnabled { get; set; } = false;
        /// <summary>
        /// The user that last turned on experience.
        /// </summary>
        public User AwardSetter { get; set; }

        public PlayerSystem(IConnectionManager connectionManager, SettingsManager settingsManager, UserSystem userSystem)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            UserSystem = userSystem;
        }

        private int ExperienceForLevel(int level)
        {
            return (int)(4 * Math.Pow(level, 3) + 50);
        }

        private int LevelFromExperience(int experience)
        {
            experience = Math.Max(experience, 81);
            var level = Math.Pow((experience - 50.0f) / 4.0f, (1.0f / 3.0f));
            return (int)Math.Floor(level);
        }

        /// <summary>
        /// Gives experience to a player. Raises the LevelUp event if the
        /// player levels up or prestiges as a result of this experience gain.
        /// </summary>
        /// <param name="user">The user object for the player.</param>
        /// <param name="player">The player character object.</param>
        /// <param name="experience">The amount of experience to add.</param>
        public void GainExperience(User user, PlayerCharacter player, int experience)
        {
            var oldLevel = LevelFromExperience(player.Experience);
            var newLevel = LevelFromExperience(player.Experience + experience);
            player.Experience += experience;
            if (newLevel != oldLevel)
            {
                if (newLevel > MaxLevel)
                {
                    player.Level = 3;
                    player.Experience = 200;
                    player.Prestige++;
                }
                else
                {
                    player.Level = newLevel;
                }
                LevelUp?.Invoke(user, player);
            }
        }

        /// <summary>
        /// Gets a player character object for a given user.
        /// </summary>
        /// <param name="user">The user to get the player character for.</param>
        /// <returns>A player character object tied to the user.</returns>
        public PlayerCharacter GetPlayerByUser(User user)
        {
            var player = ConnectionManager.CurrentConnection.PlayerCharacters.FirstOrDefault(x => x.UserId.Equals(user.TwitchId));
            if (player == null)
            {
                player = new PlayerCharacter()
                {
                    UserId = user.TwitchId,
                    CharacterClassId = ConnectionManager.CurrentConnection.CharacterClassData.First(x => !x.CanPlay).Id,
                };
                ConnectionManager.CurrentConnection.PlayerCharacters.Create(player);
                ConnectionManager.CurrentConnection.PlayerCharacters.Commit();
            }
            return player;
        }

        /// <summary>
        /// Returns the amount of experience needed to gain a level.
        /// </summary>
        /// <param name="experience">The current total experience.</param>
        /// <returns>The amount of experience needed to trigger a level up.</returns>
        public int GetExperienceToNextLevel(int experience)
        {
            return ExperienceForLevel(LevelFromExperience(experience) + 1) - experience;
        }

        /// <summary>
        /// Gets a collection of all playable classes. Non-playable classes
        /// such as the starting class will not be included.
        /// </summary>
        /// <returns>A collection of class data.</returns>
        public IEnumerable<CharacterClass> GetPlayableClasses()
        {
            return ConnectionManager.CurrentConnection.CharacterClassData.Read(x => x.CanPlay);
        }

        /// <summary>
        /// Gets metrics for the distribution of selected classes.
        /// </summary>
        /// <returns>A map of all playable classes and how many players have
        /// chosen each class.</returns>
        public IDictionary<CharacterClass, int> GetClassDistribution()
        {
            var classes = GetPlayableClasses();
            var allPlayers = ConnectionManager.CurrentConnection.PlayerCharacters.Read(x => x.CharacterClass.CanPlay);
            var output = new Dictionary<CharacterClass, int>();
            foreach (var characterClass in classes)
            {
                output.Add(characterClass, allPlayers.Count(x => x.CharacterClassId == characterClass.Id));
            }
            return output;
        }

        /// <summary>
        /// Gets the currency cost for a player to respec.
        /// </summary>
        /// <param name="level">The player's current level.</param>
        /// <returns>The currency required respec.</returns>
        public int GetRespecCost(int level)
        {
            var settings = SettingsManager.GetGameSettings();
            return Math.Max(settings.RespecCost, settings.RespecCost * (level - 4));
        }

        /// <summary>
        /// Gets the cost to pry on another player.
        /// </summary>
        /// <returns>The amount of currency required to pry.</returns>
        public int GetPryCost()
        {
            var settings = SettingsManager.GetGameSettings();
            return settings.PryCost;
        }

        /// <summary>
        /// Clears the class from a player. This should only be used for
        /// debugging purposes.
        /// </summary>
        /// <param name="player">The player to remove the class from.</param>
        public void ClearClass(PlayerCharacter player)
        {
            var baseClass = ConnectionManager.CurrentConnection.CharacterClassData.First(x => !x.CanPlay);
            SetClass(player, baseClass);
        }

        /// <summary>
        /// Sets the character class for a player.
        /// </summary>
        /// <param name="player">The player to change the class of.</param>
        /// <param name="characterClass">The class to change to.</param>
        public void SetClass(PlayerCharacter player, CharacterClass characterClass)
        {
            player.CharacterClass = characterClass;
        }

        /// <summary>
        /// Changes a player's class to a different one.
        /// </summary>
        /// <param name="player">The player to respec.</param>
        /// <param name="characterClass">The class to change to.</param>
        /// <param name="cost">The amount of currency to remove.</param>
        /// <returns>Whether or not the respec was successful.</returns>
        public bool Respec(PlayerCharacter player, CharacterClass characterClass, int cost)
        {
            if (player.Currency >= cost)
            {
                player.Currency -= cost;
                PendingRespec.Remove(player.UserId);
                SetClass(player, characterClass);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether a player has initiated a respec.
        /// </summary>
        /// <param name="player">The player to check for.</param>
        /// <returns>True if the player has initiated a respec.</returns>
        public bool IsFlaggedForRespec(PlayerCharacter player)
        {
            return PendingRespec.Contains(player.UserId);
        }

        /// <summary>
        /// Initiates a respec for a player.
        /// </summary>
        /// <param name="player">The player to flag for a respec.</param>
        /// <returns>True if the respec initiated successfully, false if the
        /// player was already flagged.</returns>
        public bool FlagForRespec(PlayerCharacter player)
        {
            if (!PendingRespec.Contains(player.UserId))
            {
                PendingRespec.Add(player.UserId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cancels a pending respec.
        /// </summary>
        /// <param name="player">The player to unflag for respec.</param>
        public void UnflagForRespec(PlayerCharacter player)
        {
            PendingRespec.Remove(player.UserId);
        }

        /// <summary>
        /// Checks whether a player is elligible to choose a class.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player can select a class.</returns>
        public bool CanSelectClass(PlayerCharacter player)
        {
            return IsFlaggedForRespec(player) || (!player.CharacterClass.CanPlay && player.Level >= 3);
        }

        /// <summary>
        /// Checks if the player can afford to pry.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player has enough currency to pry.</returns>
        public bool CanPry(PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            return player.Currency >= settings.PryCost;
        }

        /// <summary>
        /// Performs a pry, returning the data for another player.
        /// </summary>
        /// <param name="player">The player executing the pry.</param>
        /// <param name="target">The player to pry on.</param>
        /// <returns>The player character object for the target.</returns>
        public PlayerCharacter Pry(PlayerCharacter player, string target)
        {
            var targetUser = UserSystem.GetUserByName(target);
            if (targetUser != null)
            {
                var targetPlayer = GetPlayerByUser(targetUser);
                if (targetPlayer != null)
                {
                    var settings = SettingsManager.GetGameSettings();
                    player.Currency -= settings.PryCost;
                    return targetPlayer;
                }
            }
            return null;
        }

        /// <summary>
        /// Enables periodic experience and currency awards.
        /// </summary>
        /// <param name="user">The user triggering the enable.</param>
        public void EnableAwards(User user)
        {
            AwardsEnabled = true;
            AwardSetter = user;
        }


        /// <summary>
        /// Disables periodic experience and currency awards.
        /// </summary>
        public void DisableAwards()
        {
            AwardsEnabled = false;
            AwardSetter = null;
        }

        public Task Process()
        {
            var settings = SettingsManager.GetGameSettings();
            if (AwardsEnabled)
            {
                if (LastAward == null)
                {
                    LastAward = DateTime.Now;
                }
                else if (LastAward + TimeSpan.FromMinutes(settings.ExperienceFrequency) <= DateTime.Now)
                {
                    var chatters = UserSystem.Viewers.ToList();
                    var xpToAward = settings.ExperienceValue * CurrentMultiplier;
                    var coinsToAward = settings.CoinValue * CurrentMultiplier;
                    var subMultiplier = settings.SubRewardMultiplier;
                    Logger.Info("{coins} wolfcoins and {xp} experience awarded to {count} viewers.", coinsToAward, xpToAward, chatters.Count);
                    foreach (var chatter in chatters)
                    {
                        if (chatter.IsSub)
                        {
                            xpToAward *= subMultiplier;
                            coinsToAward *= subMultiplier;
                        }
                        var player = GetPlayerByUser(chatter);
                        GainExperience(chatter, player, xpToAward);
                        player.Currency += coinsToAward;
                        // I don't think we need to store mod/vip/sub flags in the database once everything is ported over to the new system
                        // The only time we would need access is when someone references them in a command, since any command they send comes with the flags in the chat message
                        //   This is not true of whispers, I'm an idiot.
                        //   We definitely need to store them, so we know their chat state when a whisper comes in.
                        // Maybe instead of doing batch user syncing operations, we can do it granularly each time someone sends a message
                        // If we only update on message received, then people who don't talk will end up stale
                        //  Could swap the batch sync to once a day or something?
                        //  If someone gets the wrong xp amount for at most a day if they change state and don't talk in chat, that's not the end of the world
                    }
                    ExperienceAwarded?.Invoke(xpToAward, coinsToAward, subMultiplier);
                }
            }
            else
            {
                LastAward = null;
            }
            return Task.CompletedTask;
        }
    }
}
