using Autofac;
using LobotJR.Command.View.Pets;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Pets
{
    [TestClass]
    public class PetViewTests
    {
        private PetView View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<PetView>();
        }

        [TestMethod]
        public void ListPetsReturnsAllPets() { }

        [TestMethod]
        public void ListPetsReturnsEmptyStableMessage() { }

        [TestMethod]
        public void DetailPetDescribesPetAtIndex() { }

        [TestMethod]
        public void DetailPetReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void DetailPetReturnsEmptyStableMessage() { }

        [TestMethod]
        public void RenamePetUpdatesPetName() { }

        [TestMethod]
        public void RenamePetErrorsOnTooLongPetName() { }

        [TestMethod]
        public void RenamePetReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void RenamePetReturnsEmptyStableMessage() { }

        [TestMethod]
        public void FeedPetResetsPetHunger() { }

        [TestMethod]
        public void FeedPetLevelsUpPet() { }

        [TestMethod]
        public void FeedPetReturnsErrorOnNotEnoughCoins() { }

        [TestMethod]
        public void FeedPetReturnsErrorOnFullPet() { }

        [TestMethod]
        public void FeedPetReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void FeedPetReturnsEmptyStableMessage() { }

        [TestMethod]
        public void ActivatePetSetsActivePet() { }

        [TestMethod]
        public void ActivatePetDismissesCurrentPet() { }

        [TestMethod]
        public void ActivatePetReturnsErrorOnPetAlreadySummoned() { }

        [TestMethod]
        public void ActivatePetReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void ActivatePetReturnsEmptyStableMessage() { }

        [TestMethod]
        public void DeactivatePetDismissesCurrentPet() { }

        [TestMethod]
        public void DeactivatePetReturnsErrorOnPetNotSummoned() { }

        [TestMethod]
        public void DeletePetFlagsPetForRelease() { }

        [TestMethod]
        public void DeletePetReturnsErrorOnAlreadyFlagged() { }

        [TestMethod]
        public void DeletePetReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void DeletePetReturnsEmptyStableMessage() { }

        [TestMethod]
        public void PetDeathEventSendsPetDeathMessage() { }

        [TestMethod]
        public void PetWarningEventSendsPetWarningMessage() { }

        [TestMethod]
        public void PetFoundEventSendsPetFoundMessage() { }

        [TestMethod]
        public void ConfirmEventReleasesPendingPet() { }

        [TestMethod]
        public void CancelEventCancelsPendingRelease() { }
    }
}
