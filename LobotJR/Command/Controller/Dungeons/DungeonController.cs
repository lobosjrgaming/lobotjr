using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.Controller.Dungeons
{
    /// <summary>
    /// Controller for managing player parties and running dungeons.
    /// </summary>
    public class DungeonController : IProcessor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly PartyController PartyController;
        private readonly GroupFinderController GroupFinderController;
        private readonly PlayerController PlayerController;
        private readonly EquipmentController EquipmentController;
        private readonly PetController PetController;

        private readonly Random Random = new Random();

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
        /// <param name="wasQueueGroup">Group was formed by the dungeon finder
        /// queue.</param>
        /// <param name="groupFinderBonus">The player earned double rewards
        /// from the Daily Group Finder bonus.</param>
        /// <param name="critBonus">The player earned the critical experience
        /// bonus.</param>
        public delegate void DungeonCompleteHandler(PlayerCharacter player, int experience, int currency, Item loot, bool wasQueueGroup, bool groupFinderBonus, bool critBonus);
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

        public DungeonController(IConnectionManager connectionManager, SettingsManager settingsManager, PartyController partyController, GroupFinderController groupFinderController, PlayerController playerController, EquipmentController equipmentController, PetController petController)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            PartyController = partyController;
            GroupFinderController = groupFinderController;
            PlayerController = playerController;
            EquipmentController = equipmentController;
            PetController = petController;
        }

        public Dungeon GetDungeonById(int dungeonId)
        {
            return ConnectionManager.CurrentConnection.DungeonData.ReadById(dungeonId);
        }

        public LevelRange GetDungeonLevel(int dungeonId, int modeId)
        {
            return ConnectionManager.CurrentConnection.LevelRangeData.Read(x => x.DungeonId == dungeonId && x.ModeId == modeId).FirstOrDefault();
        }

        /// <summary>
        /// Gets the formatted display name of a dungeon and mode.
        /// </summary>
        /// <param name="run">The dungeon run, or paired dungeon plus mode.</param>
        /// <returns>The formatted display name.</returns>
        public string GetDungeonName(Dungeon dungeon, DungeonMode mode)
        {
            var modeName = mode.IsDefault ? "" : $" [{mode.Name}]";
            return $"{dungeon.Name}{modeName}";
        }

        /// <summary>
        /// Gets the formatted display name of a dungeon and mode.
        /// </summary>
        /// <param name="run">The dungeon run, or paired dungeon plus mode.</param>
        /// <returns>The formatted display name.</returns>
        public string GetDungeonName(int dungeonId, int modeId)
        {
            var dungeon = ConnectionManager.CurrentConnection.DungeonData.ReadById(dungeonId);
            var mode = ConnectionManager.CurrentConnection.DungeonModeData.ReadById(modeId);
            var modeName = mode.IsDefault ? "" : $" [{mode.Name}]";
            return $"{dungeon.Name}{modeName}";
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
                var dungeon = ConnectionManager.CurrentConnection.DungeonData.ReadById(idNumber);
                if (dungeon != null)
                {
                    return new DungeonRun(dungeon, mode);
                }
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
                var dungeonLists = party.Members.Select(x => GetEligibleDungeons(PlayerController.GetPlayerByUserId(x), settings));
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
            return settings.DungeonBaseCost + (player.Level - PlayerController.MinLevel) * settings.DungeonLevelCost;
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
        /// <param name="dungeonId">The id of the dungeon to start.</param>
        /// <param name="modeId">The id of the mode to start the dungeon in.</param>
        /// <param name="playersWithoutCoins">Provides the collection of
        /// players that don't have enough money to start a dungeon.</param>
        /// <returns>True if the party was able to start the dungeon.</returns>
        public bool TryStartDungeon(Party party, int dungeonId, int modeId, out IEnumerable<PlayerCharacter> playersWithoutCoins)
        {
            playersWithoutCoins = Array.Empty<PlayerCharacter>();
            if (dungeonId > 0)
            {
                if (CanStartDungeon(party))
                {
                    var settings = SettingsManager.GetGameSettings();
                    var members = party.Members.Select(x => PlayerController.GetPlayerByUserId(x));
                    var costs = members.ToDictionary(x => x, x => GetDungeonCost(x, settings));
                    playersWithoutCoins = costs.Where(x => x.Key.Currency < x.Value).Select(x => x.Key);
                    if (!playersWithoutCoins.Any())
                    {
                        foreach (var pair in costs)
                        {
                            pair.Key.Currency -= pair.Value;
                        }
                        party.DungeonId = dungeonId;
                        party.ModeId = modeId;
                        party.State = PartyState.Started;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanProcessUpdate(Party party, TimeSpan stepTime)
        {
            return party.LastUpdate == null || party.LastUpdate + stepTime <= DateTime.Now;
        }

        private bool CalculateCheck(float chance)
        {
            var value = Random.NextDouble();
            return value < chance;
        }

        private Dictionary<CharacterClass, float> GetClassModifiers(IEnumerable<PlayerCharacter> players)
        {
            return players.GroupBy(x => x.CharacterClass).ToDictionary(x => x.Key, x => 1f / x.Count());
        }

        private float PlayerSuccessChance(PlayerCharacter player)
        {
            var multiplier = Math.Max(0.75f, (float)player.Level / PlayerController.MaxLevel);
            return (player.CharacterClass.SuccessChance + EquipmentController.GetEquippedGear(player).Sum(x => x.SuccessChance)) * multiplier;
        }

        private float PartySuccessChance(IEnumerable<PlayerCharacter> players, Dictionary<CharacterClass, float> classMultipliers)
        {
            return players.Sum(x => PlayerSuccessChance(x) * classMultipliers[x.CharacterClass]);
        }

        private float PlayerDeathChance(PlayerCharacter player)
        {
            return EquipmentController.GetEquippedGear(player).Sum(x => x.PreventDeathBonus);
        }

        private float PartyDeathChance(IEnumerable<PlayerCharacter> players, Dictionary<CharacterClass, float> classMultipliers)
        {
            return players.Sum(x => x.CharacterClass.PreventDeathBonus * classMultipliers[x.CharacterClass]);
        }

        private float PartyLootChance(IEnumerable<PlayerCharacter> players, Dictionary<CharacterClass, float> classMultipliers)
        {
            return players.Sum(x => (x.CharacterClass.ItemFind + EquipmentController.GetEquippedGear(x).Sum(y => y.ItemFind)) * classMultipliers[x.CharacterClass]);
        }

        private float CalculatePlayerExperience(PlayerCharacter player)
        {
            return 11 + (player.Level - 2) * 3;
        }

        private float PartyExperienceBonus(IEnumerable<PlayerCharacter> players, Dictionary<CharacterClass, float> classMultipliers)
        {
            return players.Sum(x => (x.CharacterClass.XpBonus + EquipmentController.GetEquippedGear(x).Sum(y => y.XpBonus)) * classMultipliers[x.CharacterClass]);
        }

        private float CalculatePlayerCoins(PlayerCharacter player)
        {
            return 50 * (1 + 0.05f * player.Level);
        }

        private float PartyCoinBonus(IEnumerable<PlayerCharacter> players, Dictionary<CharacterClass, float> classMultipliers)
        {
            return players.Sum(x => (x.CharacterClass.CoinBonus + EquipmentController.GetEquippedGear(x).Sum(y => y.CoinBonus)) * classMultipliers[x.CharacterClass]);
        }

        private void ProgressDungeon(Party party)
        {
            var members = party.Members.Select(x => PlayerController.GetPlayerByUserId(x));
            var classMultipliers = GetClassModifiers(members);
            var dungeon = GetDungeonById(party.DungeonId);
            var name = GetDungeonName(party.DungeonId, party.ModeId);
            Logger.Debug("Processing dungeon step for party {description} in state {state} for {dungeon} on step {step}", string.Join(", ", party.Members), party.State, name, party.CurrentEncounter);
            string message;
            if (party.CurrentEncounter == 0)
            {
                Logger.Debug("  Sending intro text");
                message = dungeon.Introduction;
                party.CurrentEncounter++;
                party.StepState = StepState.Setup;
            }
            else if (party.StepState == StepState.Setup)
            {
                Logger.Debug("  Sending encounter setup");
                message = dungeon.Encounters.ElementAt(party.CurrentEncounter - 1).SetupText;
                party.StepState = StepState.Resolving;
            }
            else if (party.StepState == StepState.Resolving)
            {
                Logger.Debug("  Resolving encounter...");
                var encounter = dungeon.Encounters.ElementAt(party.CurrentEncounter - 1);
                var difficulty = encounter.Levels.FirstOrDefault(x => x.Mode.Id.Equals(party.ModeId)).Difficulty;
                var successChance = PartySuccessChance(members, classMultipliers);
                Logger.Debug("  Encounter difficulty: {difficulty}", difficulty);
                Logger.Debug("  successChance: {chance}", successChance);

                if (CalculateCheck(difficulty + successChance))
                {
                    Logger.Debug("  Success!");
                    party.StepState = StepState.Complete;
                    message = $"Your party successfully defeated the {encounter.Enemy}!";
                }
                else
                {
                    Logger.Debug("  Failure!");
                    message = dungeon.FailureText;
                    party.StepState = StepState.Setup;
                    party.State = PartyState.Failed;
                }
            }
            else //party.StepState == StepState.Complete
            {
                Logger.Debug("  Sending encounter success");
                message = dungeon.Encounters.ElementAt(party.CurrentEncounter - 1).CompleteText;
                party.CurrentEncounter++;
                party.StepState = StepState.Setup;
                if (party.CurrentEncounter > dungeon.Encounters.Count())
                {
                    party.State = PartyState.Complete;
                }
            }
            party.LastUpdate = DateTime.Now;
            DungeonProgress?.Invoke(party, message);
        }

        private DungeonHistory CreateDungeonHistory(Party party, bool success)
        {
            return new DungeonHistory()
            {
                Date = DateTime.Now,
                DungeonId = party.DungeonId,
                ModeId = party.ModeId,
                IsQueueGroup = party.IsQueueGroup,
                StepsComplete = party.CurrentEncounter - 1,
                Success = success
            };
        }

        private void AddParticipant(DungeonHistory history, Party party, string userId, int xp, int coins, Item itemDrop, Pet petDrop)
        {
            party.QueueTimes.TryGetValue(userId, out var waitTime);
            history.Participants.Add(new DungeonParticipant()
            {
                UserId = userId,
                ExperienceEarned = xp,
                CurrencyEarned = coins,
                ItemDrop = itemDrop,
                PetDrop = petDrop,
                WaitTime = waitTime
            });
        }

        private void ResetParty(Party party)
        {
            party.Reset();
            if (party.IsQueueGroup)
            {
                party.State = PartyState.Disbanded;
            }
        }

        private void HandleCompletion(Party party)
        {
            var members = party.Members.Select(x => PlayerController.GetPlayerByUserId(x));
            var dungeon = GetDungeonById(party.DungeonId);
            var name = GetDungeonName(party.DungeonId, party.ModeId);
            Logger.Debug("Processing dungeon complete for party {description} in state {state} for {dungeon}", string.Join(", ", party.Members), party.State, name);
            var history = CreateDungeonHistory(party, true);
            var settings = SettingsManager.GetGameSettings();
            var classMultipliers = GetClassModifiers(members);
            var lootChance = PartyLootChance(members, classMultipliers);
            var xpBonus = PartyExperienceBonus(members, classMultipliers);
            var coinBonus = PartyCoinBonus(members, classMultipliers);
            foreach (var member in members)
            {
                var user = PlayerController.GetUserByPlayer(member);
                var xpRaw = CalculatePlayerExperience(member);
                var crit = CalculateCheck(settings.DungeonCritChance);
                if (crit)
                {
                    xpRaw *= 1f + settings.DungeonCritBonus;
                }
                var coinsRaw = CalculatePlayerCoins(member);
                var gfBonus = party.IsQueueGroup && GroupFinderController.GetLockoutTime(member).TotalMilliseconds <= 0;
                if (gfBonus)
                {
                    xpRaw *= 2;
                    coinsRaw *= 2;
                    GroupFinderController.SetLockout(member);
                }
                var xp = (int)Math.Round(xpRaw * (1f + xpBonus));
                var coins = (int)Math.Round(coinsRaw * (1f + coinBonus));
                PlayerController.GainExperience(member, xp);
                member.Currency += coins;

                var loot = ConnectionManager.CurrentConnection.LootData.Read(x => x.Dungeon.Id == party.DungeonId && x.Mode.Id == party.ModeId);
                var lootFilter = ConnectionManager.CurrentConnection.EquippableData.Read(x => x.CharacterClass.Id.Equals(member.CharacterClass.Id));
                var currentLoot = EquipmentController.GetInventoryByPlayer(member);

                var filterTypes = lootFilter.Select(x => x.ItemType).ToList();
                var filteredLoot = loot.Where(x => filterTypes.Contains(x.Item.Type));
                var currentItems = currentLoot.Select(x => x.Item);
                var possibleDrops = filteredLoot.Where(x => !currentItems.Contains(x.Item));

                var drops = possibleDrops.Where(x => CalculateCheck(lootChance - (float)x.DropChance));
                var earnedDrop = drops.FirstOrDefault()?.Item;
                if (earnedDrop != null)
                {
                    EquipmentController.AddInventoryRecord(user, earnedDrop);
                }
                DungeonComplete?.Invoke(member, xp, coins, earnedDrop, party.IsQueueGroup, gfBonus, crit);

                var rarity = PetController.RollForRarity();
                Stable earnedPet = null;
                if (rarity != null)
                {
                    PetController.GrantPet(user, rarity);
                }

                var activePet = PetController.GetActivePet(member);
                if (activePet != null)
                {
                    PetController.AddHunger(member, activePet);
                }

                AddParticipant(history, party, member.UserId, xp, coins, earnedDrop, earnedPet?.Pet);

            }
            ConnectionManager.CurrentConnection.DungeonHistories.Create(history);
            ResetParty(party);
        }

        private void HandleFailure(Party party, GameSettings settings)
        {
            var members = party.Members.Select(x => PlayerController.GetPlayerByUserId(x));
            var dungeon = GetDungeonById(party.DungeonId);
            var name = GetDungeonName(party.DungeonId, party.ModeId);
            Logger.Debug("Processing dungeon failure for party {description} in state {state} for {dungeon}", string.Join(", ", party.Members), party.State, name);
            var history = CreateDungeonHistory(party, false);
            var classMultipliers = GetClassModifiers(members);
            var deathChance = settings.DungeonDeathChance - PartyDeathChance(members, classMultipliers);
            var dead = members.Where(x => CalculateCheck(deathChance - PlayerDeathChance(x))).ToList();
            foreach (var member in dead)
            {
                var xp = (int)Math.Round(CalculatePlayerExperience(member));
                var coins = (int)Math.Round(CalculatePlayerCoins(member));
                var toNewLevel = PlayerController.GetExperienceToNextLevel(member.Experience - xp);
                if (toNewLevel <= xp)
                {
                    xp -= toNewLevel;
                }
                member.Experience -= xp;
                member.Currency -= coins;
                PlayerDeath?.Invoke(member, xp, coins);
                AddParticipant(history, party, member.UserId, -xp, -coins, null, null);
            }
            foreach (var member in members.Except(dead))
            {
                AddParticipant(history, party, member.UserId, 0, 0, null, null);
            }
            DungeonFailure?.Invoke(party, dead);
            ConnectionManager.CurrentConnection.DungeonHistories.Create(history);
            ResetParty(party);
        }

        public Task Process()
        {
            var settings = SettingsManager.GetGameSettings();
            var stepTime = TimeSpan.FromMilliseconds(settings.DungeonStepTime);
            var groups = PartyController.GetAllGroups();
            var toUpdate = groups.Where(x => x.State == PartyState.Started && CanProcessUpdate(x, stepTime));
            foreach (var party in toUpdate)
            {
                ProgressDungeon(party);
            }
            var toComplete = groups.Where(x => x.State == PartyState.Complete && CanProcessUpdate(x, stepTime));
            foreach (var party in toComplete)
            {
                HandleCompletion(party);
            }
            var toFail = groups.Where(x => x.State == PartyState.Failed && CanProcessUpdate(x, stepTime));
            foreach (var party in toFail)
            {
                HandleFailure(party, settings);
            }
            var toRemove = groups.Where(x => x.State == PartyState.Disbanded).ToList();
            foreach (var party in toRemove)
            {
                PartyController.DisbandParty(party);
            }
            return Task.CompletedTask;
        }
    }
}