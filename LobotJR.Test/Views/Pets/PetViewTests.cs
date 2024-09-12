using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.View;
using LobotJR.Command.View.Pets;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LobotJR.Test.Views.Pets
{
    [TestClass]
    public class PetViewTests
    {
        private SettingsManager SettingsManager;
        private User User;
        private PlayerController PlayerController;
        private ConfirmationController ConfirmationController;
        private PetController Controller;
        private PetView View;

        [TestInitialize]
        public void Initialize()
        {
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            ConfirmationController = AutofacMockSetup.Container.Resolve<ConfirmationController>();
            Controller = AutofacMockSetup.Container.Resolve<PetController>();
            View = AutofacMockSetup.Container.Resolve<PetView>();
            User = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().First();
            AutofacMockSetup.ResetPlayers();
            Controller.ClearPendingReleases();
        }

        private void ClearPets()
        {
            var stables = Controller.GetStableForUser(User);
            foreach (var stable in stables)
            {
                Controller.DeletePet(stable);
            }
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
        }

        [TestMethod]
        public void ListPetsReturnsAllPets()
        {
            var response = View.ListPets(User);
            var stables = Controller.GetStableForUser(User);
            var rarities = Controller.GetRarities();
            Assert.AreEqual(rarities.Count() + 1, response.Responses.Count);
            foreach (var stable in stables)
            {
                Assert.IsTrue(response.Responses.Any(x => x.Contains(stable.Name) && x.Contains(stable.Pet.Name)));
            }
        }

        [TestMethod]
        public void ListPetsReturnsEmptyStableMessage()
        {
            ClearPets();
            var response = View.ListPets(User);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("no pets")));
        }

        [TestMethod]
        public void DetailPetDescribesPetAtIndex()
        {
            var pet = Controller.GetStableForUser(User).First();
            var response = View.DescribePet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Name)));
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Pet.Name)));
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Level.ToString())));
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Affection.ToString())));
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Hunger.ToString())));
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Stabled")));
        }

        [TestMethod]
        public void DetailPetReturnsErrorOnInvalidIndex()
        {
            var response = View.DescribePet(User, 0);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
            response = View.DescribePet(User, Controller.GetStableForUser(User).Count() + 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
        }

        [TestMethod]
        public void DetailPetReturnsEmptyStableMessage()
        {
            ClearPets();
            var response = View.DescribePet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("no pets")));
        }

        [TestMethod]
        public void RenamePetUpdatesPetName()
        {
            var pet = Controller.GetStableForUser(User).First();
            var oldName = pet.Name;
            var newName = "NewName";
            var response = View.RenamePet(User, 1, newName);
            Assert.AreEqual(newName, pet.Name);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(oldName) && x.Contains(newName)));
        }

        [TestMethod]
        public void RenamePetErrorsOnTooLongPetName()
        {
            var pet = Controller.GetStableForUser(User).First();
            var oldName = pet.Name;
            var newName = "NameThatIsDefinitelyTooLong";
            var response = View.RenamePet(User, 1, newName);
            Assert.AreEqual(oldName, pet.Name);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("16")));
        }

        [TestMethod]
        public void RenamePetReturnsErrorOnInvalidIndex()
        {
            var response = View.RenamePet(User, 0, "Test");
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
            response = View.RenamePet(User, Controller.GetStableForUser(User).Count() + 1, "Test");
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
        }

        [TestMethod]
        public void RenamePetReturnsEmptyStableMessage()
        {
            ClearPets();
            var response = View.RenamePet(User, 1, "Test");
            Assert.IsTrue(response.Responses.Any(x => x.Contains("no pets")));
        }

        [TestMethod]
        public void FeedPetResetsPetHunger()
        {
            var settings = SettingsManager.GetGameSettings();
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = settings.PetFeedingCost;
            var pet = Controller.GetStableForUser(User).First();
            pet.Hunger = 0;
            var response = View.FeedPet(User, 1);
            Assert.AreEqual(settings.PetHungerMax, pet.Hunger);
            Assert.AreEqual(0, player.Currency);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Name) && x.Contains(settings.PetFeedingCost.ToString())));
        }

        [TestMethod]
        public void FeedPetLevelsUpPet()
        {
            var settings = SettingsManager.GetGameSettings();
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = settings.PetFeedingCost;
            var pet = Controller.GetStableForUser(User).First();
            var level = pet.Level;
            pet.Hunger = 0;
            pet.Experience = settings.PetExperienceToLevel - 1;
            var response = View.FeedPet(User, 1);
            Assert.AreEqual(settings.PetHungerMax, pet.Hunger);
            Assert.AreEqual(level + 1, pet.Level);
            Assert.AreEqual(0, player.Currency);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Name) && x.Contains(settings.PetFeedingCost.ToString())));
            Assert.IsTrue(response.Responses.Any(x => x.Contains("leveled up") && x.Contains(pet.Level.ToString())));
        }

        [TestMethod]
        public void FeedPetReturnsErrorOnNotEnoughCoins()
        {
            var settings = SettingsManager.GetGameSettings();
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = 0;
            var pet = Controller.GetStableForUser(User).First();
            pet.Hunger = 0;
            var response = View.FeedPet(User, 1);
            Assert.AreEqual(0, pet.Hunger);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(settings.PetFeedingCost.ToString())));
        }

        [TestMethod]
        public void FeedPetReturnsErrorOnFullPet()
        {
            var settings = SettingsManager.GetGameSettings();
            var player = PlayerController.GetPlayerByUser(User);
            player.Currency = settings.PetFeedingCost;
            var pet = Controller.GetStableForUser(User).First();
            pet.Hunger = settings.PetHungerMax;
            pet.Experience = settings.PetExperienceToLevel - 1;
            var response = View.FeedPet(User, 1);
            Assert.AreEqual(settings.PetHungerMax, pet.Hunger);
            Assert.AreEqual(settings.PetFeedingCost, player.Currency);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Name) && x.Contains("is full")));
        }

        [TestMethod]
        public void FeedPetReturnsErrorOnInvalidIndex()
        {
            var response = View.FeedPet(User, 0);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
            response = View.FeedPet(User, Controller.GetStableForUser(User).Count() + 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
        }

        [TestMethod]
        public void FeedPetReturnsEmptyStableMessage()
        {
            ClearPets();
            var response = View.FeedPet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("no pets")));
        }

        [TestMethod]
        public void ActivatePetSetsActivePet()
        {
            var stable = Controller.GetStableForUser(User);
            foreach (var pet in stable)
            {
                pet.IsActive = false;
            }
            var response = View.ActivatePet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("summoned") && x.Contains(stable.First().Name)));
            Assert.IsTrue(stable.First().IsActive);
        }

        [TestMethod]
        public void ActivatePetDismissesCurrentPet()
        {
            var stable = Controller.GetStableForUser(User);
            foreach (var pet in stable)
            {
                pet.IsActive = false;
            }
            stable.Last().IsActive = true;
            var response = View.ActivatePet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("summoned") && x.Contains(stable.First().Name)));
            Assert.IsTrue(response.Responses.Any(x => x.Contains("stable") && x.Contains(stable.Last().Name)));
            Assert.IsFalse(stable.Last().IsActive);
            Assert.IsTrue(stable.First().IsActive);
        }

        [TestMethod]
        public void ActivatePetReturnsErrorOnPetAlreadySummoned()
        {
            var stable = Controller.GetStableForUser(User);
            foreach (var pet in stable)
            {
                pet.IsActive = false;
            }
            stable.First().IsActive = true;
            var response = View.ActivatePet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("already") && x.Contains(stable.First().Name)));
            Assert.IsTrue(stable.First().IsActive);
        }

        [TestMethod]
        public void ActivatePetReturnsErrorOnInvalidIndex()
        {
            var response = View.ActivatePet(User, 0);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
            response = View.ActivatePet(User, Controller.GetStableForUser(User).Count() + 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
        }

        [TestMethod]
        public void ActivatePetReturnsEmptyStableMessage()
        {
            ClearPets();
            var response = View.ActivatePet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("no pets")));
        }

        [TestMethod]
        public void DeactivatePetDismissesCurrentPet()
        {
            var stable = Controller.GetStableForUser(User);
            foreach (var pet in stable)
            {
                pet.IsActive = false;
            }
            stable.First().IsActive = true;
            var response = View.DeactivatePet(User);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("dismiss") && x.Contains(stable.First().Name)));
            Assert.IsFalse(stable.First().IsActive);
        }

        [TestMethod]
        public void DeactivatePetReturnsErrorOnPetNotSummoned()
        {
            var stable = Controller.GetStableForUser(User);
            foreach (var pet in stable)
            {
                pet.IsActive = false;
            }
            var response = View.DeactivatePet(User);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("do not")));
        }

        [TestMethod]
        public void DeletePetFlagsPetForRelease()
        {
            var stable = Controller.GetStableForUser(User);
            var response = View.DeletePet(User, 1);
            var toDelete = Controller.IsFlaggedForDelete(User);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("gone forever") && x.Contains(stable.First().Name)));
            Assert.AreEqual(toDelete, stable.First());
        }

        [TestMethod]
        public void DeletePetReturnsErrorOnAlreadyFlagged()
        {
            var stable = Controller.GetStableForUser(User);
            Controller.FlagForDelete(User, stable.First());
            var response = View.DeletePet(User, 1);
            var toDelete = Controller.IsFlaggedForDelete(User);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("already") && x.Contains(stable.First().Name)));
            Assert.AreEqual(toDelete, stable.First());
        }

        [TestMethod]
        public void DeletePetReturnsErrorOnInvalidIndex()
        {
            var response = View.DeletePet(User, 0);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
            response = View.DeletePet(User, Controller.GetStableForUser(User).Count() + 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index")));
        }

        [TestMethod]
        public void DeletePetReturnsEmptyStableMessage()
        {
            ClearPets();
            var response = View.DeletePet(User, 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("no pets")));
        }

        [TestMethod]
        public void PetDeathEventSendsPetDeathMessage()
        {
            var stable = Controller.GetStableForUser(User);
            var pet = stable.First();
            pet.Hunger = 0;
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            Controller.AddHunger(PlayerController.GetPlayerByUser(User), pet);
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains(pet.Name) && z.Contains("starved")))));
        }

        [TestMethod]
        public void PetWarningEventSendsPetWarningMessage()
        {
            var stable = Controller.GetStableForUser(User);
            var pet = stable.First();
            pet.Hunger = (int)Math.Floor(SettingsManager.GetGameSettings().PetHungerMax * 0.25 - 1);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            Controller.AddHunger(PlayerController.GetPlayerByUser(User), pet);
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains(pet.Name) && z.Contains("hungry")))));
        }

        [TestMethod]
        public void PetFoundEventSendsPetFoundMessage()
        {
            var pet = Controller.GetStableForUser(User).First().Pet;
            ClearPets();
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            Controller.GrantPet(User, pet.Rarity);
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains(pet.Name) && z.Contains("found")))));
        }

        [TestMethod]
        public void ConfirmEventReleasesPendingPet()
        {
            var pet = Controller.GetStableForUser(User).First();
            Controller.FlagForDelete(User, pet);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            ConfirmationController.Confirm(User);
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains(pet.Name) && z.Contains("released")))));
            var remaining = Controller.GetStableForUser(User);
            Assert.IsFalse(remaining.Any(x => x.Equals(pet)));
        }

        [TestMethod]
        public void CancelEventCancelsPendingRelease()
        {
            var pet = Controller.GetStableForUser(User).First();
            Controller.FlagForDelete(User, pet);
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            ConfirmationController.Cancel(User);
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
            listener.Verify(x => x(User, It.Is<CommandResult>(y => y.Responses.Any(z => z.Contains(pet.Name) && z.Contains("keep")))));
            var remaining = Controller.GetStableForUser(User);
            Assert.IsTrue(remaining.Any(x => x.Equals(pet)));
        }
    }
}
