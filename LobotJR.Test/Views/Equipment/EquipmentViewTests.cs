using Autofac;
using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.View.Equipment;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Views.Equipment
{
    [TestClass]
    public class EquipmentViewTests
    {
        private EquipmentController Controller;
        private EquipmentView View;
        private User User;

        [TestInitialize]
        public void Initialize()
        {
            Controller = AutofacMockSetup.Container.Resolve<EquipmentController>();
            View = AutofacMockSetup.Container.Resolve<EquipmentView>();
            User = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().First();
            AutofacMockSetup.ResetPlayers();
            var items = AutofacMockSetup.ConnectionManager.CurrentConnection.ItemData.Read().ToList();
            for (var i = 0; i < 8; i++)
            {
                var item = items[i];
                Controller.AddInventoryRecord(User, item);
            }
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
        }

        private void ClearInventory()
        {
            var inventory = Controller.GetInventoryByUser(User);
            foreach (var record in inventory)
            {
                Controller.RemoveInventoryRecord(record);
            }
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
        }

        [TestMethod]
        public void GetInventoryGetsUserItems()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.GetInventory(User);
            Assert.IsTrue(response.Responses.First().Contains(inventory.Count().ToString()));
            Assert.AreEqual(inventory.Count() * 6 + 1, response.Responses.Count());
            foreach (var item in inventory)
            {
                Assert.IsTrue(response.Responses.Any(x => x.Contains(item.Item.Name)));
            }
        }

        [TestMethod]
        public void GetInventoryCompactGetsUserItems()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.GetInventoryCompact(User);
            Assert.AreEqual(inventory.Count(), response.Items.Count());
            foreach (var item in inventory)
            {
                Assert.IsTrue(response.Items.Any(x => x.Equals(item)));
            }
        }

        [TestMethod]
        public void GetInventoryReturnsErrorOnEmptyInventory()
        {
            ClearInventory();
            var response = View.GetInventory(User);
            Assert.AreEqual("You have no items.", response.Responses.First());
        }

        [TestMethod]
        public void DescribeItemGetsItemDetails()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.DescribeItem(User, 1);
            var responseString = response.Responses.First();
            Assert.IsTrue(responseString.Contains(inventory.First().Item.Name));
            Assert.IsTrue(responseString.Contains(inventory.First().Item.Description));
        }

        [TestMethod]
        public void DescribeItemReturnsErrorOnInvalidIndex()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.DescribeItem(User, 0);
            var responseString = response.Responses.First();
            Assert.IsTrue(responseString.Contains("Invalid index") && responseString.Contains(inventory.Count().ToString()));
        }

        [TestMethod]
        public void DescribeItemReturnsErrorOnTooHighIndex()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.DescribeItem(User, inventory.Count() + 1);
            var responseString = response.Responses.First();
            Assert.IsTrue(responseString.Contains("Invalid index") && responseString.Contains(inventory.Count().ToString()));
        }

        [TestMethod]
        public void DescribeItemReturnsErrorOnEmptyInventory()
        {
            ClearInventory();
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.DescribeItem(User, inventory.Count() + 1);
            Assert.IsTrue(response.Responses.First().Contains("no items"));
        }

        [TestMethod]
        public void EquipItemEquipsItem()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.EquipItem(User, 1);
            Assert.IsTrue(response.Responses.First().Contains(inventory.First().Item.Name));
        }

        [TestMethod]
        public void EquipItemUnequipsItemsInSameSlot()
        {
            var inventory = Controller.GetInventoryByUser(User).ToList();
            var slot = inventory.First().Item.Slot;
            var sameSlot = inventory.Where(x => x.Item.Slot.Equals(slot));
            sameSlot.First().IsEquipped = true;
            var response = View.EquipItem(User, inventory.IndexOf(sameSlot.Last()) + 1);
            Assert.IsFalse(sameSlot.First().IsEquipped);
            Assert.IsTrue(response.Responses.First().Contains(sameSlot.Last().Item.Name));
            Assert.IsTrue(response.Responses.First().Contains($"unequipped {sameSlot.First().Item.Name}"));
        }

        [TestMethod]
        public void EquipItemReturnsErrorOnItemAlreadyEquipped()
        {
            var inventory = Controller.GetInventoryByUser(User).ToList();
            inventory.First().IsEquipped = true;
            var response = View.EquipItem(User, 1);
            Assert.IsTrue(response.Responses.First().Contains("already equipped"));
        }

        [TestMethod]
        public void EquipItemAllowsMultipleEquipsIfSlotMaxAllows()
        {
            var inventory = Controller.GetInventoryByUser(User).ToList();
            var slot = inventory.First().Item.Slot;
            slot.MaxEquipped = 2;
            var sameSlot = inventory.Where(x => x.Item.Slot.Equals(slot));
            sameSlot.First().IsEquipped = true;
            var response = View.EquipItem(User, inventory.IndexOf(sameSlot.Last()) + 1);
            slot.MaxEquipped = 1;
            Assert.IsTrue(sameSlot.First().IsEquipped);
            Assert.IsTrue(sameSlot.Last().IsEquipped);
            Assert.AreEqual($"Equipped {sameSlot.Last().Item.Name}.", response.Responses.First());
        }

        [TestMethod]
        public void EquipItemReturnsErrorOnMultipleEquipAtSlotMax()
        {
            var inventory = Controller.GetInventoryByUser(User).ToList();
            var slot = inventory.First().Item.Slot;
            slot.MaxEquipped = 2;
            var sameSlot = inventory.Where(x => x.Item.Slot.Equals(slot)).ToList();
            sameSlot.First().IsEquipped = true;
            sameSlot.ElementAt(1).IsEquipped = true;
            var response = View.EquipItem(User, inventory.IndexOf(sameSlot.Last()) + 1);
            Assert.IsTrue(sameSlot.First().IsEquipped);
            Assert.IsTrue(sameSlot.ElementAt(1).IsEquipped);
            Assert.IsFalse(sameSlot.Last().IsEquipped);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(slot.MaxEquipped.ToString()) && x.Contains(slot.Name)));
            slot.MaxEquipped = 1;
        }

        [TestMethod]
        public void EquipItemReturnsErrorOnInvalidIndex()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.EquipItem(User, 0);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index") && x.Contains(inventory.Count().ToString())));
        }

        [TestMethod]
        public void EquipItemReturnsErrorOnTooHighIndex()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.EquipItem(User, inventory.Count() + 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index") && x.Contains(inventory.Count().ToString())));
        }

        [TestMethod]
        public void EquipItemReturnsErrorOnEmptyInventory()
        {
            ClearInventory();
            var response = View.EquipItem(User, 0);
            Assert.AreEqual("You have no items.", response.Responses.First());
        }

        [TestMethod]
        public void UnequipItemRemovesItemEquippedFlag()
        {
            var inventory = Controller.GetInventoryByUser(User);
            inventory.First().IsEquipped = true;
            var response = View.UnequipItem(User, 1);
            Assert.IsTrue(response.Responses.First().Contains(inventory.First().Item.Name));
            Assert.IsFalse(inventory.First().IsEquipped);
        }

        [TestMethod]
        public void UnequipItemReturnsErrorOnItemNotEquipped()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.UnequipItem(User, 1);
            Assert.IsTrue(response.Responses.First().Contains(inventory.First().Item.Name));
            Assert.IsTrue(response.Responses.First().Contains("not equipped"));
            Assert.IsFalse(inventory.First().IsEquipped);
        }

        [TestMethod]
        public void UnequipItemReturnsErrorOnInvalidIndex()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.UnequipItem(User, 0);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index") && x.Contains(inventory.Count().ToString())));
        }

        [TestMethod]
        public void UnequipItemReturnsErrorOnTooHighIndex()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var response = View.UnequipItem(User, inventory.Count() + 1);
            Assert.IsTrue(response.Responses.Any(x => x.Contains("Invalid index") && x.Contains(inventory.Count().ToString())));
        }

        [TestMethod]
        public void UnequipItemReturnsErrorOnEmptyInventory()
        {
            ClearInventory();
            var response = View.UnequipItem(User, 0);
            Assert.AreEqual("You have no items.", response.Responses.First());
        }
    }
}
