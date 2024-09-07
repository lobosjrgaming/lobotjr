using Autofac;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.View.Pets;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Views.Pets
{
    [TestClass]
    public class PetAdminTests
    {
        private IConnectionManager ConnectionManager;
        private User User;
        private User Other;
        private PetController Controller;
        private PetAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            Controller = AutofacMockSetup.Container.Resolve<PetController>();
            View = AutofacMockSetup.Container.Resolve<PetAdmin>();
            User = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().First();
            Other = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().ElementAt(1);
            AutofacMockSetup.ResetPlayers();
            Controller.ClearPendingReleases();
        }

        [TestMethod]
        public void CheckPetsGetsPetsForUser()
        {
            var stable = Controller.GetStableForUser(User);
            var response = View.CheckPets(User.Username);
            foreach (var pet in stable)
            {
                Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Name)));
            }
        }

        [TestMethod]
        public void CheckPetsReturnsErrorOnUserNotFound()
        {
            var username = "InvalidUserName";
            var response = View.CheckPets(username);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Unable to find") && x.Contains(username)));
        }

        [TestMethod]
        public void GrantPetGrantsPetToUser()
        {
            var response = View.GrantPet(Other);
            ConnectionManager.CurrentConnection.Commit();
            var stable = Controller.GetStableForUser(Other);
            Assert.AreEqual(1, stable.Count());
            Assert.IsTrue(response.Responses.Any(x => x.Contains(Other.Username) && x.Contains("granted")));
        }

        [TestMethod]
        public void GrantPetGrantsPetOfSpecificRarity()
        {
            var rarity = Controller.GetRarities().Last();
            var response = View.GrantPet(Other, 2);
            ConnectionManager.CurrentConnection.Commit();
            var stable = Controller.GetStableForUser(Other);
            Assert.AreEqual(1, stable.Count());
            Assert.IsTrue(response.Responses.Any(x => x.Contains(Other.Username) && x.Contains(rarity.Name)));
        }

        [TestMethod]
        public void GrantPetReturnsErrorOnInvalidIndex()
        {
            var response = View.GrantPet(Other, 3);
            var stable = Controller.GetStableForUser(Other);
            Assert.AreEqual(0, stable.Count());
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid rarity index")));
        }

        [TestMethod]
        public void ClearPetsDeletesAllPetsForUser()
        {
            var response = View.ClearPets(User);
            var stable = Controller.GetStableForUser(Other);
            Assert.AreEqual(0, stable.Count());
            Assert.IsTrue(response.Responses.Contains("Pets cleared."));
        }

        [TestMethod]
        public void SetHungerSetsPetHunger()
        {
            var pet = Controller.GetStableForUser(User).First();
            pet.Hunger = 0;
            var amount = 50;
            var response = View.SetHunger(User, 1, amount);
            Assert.AreEqual(amount, pet.Hunger);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(pet.Name) && x.Contains(amount.ToString())));
        }

        [TestMethod]
        public void SetHungerReturnsErrorOnInvalidIndex()
        {
            var stable = Controller.GetStableForUser(User);
            var response = View.SetHunger(User, 0, 50);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index") && x.Contains(stable.Count().ToString())));
        }

        [TestMethod]
        public void SetHungerReturnsErrorOnEmptyStable()
        {
            var response = View.SetHunger(Other, 1, 50);
            Assert.IsTrue(response.Responses.Contains("You don't have any pets."));
        }
    }
}
