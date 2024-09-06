using Autofac;
using LobotJR.Command.View.Equipment;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Equipment
{
    [TestClass]
    public class EquipmentAdminTests
    {
        private EquipmentAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<EquipmentAdmin>();
        }

        [TestMethod]
        public void ClearItemsRemovesAllItemsFromPlayer() { }

        [TestMethod]
        public void ClearItemsReturnsErrorOnPlayerNotFound() { }

        [TestMethod]
        public void GiveItemGivesItemToPlayer() { }

        [TestMethod]
        public void GiveItemReturnsErrorIfPlayerHasMaxOfThatItem() { }

        [TestMethod]
        public void GiveItemReturnsErrorOnInvalidItemId() { }

        [TestMethod]
        public void GiveItemReturnsErrorOnPlayerNotFound() { }

        [TestMethod]
        public void FixInventoryRemovesDuplicatesAndOverages() { }
    }
}
