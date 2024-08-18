using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Equipment;
using LobotJR.Command.System.Pets;
using LobotJR.Command.System.Player;
using LobotJR.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Dungeons
{
    /// <summary>
    /// System for managing player parties and running dungeons.
    /// </summary>
    public class DungeonSystem : ISystemProcess
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly PartySystem PartySystem;
        private readonly GroupFinderSystem GroupFinderSystem;
        private readonly PlayerSystem PlayerSystem;
        private readonly EquipmentSystem EquipmentSystem;
        private readonly PetSystem PetSystem;

        private readonly Random Random = new Random();
        private readonly List<Party> DungeonGroups = new List<Party>();

        /// <summary>
        /// Gets the number of dungeon groups.
        /// </summary>
        public int PartyCount { get { return DungeonGroups.Count; } }

        /// <summary>
        /// Event handler for events related to progressing through a dungeon.
        /// </summary>
        /// <param name="party">The party to send the progress message to.</param>
        /// <param name="result">The progress message to send.</param>
        public delegate void DungeonProgressHandler(Party party, string result);
        /// <summary>
        /// Event fired when a player makes progress through a dungeon.
        /// </summary>
        public event DungeonProgressHandler DungeonProgress;

        /// <summary>
        /// Event handler for completing a dungeon.
        /// </summary>
        /// <param name="player">The player that completed a dungeon.</param>
        /// <param name="experience">The amount of experience earned.</param>
        /// <param name="currency">The amount of wolfcoins earned.</param>
        /// <param name="loot">The loot the player earned, if any.</param>
        /// <param name="groupFinderBonus">The player earned double rewards
        /// from the Daily Group Finder bonus.</param>
        public delegate void DungeonCompleteHandler(PlayerCharacter player, int experience, int currency, Item loot, bool groupFinderBonus);
        /// <summary>
        /// Event fired when a player completes a dungeon.
        /// </summary>
        public event DungeonCompleteHandler DungeonComplete;

        /// <summary>
        /// Event handler for player death.
        /// </summary>
        /// <param name="player">The player that died.</param>
        /// <param name="experienceLost">The amount of experience the player
        /// lost as a result of dying.</param>
        /// <param name="currencyLost">The amount of currency the player lost
        /// as a result of dying.</param>
        public delegate void PlayerDeathHandler(PlayerCharacter player, int experienceLost, int currencyLost);
        /// <summary>
        /// Event fired when a player dies.
        /// </summary>
        public event PlayerDeathHandler PlayerDeath;

        /// <summary>
        /// Event handler for failing a dungeon.
        /// </summary>
        /// <param name="party">The party to send the notice to.</param>
        /// <param name="deceased">The players that died as a result of the
        /// failure.</param>
        public delegate void DungeonFailureHandler(Party party, IEnumerable<PlayerCharacter> deceased);
        /// <summary>
        /// Event fired when a party fails a dungeon.
        /// </summary>
        public event DungeonFailureHandler DungeonFailure;

        public DungeonSystem(IConnectionManager connectionManager, SettingsManager settingsManager, PartySystem partySystem, GroupFinderSystem groupFinderSystem, PlayerSystem playerSystem, EquipmentSystem equipmentSystem, PetSystem petSystem)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            PartySystem = partySystem;
            GroupFinderSystem = groupFinderSystem;
            PlayerSystem = playerSystem;
            EquipmentSystem = equipmentSystem;
            PetSystem = petSystem;
        }

        /// <summary>
        /// Gets the formatted display name of a dungeon and mode.
        /// </summary>
        /// <param name="run">The dungeon run, or paired dungeon plus mode.</param>
        /// <returns>The formatted display name.</returns>
        public string GetDungeonName(DungeonRun run)
        {
            var modeName = run.Mode.IsDefault ? "" : $" [{run.Mode.Name}]";
            return $"{run.Dungeon.Name}{modeName}";
        }

        /// <summary>
        /// Parses a dungeon id string into a paired dungeon and mode object.
        /// </summary>
        /// <param name="dungeonId">The dungeon id string. This should be in
        /// the format of "{id}{modeFlag}". For example "1h" would get the heroic
        /// mode of the dugneon with id 1. If no flag is provided, for example
        /// "1", then the default dungeon mode will be used.</param>
        /// <returns>A dungeon run object containing the dungeon and mode.</returns>
        public DungeonRun ParseDungeonId(string dungeonId)
        {
            var modes = ConnectionManager.CurrentConnection.DungeonModeData.Read().ToArray();
            var defaultMode = modes.FirstOrDefault(x => x.IsDefault);
            var selectedMode = modes.FirstOrDefault(x => dungeonId.EndsWith(x.Flag, StringComparison.OrdinalIgnoreCase));
            var mode = selectedMode ?? defaultMode;
            var id = dungeonId;
            if (selectedMode != null)
            {
                id = dungeonId.Substring(0, dungeonId.Length - selectedMode.Flag.Length);
            }
            if (int.TryParse(id, out var idNumber) && mode != null)
            {
                var dungeon = ConnectionManager.CurrentConnection.DungeonData.FirstOrDefault(x => x.Id == idNumber);
                return new DungeonRun(dungeon, mode);
            }
            return null;
        }

        /// <summary>
        /// Gets all available dungeons in every mode they can be run in.
        /// </summary>
        /// <returns>A collection of dungeon run objects.</returns>
        public IEnumerable<DungeonRun> GetAllDungeons()
        {
            var allDungeons = ConnectionManager.CurrentConnection.DungeonData.Read();
            var allRanges = ConnectionManager.CurrentConnection.LevelRangeData.Read();
            return allRanges.Select(x => new DungeonRun(allDungeons.FirstOrDefault(y => y.Id.Equals(x.DungeonId)), x.Mode));
        }

        /// <summary>
        /// Gets all dungeons a player is eligible to run based on their level.
        /// </summary>
        /// <param name="player">The player to check against.</param>
        /// <returns>A collection of dungeon run objects the player can run
        /// based on their level.</returns>
        public IEnumerable<DungeonRun> GetEligibleDungeons(PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            return GetEligibleDungeons(player, settings);
        }

        /// <summary>
        /// Gets all dungeons a player is eligible to run based on their level.
        /// </summary>
        /// <param name="player">The player to check against.</param>
        /// <param name="settings">A game settings object. Provide when looping
        /// to prevent extra calls to fetch settings.</param>
        /// <returns>A collection of dungeon run objects the player can run
        /// based on their level.</returns>
        public IEnumerable<DungeonRun> GetEligibleDungeons(PlayerCharacter player, GameSettings settings)
        {
            if (settings.DungeonLevelRestrictions)
            {
                var allDungeons = ConnectionManager.CurrentConnection.DungeonData.Read();
                var allRanges = ConnectionManager.CurrentConnection.LevelRangeData.Read(x => player.Level >= x.Minimum && player.Level <= x.Maximum);
                return allRanges.Select(x => new DungeonRun(allDungeons.FirstOrDefault(y => y.Id.Equals(x.DungeonId)), x.Mode));
            }
            return GetAllDungeons();
        }

        /// <summary>
        /// Gets the list of dungeons that all members of the party are
        /// eligible for.
        /// </summary>
        /// <param name="party">The party to get dungeons for.</param>
        /// <returns>A collection of dungeon runs that everyone in the party is
        /// eligible to run.</returns>
        public IEnumerable<DungeonRun> GetEligibleDungeons(Party party)
        {
            var settings = SettingsManager.GetGameSettings();
            if (settings.DungeonLevelRestrictions)
            {
                var dungeonLists = party.Members.Select(x => GetEligibleDungeons(x, settings)).ToList();
                var dungeons = dungeonLists.FirstOrDefault();
                foreach (var list in dungeonLists.Skip(1))
                {
                    dungeons = dungeons.Intersect(list);
                }
                return dungeons;
            }
            return GetAllDungeons();
        }

        /// <summary>
        /// Gets the cost for a player to run a dungeon. This method takes a
        /// settings object so that it can be reused to reduce database calls.
        /// </summary>
        /// <param name="player">The player to get the cost for.</param>
        /// <param name="settings">The game settings object, used to get the
        /// dungeon cost.</param>
        /// <returns>The cost for the player to run a dungeon.</returns>
        public int GetDungeonCost(PlayerCharacter player, GameSettings settings)
        {
            return settings.DungeonBaseCost + (player.Level - PlayerSystem.MinLevel) * settings.DungeonLevelCost;
        }

        /// <summary>
        /// Gets the cost for a player to run a dungeon.
        /// </summary>
        /// <param name="player">The player to get the cost for.</param>
        /// <returns>The cost for the player to run a dungeon.</returns>
        public int GetDungeonCost(PlayerCharacter player)
        {
            return GetDungeonCost(player, SettingsManager.GetGameSettings());
        }

        /// <summary>
        /// Checks if a party is in a state where they can start a dungeon.
        /// </summary>
        /// <param name="party">The party to check.</param>
        /// <returns>True if the party is in the right state to start a
        /// dungeon.</returns>
        public bool CanStartDungeon(Party party)
        {
            return party.State == PartyState.Full;
        }

        /// <summary>
        /// Attempts to start a dungeon for a party.
        /// </summary>
        /// <param name="party">The party to start in a dungeon.</param>
        /// <param name="run">The dungeon run to start.</param>
        /// <param name="playersWithoutCoins">Provides the collection of
        /// players that don't have enough money to start a dungeon.</param>
        /// <returns>True if the party was able to start the dungeon.</returns>
        public bool TryStartDungeon(Party party, DungeonRun run, out IEnumerable<PlayerCharacter> playersWithoutCoins)
        {
            if (run != null)
            {
                if (CanStartDungeon(party))
                {
                    var settings = SettingsManager.GetGameSettings();
                    var costs = party.Members.ToDictionary(x => x, x => GetDungeonCost(x, settings));
                    playersWithoutCoins = costs.Where(x => x.Key.Currency < x.Value).Select(x => x.Key);
                    if (!playersWithoutCoins.Any())
                    {
                        foreach (var pair in costs)
                        {
                            pair.Key.Currency -= pair.Value;
                        }
                        party.Run = run;
                        party.State = PartyState.Started;
                        return true;
                    }
                }
            }
            playersWithoutCoins = Array.Empty<PlayerCharacter>();
            return false;
        }

        private bool CanProcessUpdate(Party party, TimeSpan stepTime)
        {
            return party.LastUpdate == null || party.LastUpdate + stepTime <= DateTime.Now;
        }

        private Dictionary<CharacterClass, int> GetClassModifiers(Party party)
        {
            return party.Members.GroupBy(x => x.CharacterClass).ToDictionary(x => x.Key, x => 1 / x.Count());
        }

        private float PlayerSuccessChance(PlayerCharacter player)
        {
            return player.CharacterClass.SuccessChance + EquipmentSystem.GetEquippedGear(player).Sum(x => x.SuccessChance);
        }

        private float PartySuccessChance(Party party)
        {
            var classMultipliers = GetClassModifiers(party);
            return party.Members.Sum(x => PlayerSuccessChance(x) * classMultipliers[x.CharacterClass]);
        }

        private float PlayerDeathChance(PlayerCharacter player)
        {
            return EquipmentSystem.GetEquippedGear(player).Sum(x => x.PreventDeathBonus);
        }

        private float PartyDeathChance(Party party)
        {
            var classMultipliers = GetClassModifiers(party);
            return party.Members.Sum(x => x.CharacterClass.PreventDeathBonus * classMultipliers[x.CharacterClass]);
        }

        private float PlayerLootChance(PlayerCharacter player)
        {
            return EquipmentSystem.GetEquippedGear(player).Sum(x => x.ItemFind);
        }

        private bool CalculateCheck(float chance)
        {
            return Random.NextDouble() < chance;
        }

        private int CalculateExperienceReward(PlayerCharacter player, bool includeBonuses)
        {
            var experienceMultiplier = 1f;
            var variance = 1f;
            if (includeBonuses)
            {
                experienceMultiplier += player.CharacterClass.XpBonus + EquipmentSystem.GetEquippedGear(player).Sum(x => x.XpBonus);
                var settings = SettingsManager.GetGameSettings();
                if (CalculateCheck(settings.DungeonCritChance))
                {
                    variance += settings.DungeonCritBonus;
                }
            }
            return (int)Math.Round((11 + (player.Level - 2) * 3) * experienceMultiplier * variance);
        }

        private int CalculateCoinReward(PlayerCharacter player, bool includeBonuses)
        {
            var coinMultiplier = 1f;
            var levelScale = 1 + 0.05f * player.Level;
            if (includeBonuses)
            {
                coinMultiplier += player.CharacterClass.CoinBonus + EquipmentSystem.GetEquippedGear(player).Sum(x => x.CoinBonus);
            }
            return (int)Math.Round(50 * coinMultiplier * levelScale);
        }

        private void ProgressDungeon(Party party)
        {
            string message;
            if (party.CurrentEncounter == 0)
            {
                message = party.Run.Dungeon.Introduction;
                party.CurrentEncounter++;
                party.StepState = StepState.Setup;
            }
            else if (party.StepState == StepState.Setup)
            {
                message = party.Run.Dungeon.Encounters.ElementAt(party.CurrentEncounter - 1).SetupText;
                party.StepState = StepState.Resolving;
            }
            else if (party.StepState == StepState.Resolving)
            {
                var encounter = party.Run.Dungeon.Encounters.ElementAt(party.CurrentEncounter - 1);
                var difficulty = encounter.Levels.FirstOrDefault(x => x.Mode.Equals(party.Run.Mode)).Difficulty;
                var successChance = PartySuccessChance(party);
                if (CalculateCheck(difficulty + successChance))
                {
                    party.StepState = StepState.Complete;
                    message = $"Your party successfully defeated the {encounter.Enemy}!";
                }
                else
                {
                    message = party.Run.Dungeon.FailureText;
                    party.CurrentEncounter = 0;
                    party.StepState = StepState.Setup;
                    party.State = PartyState.Failed;
                }
            }
            else //party.StepState == StepState.Complete
            {
                message = party.Run.Dungeon.Encounters.ElementAt(party.CurrentEncounter - 1).CompleteText;
                party.CurrentEncounter++;
                if (party.CurrentEncounter > party.Run.Dungeon.Encounters.Count)
                {
                    party.State = PartyState.Complete;
                }
            }
            party.LastUpdate = DateTime.Now;
            DungeonProgress?.Invoke(party, message);
        }

        private void HandleCompletion(Party party)
        {
            foreach (var member in party.Members)
            {
                var user = PlayerSystem.GetUserByPlayer(member);
                var xp = CalculateExperienceReward(member, true);
                var coins = CalculateCoinReward(member, true);
                var gfBonus = party.IsQueueGroup && GroupFinderSystem.GetLockoutTime(member).TotalMilliseconds <= 0;
                if (gfBonus)
                {
                    xp *= 2;
                    coins *= 2;
                }
                PlayerSystem.GainExperience(member, xp);
                member.Currency += coins;

                var loot = ConnectionManager.CurrentConnection.LootData.Read(x => x.Dungeon.Equals(party.Run.Dungeon) && x.Mode.Equals(party.Run.Mode));
                var lootFilter = ConnectionManager.CurrentConnection.EquippableData.Read(x => x.CharacterClass.Equals(member.CharacterClass));
                var currentLoot = EquipmentSystem.GetInventoryByPlayer(member);
                var possibleDrops = loot.Where(x => !currentLoot.Any(y => y.Item.Equals(x)) && lootFilter.Any(y => y.ItemType.Equals(x.Item.Type)));
                var lootChance = PlayerLootChance(member);
                var drops = possibleDrops.Where(x => CalculateCheck((float)x.DropChance + lootChance));
                var earnedDrop = drops.FirstOrDefault()?.Item;
                if (earnedDrop != null)
                {
                    EquipmentSystem.AddInventoryRecord(user, earnedDrop);
                }
                DungeonComplete?.Invoke(member, xp, coins, earnedDrop, gfBonus);

                var activePet = PetSystem.GetActivePet(member);
                if (activePet != null)
                {
                    PetSystem.AddHunger(member, activePet);
                }
                var rarity = PetSystem.RollForRarity();
                if (rarity != null)
                {
                    PetSystem.GrantPet(user, rarity);
                }

                party.Reset();
                if (party.IsQueueGroup)
                {
                    PartySystem.DisbandParty(party);
                }
            }
        }

        private void HandleFailure(Party party, GameSettings settings)
        {
            var deathChance = settings.DungeonDeathChance - PartyDeathChance(party);
            var dead = party.Members.Where(x => CalculateCheck(deathChance - PlayerDeathChance(x))).ToList();
            foreach (var member in dead)
            {
                var xp = CalculateExperienceReward(member, false);
                var coins = CalculateCoinReward(member, false);
                member.Experience -= xp;
                member.Currency -= coins;
                PlayerDeath?.Invoke(member, xp, coins);
            }
            DungeonFailure?.Invoke(party, dead);
        }

        public Task Process()
        {
            var settings = SettingsManager.GetGameSettings();
            var stepTime = TimeSpan.FromMilliseconds(settings.DungeonStepTime);
            var toUpdate = DungeonGroups.Where(x => x.State == PartyState.Started && CanProcessUpdate(x, stepTime));
            foreach (var party in toUpdate)
            {
                ProgressDungeon(party);
            }
            var toComplete = DungeonGroups.Where(x => x.State == PartyState.Complete && CanProcessUpdate(x, stepTime));
            foreach (var party in toComplete)
            {
                HandleCompletion(party);
            }
            var toFail = DungeonGroups.Where(x => x.State == PartyState.Failed && CanProcessUpdate(x, stepTime));
            foreach (var party in toFail)
            {
                HandleFailure(party, settings);
            }
            return Task.CompletedTask;
        }
    }
}
