using Autofac;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Controllers.Equipment
{
    [TestClass]
    public class EquipmentControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            AutofacMockSetup.ResetPlayers();
        }

        [TestMethod]
        public void GetsUserInventory() { }

        [TestMethod]
        public void GetsPlayerInventory() { }

        [TestMethod]
        public void GetsEquippedGear() { }

        [TestMethod]
        public void HasItemReturnsTrueForOwnedItems() { }

        [TestMethod]
        public void HasItemReturnsFalseForUnownedItems() { }

        [TestMethod]
        public void GetInventoryRecordGetsSpecificItem() { }

        [TestMethod]
        public void GetInventoryRecordReturnsNullForUnownedItems() { }

        [TestMethod]
        public void AddInventoryRecordAddsNewItems() { }

        [TestMethod]
        public void AddInventoryRecordIncrementsOwnedItems() { }

        [TestMethod]
        public void AddInventoryRecordCapsQuantityAtItemMax() { }

        [TestMethod]
        public void RemoveInventoryRecordRemovesItems() { }

        [TestMethod]
        public void RemoveDuplicatesRemovesAllDuplicateItemEntries() { }

        [TestMethod]
        public void UnequipDuplicatesUnequipsItemsOverSlotMax() { }

        [TestMethod]
        public void UnequipDuplicatesDoesNotUnequipItemsUnderSlotMax() { }

        [TestMethod]
        public void FixCountErrorsSetsQuantityToMaxIfOver() { }

        [TestMethod]
        public void GetItemByItemGetsItemData() { }

        [TestMethod]
        public void GetItemByNameGetsItemData() { }
    }
}
