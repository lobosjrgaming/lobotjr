using Autofac;
using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.View.Equipment;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Views.Equipment
{
    [TestClass]
    public class EquipmentAdminTests
    {
        private EquipmentController Controller;
        private EquipmentAdmin View;
        private User User;

        [TestInitialize]
        public void Initialize()
        {
            Controller = AutofacMockSetup.Container.Resolve<EquipmentController>();
            View = AutofacMockSetup.Container.Resolve<EquipmentAdmin>();
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
        public void ClearItemsRemovesAllItemsFromPlayer()
        {
            var oldCount = Controller.GetInventoryByUser(User).Count();
            var response = View.ClearItems(User.Username);
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
            var inventory = Controller.GetInventoryByUser(User);
            Assert.AreEqual(0, inventory.Count());
            Assert.IsTrue(response.Responses.Any(x => x.Contains(User.Username) && x.Contains(oldCount.ToString())));
        }

        [TestMethod]
        public void ClearItemsReturnsErrorOnPlayerNotFound()
        {
            var username = "InvalidUserName";
            var response = View.ClearItems(username);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(username) && x.Contains("Unable to find")));
        }

        [TestMethod]
        public void GiveItemGivesItemToPlayer()
        {
            var item = Controller.GetInventoryByUser(User).First().Item;
            ClearInventory();
            var response = View.GiveItem(User.Username, item.Name);
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
            var inventory = Controller.GetInventoryByUser(User);
            Assert.AreEqual(1, inventory.Count());
            Assert.AreEqual(item, inventory.First().Item);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(User.Username) && x.Contains(item.Name)));
        }

        [TestMethod]
        public void GiveItemReturnsErrorIfPlayerHasMaxOfThatItem()
        {
            var item = Controller.GetInventoryByUser(User).First();
            var oldCount = Controller.GetInventoryByUser(User).Count();
            var response = View.GiveItem(User.Username, item.Item.Name);
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
            var inventory = Controller.GetInventoryByUser(User);
            Assert.AreEqual(oldCount, inventory.Count());
            Assert.IsTrue(response.Responses.Any(x => x.Contains(User.Username) && x.Contains(item.Item.Name) && x.Contains("already has")));
        }

        [TestMethod]
        public void GiveItemReturnsErrorOnInvalidItemId()
        {
            var itemName = "InvalidItem";
            var response = View.GiveItem(User.Username, itemName);
            Assert.IsTrue(response.Responses.Any(x => x.Contains(itemName) && x.Contains("Unable to find")));
        }

        [TestMethod]
        public void GiveItemReturnsErrorOnPlayerNotFound()
        {
            var username = "InvalidUserName";
            var response = View.GiveItem(username, "1");
            Assert.IsTrue(response.Responses.Any(x => x.Contains(username) && x.Contains("Unable to find")));
        }

        [TestMethod]
        public void FixInventoryRemovesDuplicatesAndOverages()
        {
            var inventory = Controller.GetInventoryByUser(User);
            var item = inventory.First();
            AutofacMockSetup.ConnectionManager.CurrentConnection.Inventories.Create(new Inventory()
            {
                UserId = User.TwitchId,
                Item = item.Item,
                IsEquipped = true,
            });
            foreach (var record in inventory)
            {
                record.IsEquipped = true;
            }
            AutofacMockSetup.ConnectionManager.CurrentConnection.Commit();
            inventory = Controller.GetInventoryByUser(User).ToList();
            var response = View.FixInventory().Responses.First();
            Assert.IsTrue(response.Contains("1 duplicate"));
            Assert.IsTrue(response.Contains("0"));
            Assert.IsTrue(response.Contains("6 invalid equipped"));
        }
    }
}
