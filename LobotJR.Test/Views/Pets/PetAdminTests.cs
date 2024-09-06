using Autofac;
using LobotJR.Command.View.Pets;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Pets
{
    [TestClass]
    public class PetAdminTests
    {
        private PetAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<PetAdmin>();
        }

        [TestMethod]
        public void CheckPetsGetsPetsForUser() { }

        [TestMethod]
        public void CheckPetsReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void GrantPetGrantsPetToUser() { }

        [TestMethod]
        public void GrantPetGrantsPetOfSpecificRarity() { }

        [TestMethod]
        public void GrantPetReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void ClearPetsDeletesAllPetsForUser() { }

        [TestMethod]
        public void ClearPetsReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void SetHungerSetsPetHunger() { }

        [TestMethod]
        public void SetHungerReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void SetHungerReturnsErrorOnUserNotFound() { }
    }
}
