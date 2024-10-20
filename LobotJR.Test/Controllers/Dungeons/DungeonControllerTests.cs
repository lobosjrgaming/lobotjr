using Autofac;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Controllers.Dungeons
{
    [TestClass]
    public class DungeonControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private DungeonController DungeonController;
        private PartyController PartyController;
        private PetController PetController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            DungeonController = AutofacMockSetup.Container.Resolve<DungeonController>();
            PartyController = AutofacMockSetup.Container.Resolve<PartyController>();
            PetController = AutofacMockSetup.Container.Resolve<PetController>();
            AutofacMockSetup.ResetPlayers();
            PartyController.ResetGroups();
        }

        [TestMethod]
        public void GetDungeonNameDoesNotShowDefaultModeName()
        {
            var db = ConnectionManager.CurrentConnection;
            var mode = db.DungeonModeData.Read(x => x.IsDefault).First();
            var dungeon = db.DungeonData.Read().First();
            var name = DungeonController.GetDungeonName(dungeon, mode);
            Assert.IsFalse(name.Contains(mode.Name));
            Assert.IsFalse(name.Contains('['));
            Assert.IsTrue(name.Contains(dungeon.Name));
        }

        [TestMethod]
        public void GetDungeonNameIncludesModeNameIfNotDefault()
        {
            var db = ConnectionManager.CurrentConnection;
            var mode = db.DungeonModeData.Read(x => !x.IsDefault).First();
            var dungeon = db.DungeonData.Read().First();
            var name = DungeonController.GetDungeonName(dungeon, mode);
            Assert.IsTrue(name.Contains(mode.Name));
            Assert.IsTrue(name.Contains('['));
            Assert.IsTrue(name.Contains(dungeon.Name));
        }

        [TestMethod]
        public void ParseDungeonIdGetsDungeonAndMode()
        {
            var db = ConnectionManager.CurrentConnection;
            var mode = db.DungeonModeData.Read(x => !x.IsDefault).First();
            var dungeon = db.DungeonData.Read().First();
            var run = DungeonController.ParseDungeonId($"{dungeon.Id}{mode.Flag}");
            Assert.AreEqual(dungeon.Id, run.DungeonId);
            Assert.AreEqual(mode.Id, run.ModeId);
        }

        [TestMethod]
        public void ParseDungeonIdUsesDefaultModeIfNoFlagSpecified()
        {
            var db = ConnectionManager.CurrentConnection;
            var mode = db.DungeonModeData.Read(x => x.IsDefault).First();
            var dungeon = db.DungeonData.Read().First();
            var run = DungeonController.ParseDungeonId($"{dungeon.Id}");
            Assert.AreEqual(dungeon.Id, run.DungeonId);
            Assert.AreEqual(mode.Id, run.ModeId);
        }

        [TestMethod]
        public void GetAllDungeonsGetsAllDungeonAndModePairs()
        {
            var db = ConnectionManager.CurrentConnection;
            var modes = db.DungeonModeData.Read();
            var dungeons = db.DungeonData.Read();
            var allDungeons = DungeonController.GetAllDungeons();
            Assert.AreEqual(modes.Count() * dungeons.Count(), allDungeons.Count());
            foreach (var mode in modes)
            {
                foreach (var dungeon in dungeons)
                {
                    Assert.IsTrue(allDungeons.Any(x => x.DungeonId.Equals(dungeon.Id) && x.ModeId.Equals(mode.Id)));
                }
            }
        }

        [TestMethod]
        public void GetEligibleDungeonsForPlayerReturnsEmptyCollectionForLowLevelPlayers()
        {
            var db = ConnectionManager.CurrentConnection;
            SettingsManager.GetGameSettings().DungeonLevelRestrictions = true;
            var player = db.PlayerCharacters.Read().First();
            player.Level = 1;
            var dungeons = DungeonController.GetEligibleDungeons(player);
            Assert.IsFalse(dungeons.Any());
        }

        [TestMethod]
        public void GetEligibleDungeonsForPlayerGetsDungeonsInPlayersLevelRange()
        {
            var db = ConnectionManager.CurrentConnection;
            SettingsManager.GetGameSettings().DungeonLevelRestrictions = true;
            var player = db.PlayerCharacters.Read().First();
            player.Level = 5;
            var dungeons = DungeonController.GetEligibleDungeons(player);
            Assert.AreEqual(1, dungeons.Count());
        }

        [TestMethod]
        public void GetEligibleDungeonsForPlayerGetsDungeonsOnBoundaries()
        {
            var db = ConnectionManager.CurrentConnection;
            SettingsManager.GetGameSettings().DungeonLevelRestrictions = true;
            var player = db.PlayerCharacters.Read().First();
            player.Level = 10;
            var dungeons = DungeonController.GetEligibleDungeons(player);
            Assert.AreEqual(2, dungeons.Count());
        }

        [TestMethod]
        public void GetEligibleDungeonsForPlayerGetsAllDungeonsIfLevelRestrictionsAreDisabled()
        {
            var db = ConnectionManager.CurrentConnection;
            SettingsManager.GetGameSettings().DungeonLevelRestrictions = false;
            var player = db.PlayerCharacters.Read().First();
            player.Level = 1;
            var dungeons = DungeonController.GetEligibleDungeons(player);
            Assert.AreEqual(4, dungeons.Count());
        }

        [TestMethod]
        public void GetEligibleDungeonsForPartyGetsIntersectionOfAllPlayersDungeons()
        {
            var db = ConnectionManager.CurrentConnection;
            SettingsManager.GetGameSettings().DungeonLevelRestrictions = true;
            var player1 = db.PlayerCharacters.Read().First();
            player1.Level = 5;
            var player2 = db.PlayerCharacters.Read().Last();
            player2.Level = 10;
            var dungeons = DungeonController.GetEligibleDungeons(new Party(false, player1, player2));
            Assert.AreEqual(1, dungeons.Count());
        }

        [TestMethod]
        public void GetEligibleDungeonsForPartyReturnsEmptyCollectionIfNoIntersection()
        {
            var db = ConnectionManager.CurrentConnection;
            SettingsManager.GetGameSettings().DungeonLevelRestrictions = true;
            var player1 = db.PlayerCharacters.Read().First();
            player1.Level = 5;
            var player2 = db.PlayerCharacters.Read().Last();
            player2.Level = 15;
            var dungeons = DungeonController.GetEligibleDungeons(new Party(false, player1, player2));
            Assert.AreEqual(0, dungeons.Count());
        }

        [TestMethod]
        public void GetDungeonCostGetsCorrectDungeonForLevel()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            player.Level = 10;
            var settings = SettingsManager.GetGameSettings();
            var expected = settings.DungeonBaseCost + 7 * settings.DungeonLevelCost;
            var actual = DungeonController.GetDungeonCost(player);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CanStartDungeonReturnsTrueIfPartyStateFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var player1 = db.PlayerCharacters.Read().First();
            player1.Level = 10;
            var player2 = db.PlayerCharacters.Read().First();
            player2.Level = 15;
            var party = new Party(false, player1, player2)
            {
                State = PartyState.Full
            };
            var result = DungeonController.CanStartDungeon(party);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanStartDungeonReturnsFalseIfPartyStateNotFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var player1 = db.PlayerCharacters.Read().First();
            player1.Level = 10;
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            player2.Level = 15;
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            player3.Level = 20;
            var party = new Party(false, player1, player2, player3)
            {
                State = PartyState.Forming
            };
            var result = DungeonController.CanStartDungeon(party);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryStartDungeonBeginsDungeonRunForGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player1 = db.PlayerCharacters.Read().First();
            player1.Currency = DungeonController.GetDungeonCost(player1, settings);
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            player2.Currency = DungeonController.GetDungeonCost(player2, settings);
            var party = new Party(false, player1, player2)
            {
                State = PartyState.Full
            };
            var dungeon = db.DungeonData.Read().First();
            var mode = db.DungeonModeData.Read().First();
            var result = DungeonController.TryStartDungeon(party, dungeon.Id, mode.Id, out var _);
            Assert.IsTrue(result);
            Assert.AreEqual(PartyState.Started, party.State);
            Assert.AreEqual(0, player1.Currency);
            Assert.AreEqual(0, player2.Currency);
        }

        [TestMethod]
        public void TryStartDungeonReturnsFalseIfPartyStateNotFull()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player1 = db.PlayerCharacters.Read().First();
            var p1Coins = player1.Currency = DungeonController.GetDungeonCost(player1, settings);
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var p2Coins = player2.Currency = DungeonController.GetDungeonCost(player2, settings);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            var p3Coins = player3.Currency = DungeonController.GetDungeonCost(player3, settings);
            var party = new Party(false, player1, player2)
            {
                State = PartyState.Forming
            };
            var dungeon = db.DungeonData.Read().First();
            var mode = db.DungeonModeData.Read().First();
            var result = DungeonController.TryStartDungeon(party, dungeon.Id, mode.Id, out var broke);
            Assert.IsFalse(result);
            Assert.IsFalse(broke.Any());
            Assert.AreEqual(PartyState.Forming, party.State);
            Assert.AreEqual(p1Coins, player1.Currency);
            Assert.AreEqual(p2Coins, player2.Currency);
            Assert.AreEqual(p3Coins, player3.Currency);
        }

        [TestMethod]
        public void TryStartDungeonReturnsFalseIfPlayerHasInsufficientCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player1 = db.PlayerCharacters.Read().First();
            player1.Level = 20;
            var p1Coins = player1.Currency = 0;
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var p2Coins = player2.Currency = DungeonController.GetDungeonCost(player2, settings);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            var p3Coins = player3.Currency = DungeonController.GetDungeonCost(player3, settings);
            var party = new Party(false, player1, player2)
            {
                State = PartyState.Full
            };
            var dungeon = db.DungeonData.Read().First();
            var mode = db.DungeonModeData.Read().First();
            var result = DungeonController.TryStartDungeon(party, dungeon.Id, mode.Id, out var broke);
            Assert.IsFalse(result);
            Assert.IsTrue(broke.Contains(player1));
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.AreEqual(p1Coins, player1.Currency);
            Assert.AreEqual(p2Coins, player2.Currency);
            Assert.AreEqual(p3Coins, player3.Currency);
        }

        private Party SetupProcessParty()
        {
            var db = ConnectionManager.CurrentConnection;
            var characterClass = db.CharacterClassData.Read(x => x.CanPlay).First();
            var player1 = db.PlayerCharacters.Read().First();
            var player2 = db.PlayerCharacters.Read().ElementAt(2);
            var player3 = db.PlayerCharacters.Read().ElementAt(3);
            player1.CharacterClass = characterClass;
            player2.CharacterClass = characterClass;
            player3.CharacterClass = characterClass;
            player1.Experience = (int)(4 * Math.Pow(10, 3) + 50) + 200;
            player2.Experience = (int)(4 * Math.Pow(15, 3) + 50) + 200;
            player3.Experience = (int)(4 * Math.Pow(20, 3) + 50) + 200;
            player1.Level = 10;
            player2.Level = 15;
            player3.Level = 20;
            player1.Currency = DungeonController.GetDungeonCost(player1);
            player2.Currency = DungeonController.GetDungeonCost(player2);
            player3.Currency = DungeonController.GetDungeonCost(player3);
            var dungeon = db.DungeonData.Read().First();
            var mode = db.DungeonModeData.Read().Last();
            var party = PartyController.CreateParty(false, player1, player2, player3);
            party.State = PartyState.Started;
            party.DungeonId = dungeon.Id;
            party.ModeId = mode.Id;
            party.LastUpdate = DateTime.Now - TimeSpan.FromSeconds(SettingsManager.GetGameSettings().DungeonStepTime);
            return party;
        }

        [TestMethod]
        public async Task ProcessSendsIntroMessageForNewGroup()
        {
            var party = SetupProcessParty();
            var message = DungeonController.GetDungeonById(party.DungeonId).Introduction;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, message), Times.Once);
            Assert.AreEqual(1, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
        }

        [TestMethod]
        public async Task ProcessSendsSetupMessageForEncounter()
        {
            var party = SetupProcessParty();
            party.CurrentEncounter = 1;
            party.StepState = StepState.Setup;
            var message = DungeonController.GetDungeonById(party.DungeonId).Encounters.First().SetupText;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, message), Times.Once);
            Assert.AreEqual(1, party.CurrentEncounter);
            Assert.AreEqual(StepState.Resolving, party.StepState);
        }

        [TestMethod]
        public async Task ProcessSendsResolveMessageForEncounter()
        {
            var party = SetupProcessParty();
            party.CurrentEncounter = 1;
            party.StepState = StepState.Resolving;
            var encounter = DungeonController.GetDungeonById(party.DungeonId).Encounters.First();
            var levels = encounter.Levels.Where(x => x.Mode.Id.Equals(party.ModeId)).First();
            levels.Difficulty = 1;
            var message = encounter.Enemy;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, It.Is<string>(y => y.Contains(message))), Times.Once);
            Assert.AreEqual(1, party.CurrentEncounter);
            Assert.AreEqual(StepState.Complete, party.StepState);
        }

        [TestMethod]
        public async Task ProcessIncludesClassBonusesToEncounterChance()
        {
            var party = SetupProcessParty();
            party.CurrentEncounter = 1;
            party.StepState = StepState.Resolving;
            var encounter = DungeonController.GetDungeonById(party.DungeonId).Encounters.First();
            var levels = encounter.Levels.Where(x => x.Mode.Id.Equals(party.ModeId)).First();
            levels.Difficulty = 0;
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                member.CharacterClass.SuccessChance = 1;
            }
            var message = encounter.Enemy;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            foreach (var member in members)
            {
                member.CharacterClass.SuccessChance = 0;
            }
            listener.Verify(x => x(party, It.Is<string>(y => y.Contains(message))), Times.Once);
            Assert.AreEqual(1, party.CurrentEncounter);
            Assert.AreEqual(StepState.Complete, party.StepState);
        }

        [TestMethod]
        public async Task ProcessIncludesItemBonusesToEncounterChance()
        {
            var party = SetupProcessParty();
            party.CurrentEncounter = 1;
            var item = ConnectionManager.CurrentConnection.ItemData.Read().First();
            var old = item.SuccessChance;
            item.SuccessChance = 1 / 0.75f;
            foreach (var player in party.Members)
            {
                ConnectionManager.CurrentConnection.Inventories.Create(new Inventory()
                {
                    Item = item,
                    IsEquipped = true,
                    UserId = player,
                });
            }
            ConnectionManager.CurrentConnection.Commit();
            party.StepState = StepState.Resolving;
            var encounter = DungeonController.GetDungeonById(party.DungeonId).Encounters.First();
            var levels = encounter.Levels.Where(x => x.Mode.Id.Equals(party.ModeId)).First();
            levels.Difficulty = 0;
            var message = encounter.Enemy;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            item.SuccessChance = 0;
            listener.Verify(x => x(party, It.Is<string>(y => y.Contains(message))), Times.Once);
            Assert.AreEqual(1, party.CurrentEncounter);
            Assert.AreEqual(StepState.Complete, party.StepState);
            item.SuccessChance = old;
        }

        [TestMethod]
        public async Task ProcessTriggersFailureForFailedEncounter()
        {
            var party = SetupProcessParty();
            party.CurrentEncounter = 1;
            party.StepState = StepState.Resolving;
            var encounter = DungeonController.GetDungeonById(party.DungeonId).Encounters.First();
            var levels = encounter.Levels.Where(x => x.Mode.Id.Equals(party.ModeId)).First();
            levels.Difficulty = 0;
            var message = DungeonController.GetDungeonById(party.DungeonId).FailureText;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            levels.Difficulty = 1;
            listener.Verify(x => x(party, message), Times.Once);
            Assert.AreEqual(1, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
            Assert.AreEqual(PartyState.Failed, party.State);
        }

        [TestMethod]
        public async Task ProcessSendsEncounterCompleteMessage()
        {
            var party = SetupProcessParty();
            party.CurrentEncounter = 1;
            party.StepState = StepState.Complete;
            var encounter = DungeonController.GetDungeonById(party.DungeonId).Encounters.First();
            var message = encounter.CompleteText;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, message), Times.Once);
            Assert.AreEqual(2, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
            Assert.AreEqual(PartyState.Started, party.State);
        }

        [TestMethod]
        public async Task ProcessWaitsForMessageDelayForInProgressGroups()
        {
            var party = SetupProcessParty();
            party.LastUpdate = DateTime.Now;
            var message = DungeonController.GetDungeonById(party.DungeonId).Introduction;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, message), Times.Never);
            Assert.AreEqual(0, party.CurrentEncounter);
        }

        [TestMethod]
        public async Task ProcessSendsMessageAfterDelay()
        {
            var party = SetupProcessParty();
            party.LastUpdate = DateTime.Now - TimeSpan.FromSeconds(SettingsManager.GetGameSettings().DungeonStepTime);
            var message = DungeonController.GetDungeonById(party.DungeonId).Introduction;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, message), Times.Once);
            Assert.AreEqual(1, party.CurrentEncounter);
        }

        [TestMethod]
        public async Task ProcessSetsPartyStateCompleteOnDungeonFinish()
        {
            var party = SetupProcessParty();
            var count = DungeonController.GetDungeonById(party.DungeonId).Encounters.Count();
            party.CurrentEncounter = count;
            party.StepState = StepState.Complete;
            var encounter = DungeonController.GetDungeonById(party.DungeonId).Encounters.Last();
            var message = encounter.CompleteText;
            var listener = new Mock<DungeonController.DungeonProgressHandler>();
            DungeonController.DungeonProgress += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, message), Times.Once);
            Assert.AreEqual(count + 1, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
            Assert.AreEqual(PartyState.Complete, party.State);
        }

        [TestMethod]
        public async Task ProcessCompletesDungeonAndAwardsLootAndPets()
        {
            var db = ConnectionManager.CurrentConnection;
            db.Stables.Delete();
            foreach (var item in db.DungeonData.Read().First().Loot)
            {
                //Drop chance is actually a penalty applied to the party's item
                //find stat, so setting it to -1 guarantees a drop even with 0
                //item find from gear
                item.DropChance = -1;
            }
            var party = SetupProcessParty();
            party.State = PartyState.Complete;
            SettingsManager.GetGameSettings().DungeonCritChance = 0;
            var listener = new Mock<DungeonController.DungeonCompleteHandler>();
            DungeonController.DungeonComplete += listener.Object;
            var petListener = new Mock<PetController.PetFoundHandler>();
            PetController.PetFound += petListener.Object;
            await DungeonController.Process();
            foreach (var item in db.DungeonData.Read().First().Loot)
            {
                item.DropChance = 0;
            }
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                var xp = (int)Math.Round(11 + (member.Level - 2) * 3f);
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level));
                listener.Verify(x => x(member, xp, coins, It.IsNotNull<Item>(), false, false, false), Times.Once);
                if (!member.Equals(party.Members.First()))
                {
                    petListener.Verify(x => x(It.Is<User>(y => y.TwitchId.Equals(member.UserId)), It.IsNotNull<Stable>()));
                }
            }
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
        }

        [TestMethod]
        public async Task ProcessDoesNotGiveDuplicateLootOnDungeonComplete()
        {
            var db = ConnectionManager.CurrentConnection;
            db.Stables.Delete();
            foreach (var item in db.DungeonData.Read().First().Loot)
            {
                item.DropChance = 1;
            }
            var party = SetupProcessParty();
            party.State = PartyState.Complete;
            SettingsManager.GetGameSettings().DungeonCritChance = 0;
            var listener = new Mock<DungeonController.DungeonCompleteHandler>();
            DungeonController.DungeonComplete += listener.Object;
            var members = PartyController.GetPartyPlayers(party);
            var first = members.First();
            var items = db.ItemData.Read();
            var filter = db.EquippableData.Read(x => x.CharacterClass.Equals(first.CharacterClass));
            var itemsToAdd = items.Where(x => filter.Any(y => y.ItemType.Equals(x.Type)));
            foreach (var itemToAdd in itemsToAdd)
            {
                db.Inventories.Create(new Inventory() { Item = itemToAdd, UserId = first.UserId });
            }
            db.Inventories.Commit();
            await DungeonController.Process();
            foreach (var item in db.DungeonData.Read().First().Loot)
            {
                item.DropChance = 0;
            }
            listener.Verify(x => x(first, It.IsAny<int>(), It.IsAny<int>(), It.IsNotNull<Item>(), false, false, false), Times.Never);
            listener.Verify(x => x(first, It.IsAny<int>(), It.IsAny<int>(), null, false, false, false), Times.Once());
        }

        [TestMethod]
        public async Task ProcessCompletesDungeonWithNoLootOrPets()
        {
            var party = SetupProcessParty();
            party.State = PartyState.Complete;
            var members = PartyController.GetPartyPlayers(party);
            var baseXp = members.ToDictionary(x => x.UserId, x => x.Experience);
            var baseCoins = members.ToDictionary(x => x.UserId, x => x.Currency);
            SettingsManager.GetGameSettings().DungeonCritChance = 0;
            var listener = new Mock<DungeonController.DungeonCompleteHandler>();
            DungeonController.DungeonComplete += listener.Object;
            await DungeonController.Process();
            foreach (var member in members)
            {
                var xp = (int)Math.Round(11 + (member.Level - 2) * 3f);
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level));
                listener.Verify(x => x(member, xp, coins, null, false, false, false), Times.Once);
                Assert.AreEqual(baseXp[member.UserId] + xp, member.Experience);
                Assert.AreEqual(baseCoins[member.UserId] + coins, member.Currency);
            }
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
        }

        [TestMethod]
        public async Task ProcessCompletesDungeonWithCriticalExperience()
        {
            var party = SetupProcessParty();
            party.State = PartyState.Complete;
            var settings = SettingsManager.GetGameSettings();
            settings.DungeonCritChance = 1;
            var listener = new Mock<DungeonController.DungeonCompleteHandler>();
            DungeonController.DungeonComplete += listener.Object;
            await DungeonController.Process();
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                var xp = (int)Math.Round((11 + (member.Level - 2) * 3f) * (1 + settings.DungeonCritBonus));
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level));
                listener.Verify(x => x(member, xp, coins, null, false, false, true), Times.Once);
            }
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
        }

        [TestMethod]
        public async Task ProcessCompletesDungeonAndGrantsGroupFinderBonus()
        {
            var db = ConnectionManager.CurrentConnection;
            var party = SetupProcessParty();
            party.IsQueueGroup = true;
            party.State = PartyState.Complete;
            SettingsManager.GetGameSettings().DungeonCritChance = 0;
            var listener = new Mock<DungeonController.DungeonCompleteHandler>();
            DungeonController.DungeonComplete += listener.Object;
            await DungeonController.Process();
            db.Commit();
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                var xp = (int)Math.Round((11 + (member.Level - 2) * 3f) * 2);
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level) * 2);
                listener.Verify(x => x(member, xp, coins, null, true, true, false), Times.Once);
                Assert.IsTrue(db.DungeonLockouts.Read(x => x.UserId.Equals(member.UserId)).Any());
            }
            Assert.AreEqual(PartyState.Disbanded, party.State);
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
        }

        [TestMethod]
        public async Task ProcessCompletesDungeonAndDoesNotGrantGroupFinderDuringLockout()
        {
            var db = ConnectionManager.CurrentConnection;
            var party = SetupProcessParty();
            party.IsQueueGroup = true;
            party.State = PartyState.Complete;
            var timer = db.DungeonTimerData.Read().First();
            foreach (var member in party.Members)
            {
                db.DungeonLockouts.Create(new DungeonLockout()
                {
                    UserId = member,
                    Timer = timer,
                    Time = DateTime.Now
                });
            }
            db.Commit();
            SettingsManager.GetGameSettings().DungeonCritChance = 0;
            var listener = new Mock<DungeonController.DungeonCompleteHandler>();
            DungeonController.DungeonComplete += listener.Object;
            await DungeonController.Process();
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                var xp = (int)Math.Round(11 + (member.Level - 2) * 3f);
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level));
                listener.Verify(x => x(member, xp, coins, null, true, false, false), Times.Once);
            }
            Assert.AreEqual(PartyState.Disbanded, party.State);
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
        }

        [TestMethod]
        public async Task ProcessDisbandsCompletedGroupFinderGroup()
        {
            var party = SetupProcessParty();
            party.IsQueueGroup = true;
            party.State = PartyState.Complete;
            SettingsManager.GetGameSettings().DungeonCritChance = 0;
            await DungeonController.Process();
            Assert.AreEqual(0, PartyController.PartyCount);
        }

        [TestMethod]
        public async Task ProcessAddsDungeonMetricsDataOnDungeonComplete()
        {
            var db = ConnectionManager.CurrentConnection;
            var party = SetupProcessParty();
            party.State = PartyState.Complete;
            SettingsManager.GetGameSettings().DungeonCritChance = 0;
            var listener = new Mock<DungeonController.DungeonCompleteHandler>();
            DungeonController.DungeonComplete += listener.Object;
            await DungeonController.Process();
            db.Commit();
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                var xp = (int)Math.Round(11 + (member.Level - 2) * 3f);
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level));
                listener.Verify(x => x(member, xp, coins, null, false, false, false), Times.Once);
            }
            Assert.AreEqual(PartyState.Full, party.State);
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
            Assert.AreEqual(1, db.DungeonHistories.Read().Count());
        }

        [TestMethod]
        public async Task ProcessHandlesDungeonFailure()
        {
            var party = SetupProcessParty();
            party.State = PartyState.Failed;
            var members = PartyController.GetPartyPlayers(party);
            var dungeon = DungeonController.GetDungeonById(party.DungeonId);
            var encounter = dungeon.Encounters.First();
            var baseXp = members.ToDictionary(x => x.UserId, x => x.Experience);
            var baseCurrency = members.ToDictionary(x => x.UserId, x => x.Currency);
            SettingsManager.GetGameSettings().DungeonDeathChance = 0;
            var listener = new Mock<DungeonController.DungeonFailureHandler>();
            DungeonController.DungeonFailure += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, It.Is<IEnumerable<PlayerCharacter>>(y => y.Count() == 0)), Times.Once);
            foreach (var member in members)
            {
                Assert.AreEqual(baseXp[member.UserId], member.Experience);
                Assert.AreEqual(baseCurrency[member.UserId], member.Currency);
            }
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
            Assert.AreEqual(PartyState.Full, party.State);
        }

        [TestMethod]
        public async Task ProcessHandlesPlayerDeathOnFailure()
        {
            var party = SetupProcessParty();
            party.State = PartyState.Failed;
            var dungeon = DungeonController.GetDungeonById(party.DungeonId);
            var encounter = dungeon.Encounters.First();
            var baseXp = new Dictionary<string, int>();
            var baseCurrency = new Dictionary<string, int>();
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                baseXp.Add(member.UserId, member.Experience);
                baseCurrency.Add(member.UserId, member.Currency);
            }
            SettingsManager.GetGameSettings().DungeonDeathChance = 1;
            var listener = new Mock<DungeonController.DungeonFailureHandler>();
            DungeonController.DungeonFailure += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, It.Is<IEnumerable<PlayerCharacter>>(y => y.Count() == party.Members.Count)), Times.Once);
            foreach (var member in members)
            {
                var xp = (int)Math.Round(11 + (member.Level - 2) * 3f);
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level));
                Assert.AreEqual(baseXp[member.UserId] - xp, member.Experience);
                Assert.AreEqual(baseCurrency[member.UserId] - coins, member.Currency);
            }
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
            Assert.AreEqual(PartyState.Full, party.State);
        }

        [TestMethod]
        public async Task ProcessPlayerDeathDoesNotReduceLevel()
        {
            var party = SetupProcessParty();
            party.State = PartyState.Failed;
            var dungeon = DungeonController.GetDungeonById(party.DungeonId);
            var encounter = dungeon.Encounters.First();
            var members = PartyController.GetPartyPlayers(party);
            foreach (var member in members)
            {
                member.Experience -= 200;
            }
            var baseXp = members.ToDictionary(x => x.UserId, x => x.Experience);
            var baseLevel = members.ToDictionary(x => x.UserId, x => x.Level);
            SettingsManager.GetGameSettings().DungeonDeathChance = 1;
            var listener = new Mock<DungeonController.DungeonFailureHandler>();
            DungeonController.DungeonFailure += listener.Object;
            await DungeonController.Process();
            listener.Verify(x => x(party, It.Is<IEnumerable<PlayerCharacter>>(y => y.Count() == party.Members.Count)), Times.Once);
            foreach (var member in members)
            {
                var coins = (int)Math.Round(50 * (1 + 0.05f * member.Level));
                Assert.AreEqual(baseXp[member.UserId], member.Experience);
                Assert.AreEqual(baseLevel[member.UserId], member.Level);
            }
            Assert.AreEqual(0, party.CurrentEncounter);
            Assert.AreEqual(StepState.Setup, party.StepState);
            Assert.AreEqual(PartyState.Full, party.State);
        }

        [TestMethod]
        public async Task ProcessAddsDungeonMetricsDataOnDungeonFailure()
        {
            var db = ConnectionManager.CurrentConnection;
            var party = SetupProcessParty();
            party.State = PartyState.Failed;
            SettingsManager.GetGameSettings().DungeonDeathChance = 0;
            await DungeonController.Process();
            db.Commit();
            Assert.AreEqual(1, db.DungeonHistories.Read().Count());
            Assert.AreEqual(3, db.DungeonParticipants.Read().Count());
        }
    }
}
