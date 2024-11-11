using Autofac;
using LobotJR.Command.Controller.Pets;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LobotJR.Test.Controllers.Pets
{
    [TestClass]
    public class PetControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private PetController PetController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PetController = AutofacMockSetup.Container.Resolve<PetController>();
            AutofacMockSetup.ResetPlayers();
            PetController.ClearPendingReleases();
        }

        [TestMethod]
        public void GetsStableForUser()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var stables = PetController.GetStableForUser(user);
            Assert.AreEqual(2, stables.Count());
            Assert.IsTrue(stables.Any(x => x.Name.Equals("FirstPet")));
            Assert.IsTrue(stables.Any(x => x.Name.Equals("LastPet")));
        }

        [TestMethod]
        public void GetsActivePetForUser()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var stables = db.Stables.Read(x => x.UserId.Equals(user.TwitchId));
            stables.First().IsActive = true;
            db.Commit();
            var result = PetController.GetActivePet(user);
            Assert.AreEqual(stables.First(), result);
            Assert.IsTrue(result.IsActive);
        }

        [TestMethod]
        public void GetsActivePetForPlayer()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var stables = db.Stables.Read(x => x.UserId.Equals(player.UserId));
            stables.First().IsActive = true;
            db.Commit();
            var result = PetController.GetActivePet(player);
            Assert.AreEqual(stables.First(), result);
            Assert.IsTrue(result.IsActive);
        }

        [TestMethod]
        public void ActivatePetSetsActivePetAndDeactivesOtherPets()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var stables = db.Stables.Read(x => x.UserId.Equals(user.TwitchId));
            stables.First().IsActive = true;
            db.Commit();
            var result = PetController.ActivatePet(user, stables.Last());
            Assert.AreEqual(stables.First(), result);
            Assert.IsFalse(result.IsActive);
            Assert.IsTrue(stables.Last().IsActive);
            Assert.AreEqual(1, stables.Where(x => x.IsActive).Count());
        }

        [TestMethod]
        public void DeactivatesPet()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var stables = db.Stables.Read(x => x.UserId.Equals(user.TwitchId));
            stables.First().IsActive = true;
            db.Commit();
            var result = PetController.DeactivatePet(user);
            db.Commit();
            Assert.AreEqual(stables.First(), result);
            Assert.IsFalse(result.IsActive);
            Assert.AreEqual(0, stables.Where(x => x.IsActive).Count());
        }

        [TestMethod]
        public void DeletesPet()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var oldCount = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).Count();
            PetController.DeletePet(db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).First());
            db.Commit();
            var stables = db.Stables.Read(x => x.UserId.Equals(user.TwitchId));
            stables = db.Stables.Read(x => x.UserId.Equals(user.TwitchId));
            Assert.AreEqual(oldCount - 1, stables.Count());
        }

        [TestMethod]
        public void IsHungryReturnsTrueForPetsWithoutMaxHunger()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var pet = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).First();
            pet.Hunger = SettingsManager.GetGameSettings().PetHungerMax - 1;
            var result = PetController.IsHungry(pet);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsHungryReturnsFalseForFullPets()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var pet = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).First();
            pet.Hunger = SettingsManager.GetGameSettings().PetHungerMax;
            var result = PetController.IsHungry(pet);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void FeedsPet()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player = db.PlayerCharacters.Read().First();
            player.Currency = settings.PetFeedingCost;
            var pet = db.Stables.Read(x => x.UserId.Equals(player.UserId)).First();
            var oldAffection = pet.Affection;
            pet.Hunger = 0;
            var result = PetController.Feed(player, pet);
            Assert.IsTrue(result);
            Assert.AreEqual(settings.PetHungerMax, pet.Hunger);
            Assert.AreEqual(oldAffection + settings.PetFeedingAffection, pet.Affection);
        }

        [TestMethod]
        public void FeedPetLevelsUpPet()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player = db.PlayerCharacters.Read().First();
            player.Currency = settings.PetFeedingCost;
            var pet = db.Stables.Read(x => x.UserId.Equals(player.UserId)).First();
            var oldLevel = pet.Level;
            pet.Hunger = 0;
            pet.Experience = settings.PetExperienceToLevel - settings.PetHungerMax;
            var result = PetController.Feed(player, pet);
            Assert.IsTrue(result);
            Assert.AreEqual(oldLevel + 1, pet.Level);
        }

        [TestMethod]
        public void FeedPetDoesNotLevelUpBeyondMaxLevel()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player = db.PlayerCharacters.Read().First();
            player.Currency = settings.PetFeedingCost;
            var pet = db.Stables.Read(x => x.UserId.Equals(player.UserId)).First();
            pet.Level = settings.PetLevelMax;
            pet.Hunger = 0;
            pet.Experience = settings.PetExperienceToLevel - settings.PetHungerMax;
            var result = PetController.Feed(player, pet);
            Assert.IsTrue(result);
            Assert.AreEqual(settings.PetHungerMax, pet.Hunger);
            Assert.AreEqual(settings.PetLevelMax, pet.Level);
        }

        [TestMethod]
        public void FeedPetReturnsFalseIfInsufficientCurrency()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player = db.PlayerCharacters.Read().First();
            player.Currency = 0;
            var pet = db.Stables.Read(x => x.UserId.Equals(player.UserId)).First();
            pet.Hunger = 0;
            var result = PetController.Feed(player, pet);
            Assert.IsFalse(result);
            Assert.AreEqual(0, pet.Hunger);
        }

        [TestMethod]
        public void AddHungerDecreasesPetHunger()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var player = db.PlayerCharacters.Read().First();
            player.Currency = settings.PetFeedingCost;
            var pet = db.Stables.Read(x => x.UserId.Equals(player.UserId)).First();
            pet.Hunger = settings.PetHungerMax;
            PetController.AddHunger(player, pet);
            Assert.IsTrue(pet.Hunger < settings.PetHungerMax);
        }

        [TestMethod]
        public void AddHungerIssuesWarningForLowHungerPets()
        {
            var db = ConnectionManager.CurrentConnection;
            var listener = new Mock<PetController.PetWarningHandler>();
            PetController.PetWarning += listener.Object;
            var settings = SettingsManager.GetGameSettings();
            var user = db.Users.Read().First();
            var player = db.PlayerCharacters.Read(x => x.UserId.Equals(user.TwitchId)).First();
            player.Currency = settings.PetFeedingCost;
            var pet = db.Stables.Read(x => x.UserId.Equals(player.UserId)).First();
            pet.Hunger = (int)Math.Floor(settings.PetHungerMax * 0.25f);
            PetController.AddHunger(player, pet);
            Assert.IsTrue(pet.Hunger < settings.PetHungerMax);
            listener.Verify(x => x(user, pet), Times.Once);
        }

        [TestMethod]
        public void AddHungerKillsPetsWithZeroHunger()
        {
            var db = ConnectionManager.CurrentConnection;
            var listener = new Mock<PetController.PetDeathHandler>();
            PetController.PetDeath += listener.Object;
            var settings = SettingsManager.GetGameSettings();
            var user = db.Users.Read().First();
            var player = db.PlayerCharacters.Read(x => x.UserId.Equals(user.TwitchId)).First();
            player.Currency = settings.PetFeedingCost;
            var pet = db.Stables.Read(x => x.UserId.Equals(player.UserId)).First();
            pet.Hunger = 0;
            PetController.AddHunger(player, pet);
            db.Commit();
            var stables = db.Stables.Read(x => x.UserId.Equals(player.UserId));
            Assert.IsFalse(stables.Any(x => x.Equals(pet)));
            listener.Verify(x => x(user, pet), Times.Once);
        }

        [TestMethod]
        public void FlagsForDelete()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var user = db.Users.Read().First();
            var pet = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).First();
            var result = PetController.FlagForDelete(user, pet);
            var toRelease = PetController.IsFlaggedForDelete(user);
            Assert.IsTrue(result);
            Assert.AreEqual(pet, toRelease);
        }

        [TestMethod]
        public void FlagForDeleteReturnsFalseForAlreadyFlaggedPets()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var user = db.Users.Read().First();
            var pet = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).First();
            var wrongPet = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).Last();
            PetController.FlagForDelete(user, pet);
            var result = PetController.FlagForDelete(user, wrongPet);
            var toRelease = PetController.IsFlaggedForDelete(user);
            Assert.IsFalse(result);
            Assert.AreEqual(pet, toRelease);
        }

        [TestMethod]
        public void UnflagsForDelete()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var user = db.Users.Read().First();
            var pet = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).First();
            PetController.FlagForDelete(user, pet);
            PetController.UnflagForDelete(user);
            var toRelease = PetController.IsFlaggedForDelete(user);
            Assert.IsNull(toRelease);
        }

        [TestMethod]
        public void IsFlaggedForDeleteReturnsFalseForUnflaggedPets()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var result = PetController.IsFlaggedForDelete(user);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetsAllPetData()
        {
            var db = ConnectionManager.CurrentConnection;
            var dbPets = db.PetData.Read();
            var controllerPets = PetController.GetPets();
            Assert.AreEqual(dbPets.Count(), controllerPets.Count());
            foreach (var pet in dbPets)
            {
                Assert.IsTrue(controllerPets.Any(x => x.Id.Equals(pet.Id)));
            }
        }

        [TestMethod]
        public void GetsAllRarityDataInAscendingOrderOfDropRate()
        {
            var db = ConnectionManager.CurrentConnection;
            var dbRarities = db.PetRarityData.Read().OrderBy(x => x.DropRate).ToList();
            var controllerRarities = PetController.GetRarities().ToList();
            Assert.AreEqual(dbRarities.Count, controllerRarities.Count);
            for (var i = 0; i < dbRarities.Count; i++)
            {
                Assert.AreEqual(dbRarities.ElementAt(i), controllerRarities.ElementAt(i));
            }
        }

        [TestMethod]
        public void RollForRarityRandomizesResults()
        {
            var db = ConnectionManager.CurrentConnection;
            var count = 10000;
            var rarityCount = db.PetRarityData.Read().OrderBy(x => x.DropRate).ToDictionary(x => x, y => 0);
            for (var i = 0; i < count; i++)
            {
                var roll = PetController.RollForRarity();
                rarityCount[roll]++;
            }
            var targetAdjust = 0f;
            foreach (var pair in rarityCount)
            {
                var target = (pair.Key.DropRate - targetAdjust) * count;
                targetAdjust = pair.Key.DropRate;
                Assert.IsTrue(pair.Value > target * 0.9f && pair.Value < target * 1.1f, $"Rarity with drop rate {pair.Key.DropRate} expected {target} drops +/-10%, got {pair.Value}.");
            }
        }

        [TestMethod]
        public void GrantsPets()
        {
            var db = ConnectionManager.CurrentConnection;
            db.Stables.Delete();
            db.Commit();
            var user = db.Users.Read().First();
            var rarity = PetController.GetRarities().OrderBy(x => x.DropRate).First();
            var result = PetController.GrantPet(user, rarity);
            db.Commit();
            Assert.AreEqual(rarity, result.Pet.Rarity);
            Assert.IsTrue(db.Stables.Read().Any(x => x.UserId.Equals(user.TwitchId) && x.Pet.Equals(result.Pet)));
        }

        [TestMethod]
        public void GrantsShinyPets()
        {
            var db = ConnectionManager.CurrentConnection;
            db.Stables.Delete();
            db.Commit();
            var user = db.Users.Read().First();
            var rarity = PetController.GetRarities().OrderBy(x => x.DropRate).First();
            var foundShiny = false;
            for (var i = 0; i < 1000; i++)
            {
                var result = PetController.GrantPet(user, rarity);
                if (result.IsSparkly)
                {
                    foundShiny = true;
                    break;
                }
                db.Stables.Delete();
            }
            Assert.IsTrue(foundShiny);
        }

        [TestMethod]
        public void GrantPetReturnsNullIfAllPetsOwned()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var stableCount = db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).Count();
            var rarity = PetController.GetRarities().OrderBy(x => x.DropRate).First();
            var result = PetController.GrantPet(user, rarity);
            db.Commit();
            Assert.IsNull(result);
            Assert.AreEqual(stableCount, db.Stables.Read(x => x.UserId.Equals(user.TwitchId)).Count());
        }
    }
}
