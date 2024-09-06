using Autofac;
using LobotJR.Command.View.Equipment;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Equipment
{
    [TestClass]
    public class EquipmentViewTests
    {
        private EquipmentView View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<EquipmentView>();
        }

        [TestMethod]
        public void GetInventoryGetsUserItems() { }

        [TestMethod]
        public void GetInventoryCompactGetsUserItems() { }

        [TestMethod]
        public void GetInventoryReturnsErrorOnEmptyInventory() { }

        [TestMethod]
        public void DescribeItemGetsItemDetails() { }

        [TestMethod]
        public void DescribeItemReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void DescribeItemReturnsErrorOnEmptyInventory() { }

        [TestMethod]
        public void EquipItemEquipsItem() { }

        [TestMethod]
        public void EquipItemUnequipsItemsInSameSlot() { }

        [TestMethod]
        public void EquipItemReturnsErrorOnItemAlreadyEquipped() { }

        [TestMethod]
        public void EquipItemAllowsMultipleEquipsIfSlotMaxAllows() { }

        [TestMethod]
        public void EquipItemReturnsErrorOnMultipleEquipAtSlotMax() { }

        [TestMethod]
        public void EquipItemReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void EquipItemReturnsErrorOnEmptyInventory() { }

        [TestMethod]
        public void UnequipItemRemovesItemEquippedFlag() { }

        [TestMethod]
        public void UnequipItemReturnsErrorOnItemNotEquipped() { }

        [TestMethod]
        public void UnequipItemReturnsErrorOnInvalidIndex() { }

        [TestMethod]
        public void UnequipItemReturnsErrorOnEmptyInventory() { }
    }
}
