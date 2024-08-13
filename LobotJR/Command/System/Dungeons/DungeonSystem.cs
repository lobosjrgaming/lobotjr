using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Player;
using LobotJR.Command.System.Equipment;
using LobotJR.Command.System.Player;
using LobotJR.Command.System.Twitch;
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
    public class DungeonSystem : ISystem
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private readonly UserSystem UserSystem;
        private readonly EquipmentSystem EquipmentSystem;

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
        public delegate void DungeonCompleteHandler(PlayerCharacter player, int experience, int currency);
        /// <summary>
        /// Event fired when a player completes a dungeon.
        /// </summary>
        public event DungeonCompleteHandler DungeonComplete;

        /// <summary>
        /// Event handler for player death.
        /// </summary>
        /// <param name="party">The party to send the notice to.</param>
        /// <param name="deceased">The players that died as a result of the
        /// failure.</param>
        public delegate void DungeonFailureHandler(Party party, IEnumerable<PlayerCharacter> deceased);
        /// <summary>
        /// Event fired when a player dies.
        /// </summary>
        public event DungeonFailureHandler DungeonFailure;

        public DungeonSystem(IConnectionManager connectionManager, SettingsManager settingsManager, UserSystem userSystem, EquipmentSystem equipmentSystem)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            UserSystem = userSystem;
            EquipmentSystem = equipmentSystem;
        }

        public string GetDungeonName(DungeonRun run)
        {
            var modeName = run.Mode.IsDefault ? "" : $" [{run.Mode.Name}]";
            return $"{run.Dungeon.Name}{modeName}";
        }

        public DungeonRun ParseDungeonId(string dungeonId)
        {
            var modes = ConnectionManager.CurrentConnection.DungeonModeData.Read().ToArray();
            var defaultMode = modes.FirstOrDefault(x => x.IsDefault);
            var selectedMode = modes.FirstOrDefault(x => dungeonId.EndsWith(x.Flag));
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

        public IEnumerable<DungeonRun> GetAllDungeons()
        {
            var allDungeons = ConnectionManager.CurrentConnection.DungeonData.Read();
            var allRanges = ConnectionManager.CurrentConnection.LevelRangeData.Read();
            return allRanges.Select(x => new DungeonRun(allDungeons.FirstOrDefault(y => y.Id.Equals(x.DungeonId)), x.Mode));
        }

        public IEnumerable<DungeonRun> GetEligibleDungeons(PlayerCharacter player)
        {
            var allDungeons = ConnectionManager.CurrentConnection.DungeonData.Read();
            var allRanges = ConnectionManager.CurrentConnection.LevelRangeData.Read(x => player.Level >= x.Minimum && player.Level <= x.Maximum);
            return allRanges.Select(x => new DungeonRun(allDungeons.FirstOrDefault(y => y.Id.Equals(x.DungeonId)), x.Mode));
        }

        public Party GetCurrentGroup(PlayerCharacter player)
        {
            return DungeonGroups.FirstOrDefault(x => x.Members.Contains(player) || x.PendingInvites.Contains(player));
        }

        public Party CreateParty(bool isQueueGroup, params PlayerCharacter[] players)
        {
            var party = new Party(isQueueGroup, players);
            DungeonGroups.Add(party);
            return party;
        }

        public void DisbandParty(Party party)
        {
            DungeonGroups.Remove(party);
        }

        public bool IsLeader(Party party, PlayerCharacter player)
        {
            return party.Leader.Equals(player);
        }

        public bool SetLeader(Party party, PlayerCharacter player)
        {
            if (party.Members.Any(x => x.Equals(player)))
            {
                party.Members.Remove(player);
                party.Members.Insert(0, player);
                return true;
            }
            return false;
        }

        public bool InvitePlayer(Party party, PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            if (!party.Members.Contains(player)
                && !party.PendingInvites.Contains(player)
                && party.Members.Count + party.PendingInvites.Count < settings.DungeonPartySize)
            {
                party.PendingInvites.Add(player);
                return true;
            }
            return false;
        }

        public bool AcceptInvite(Party party, PlayerCharacter player)
        {
            var settings = SettingsManager.GetGameSettings();
            if (party.PendingInvites.Contains(player)
                && party.Members.Count + party.PendingInvites.Count < settings.DungeonPartySize)
            {
                party.PendingInvites.Remove(player);
                AddPlayer(party, player);
                return true;
            }
            return false;
        }

        public bool DeclineInvite(Party party, PlayerCharacter player)
        {
            if (party.PendingInvites.Contains(player))
            {
                party.PendingInvites.Remove(player);
                return true;
            }
            return false;
        }

        public bool AddPlayer(Party party, PlayerCharacter player)
        {
            if (party.State == PartyState.Forming)
            {
                var settings = SettingsManager.GetGameSettings();
                if (party.Members.Count < settings.DungeonPartySize)
                {
                    party.Members.Add(player);
                    if (party.Members.Count == settings.DungeonPartySize)
                    {
                        party.State = PartyState.Full;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool RemovePlayer(Party party, PlayerCharacter player)
        {
            if (party.Members.Contains(player))
            {
                if (party.State != PartyState.Started && party.State != PartyState.Complete)
                {
                    party.Members.Remove(player);
                    if (party.Members.Count <= 0)
                    {
                        party.State = PartyState.Disbanded;
                    }
                    else
                    {
                        party.State = PartyState.Forming;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool SetReady(Party party)
        {
            if (party.State == PartyState.Forming && party.PendingInvites.Count == 0)
            {
                party.State = PartyState.Full;
                return true;
            }
            return false;
        }

        public bool UnsetReady(Party party)
        {
            var settings = SettingsManager.GetGameSettings();
            if (party.State == PartyState.Full && party.Members.Count < settings.DungeonPartySize)
            {
                party.State = PartyState.Forming;
                return true;
            }
            return false;
        }

        public int GetDungeonCost(PlayerCharacter player, GameSettings settings)
        {
            return settings.DungeonBaseCost + (player.Level - PlayerSystem.MinLevel) * settings.DungeonLevelCost;
        }

        public int GetDungeonCost(PlayerCharacter player)
        {
            return GetDungeonCost(player, SettingsManager.GetGameSettings());
        }

        public bool CanStartDungeon(Party party)
        {
            return party.State == PartyState.Full;
        }

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

        public bool CanProcessUpdate(Party party, TimeSpan stepTime)
        {
            return party.LastUpdate == null || party.LastUpdate + stepTime <= DateTime.Now;
        }

        public Dictionary<CharacterClass, int> GetClassModifiers(Party party)
        {
            return party.Members.GroupBy(x => x.CharacterClass).ToDictionary(x => x.Key, x => 1 / x.Count());
        }

        public float GetSuccessChance(PlayerCharacter player)
        {
            return player.CharacterClass.SuccessChance + EquipmentSystem.GetEquippedGear(UserSystem.GetUserById(player.UserId)).Sum(x => x.SuccessChance);
        }

        public float GetSuccessChance(Party party)
        {
            var classMultipliers = GetClassModifiers(party);
            return party.Members.Sum(x => GetSuccessChance(x) * classMultipliers[x.CharacterClass]);
        }

        public float GetDeathChance(PlayerCharacter player)
        {
            return player.CharacterClass.PreventDeathBonus + EquipmentSystem.GetEquippedGear(UserSystem.GetUserById(player.UserId)).Sum(x => x.PreventDeathBonus);
        }

        public float GetDeathChance(Party party)
        {
            var classMultipliers = GetClassModifiers(party);
            return party.Members.Sum(x => GetDeathChance(x) * classMultipliers[x.CharacterClass]);
        }

        public bool CalculateCheck(float chance)
        {
            return Random.NextDouble() < chance;
        }

        private void ProgressDungeon(Party party)
        {
            string message;
            if (party.CurrentEncounter == 0)
            {
                message = party.Run.Dungeon.Introduction;
                party.StepComplete = true;
            }
            else if (party.StepComplete)
            {
                var encounter = party.Run.Dungeon.Encounters.ElementAt(party.CurrentEncounter - 1);
                var difficulty = encounter.Levels.FirstOrDefault(x => x.Mode.Equals(party.Run.Mode)).Difficulty;
                var successChance = GetSuccessChance(party);
                if (CalculateCheck(difficulty + successChance))
                {
                    message = party.Run.Dungeon.Encounters.ElementAt(party.CurrentEncounter - 1).CompleteText;
                    party.CurrentEncounter++;
                    party.StepComplete = false;
                    if (party.CurrentEncounter > party.Run.Dungeon.Encounters.Count)
                    {
                        party.State = PartyState.Complete;
                    }
                }
                else
                {
                    message = party.Run.Dungeon.FailureText;
                    party.CurrentEncounter = 0;
                    party.StepComplete = false;
                    party.State = PartyState.Failed;
                }
            }
            else
            {
                message = party.Run.Dungeon.Encounters.ElementAt(party.CurrentEncounter - 1).SetupText;
                party.StepComplete = true;
            }
            party.LastUpdate = DateTime.Now;
            DungeonProgress?.Invoke(party, message);
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
                //TODO: Process rewards
                //Players earn experience in a convoluted formula that we're looking at changing.
                //Loot and pets are awarded based on the individual player's item find (just that value as a % chance)
                //  If they roll success on loot, give them the first item from that dungeon that they don't already have
                //      After that, make sure the roll was good enough for that quality of loot (basically add the item rarity * 5 to the roll and make sure it's still under)
                //Pets chance is wild, check the awardPet function in the og Dungeon class
                //  The actual pet is chosen when the dungeon finishes processing back in the main loop
                //  We will probably just have the pet module listen for the dungeon complete step and then give out pets that way
                //  Should probably also do that for loot drops too, since both systems are separate from the actual dungeon process
            }
            var toFail = DungeonGroups.Where(x => x.State == PartyState.Failed && CanProcessUpdate(x, stepTime));
            foreach (var party in toFail)
            {
                var deathChance = settings.DungeonDeathChance - GetDeathChance(party);
                var dead = party.Members.Where(x => CalculateCheck(deathChance)).ToList();
                //TODO: Death chance and penalties
                //Currently, the class death avoidance chance for each player is added to the party death avoidance chance
                //  For whatever reason, death avoidance chance on gear is not being used at all
                //Then, for each player, they have a 25% chance to die, minus the party chance, and then minus their class's chance
                //  This means that each player's class contributes twice to their own chance to avoid dying, but their gear does nothing
                //If someone dies
                //  They lose as much xp and coins as they would have gained (mostly the same formula, but without the gear bonuses, min 3)
                DungeonFailure?.Invoke(party, dead);
                // "It's a sad thing your adventure has ended here. No XP or Coins have been awarded."
                // "Sadly, you have died. You lost " + xp + " XP and " + coins + " Coins."
            }
            return Task.CompletedTask;
        }
    }
}
