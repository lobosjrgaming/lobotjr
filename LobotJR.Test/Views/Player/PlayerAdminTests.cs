using Autofac;
using LobotJR.Command.View.Player;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Player
{
    [TestClass]
    public class PlayerAdminTests
    {
        private PlayerAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<PlayerAdmin>();
        }

        [TestMethod]
        public void GiveExperienceToUserReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void GiveExperienceToUsersGivesExperience() { }

        [TestMethod]
        public void GiveExperienceToAllGivesExperienceToViewers() { }

        [TestMethod]
        public void SetExperienceReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void SetExperienceSetsPlayerExperience() { }

        [TestMethod]
        public void SetPrestigeReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void SetPrestigeSetsPlayerPrestige() { }

        [TestMethod]
        public void GiveCoinsReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void GiveCoinsGivesCoinsToPlayer() { }

        [TestMethod]
        public void RemoveCoinsReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void RemoveCoinsRemovesCoinsFromPlayer() { }

        [TestMethod]
        public void ResetPlayerReturnsErrorOnUserNotFound() { }

        [TestMethod]
        public void ResetPlayerRemovesPlayerClass() { }

        [TestMethod]
        public void SetIntervalUpdatesAwardInterval() { }

        [TestMethod]
        public void SetMultiplierSetsAwardMultiplier() { }

        [TestMethod]
        public void EnableExperienceTurnsOnExperience() { }

        [TestMethod]
        public void EnableExperienceReturnsAwardSetterIfAlreadyEnabled() { }

        [TestMethod]
        public void DisableExperienceTurnsOffExperience() { }

        [TestMethod]
        public void DisableExperienceReturnsErrorIfExperienceNotEnabled() { }

        [TestMethod]
        public void PrintNextAwardSendsNextAwardTimeToChannel() { }

        [TestMethod]
        public void PrintNextAwardGivesErrorIfAwardsOverdue() { }

        [TestMethod]
        public void PrintNextAwardGivesErrorIfAwardsDisabled() { }
    }
}
