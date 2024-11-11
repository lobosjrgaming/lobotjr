using Autofac;
using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Controllers.Equipment
{
    [TestClass]
    public class EquipmentControllerTests
    {
        private IConnectionManager ConnectionManager;
        private EquipmentController Controller;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            Controller = AutofacMockSetup.Container.Resolve<EquipmentController>();
            AutofacMockSetup.ResetPlayers();
        }

        [TestMethod]
        public void GetsUserInventory()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var item = db.ItemData.Read().First();
            db.Inventories.Create(new Inventory()
            {
                Item = item,
                UserId = user.TwitchId
            });
            db.Commit();
            var inventory = Controller.GetInventoryByUser(user);
            Assert.AreEqual(1, inventory.Count());
            Assert.IsTrue(inventory.Any(x => x.Item.Equals(item)));
        }

        [TestMethod]
        public void GetsPlayerInventory()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var item = db.ItemData.Read().First();
            db.Inventories.Create(new Inventory()
            {
                Item = item,
                UserId = player.UserId
            });
            db.Commit();
            var inventory = Controller.GetInventoryByPlayer(player);
            Assert.AreEqual(1, inventory.Count());
            Assert.IsTrue(inventory.Any(x => x.Item.Equals(item)));
        }

        [TestMethod]
        public void GetsEquippedGear()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var items = db.ItemData.Read();
            db.Inventories.Create(new Inventory()
            {
                Item = items.First(),
                UserId = player.UserId
            });
            db.Inventories.Create(new Inventory()
            {
                Item = items.ElementAt(1),
                UserId = player.UserId,
                IsEquipped = true
            });
            db.Commit();
            var inventory = Controller.GetEquippedGear(player);
            Assert.AreEqual(1, inventory.Count());
            Assert.IsTrue(inventory.Any(x => x.Equals(items.ElementAt(1))));
        }

        [TestMethod]
        public void GetInventoryRecordGetsSpecificItem()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read();
            var inventory = new Inventory()
            {
                Item = items.First(),
                UserId = user.TwitchId
            };
            db.Inventories.Create(inventory);
            db.Commit();
            var record = Controller.GetInventoryRecord(user, items.First());
            Assert.AreEqual(inventory, record);
        }

        [TestMethod]
        public void GetInventoryRecordReturnsNullForUnownedItems()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read();
            var inventory = new Inventory()
            {
                Item = items.First(),
                UserId = user.TwitchId
            };
            db.Inventories.Create(inventory);
            db.Commit();
            var record = Controller.GetInventoryRecord(user, items.ElementAt(1));
            Assert.IsNull(record);
        }

        [TestMethod]
        public void AddInventoryRecordAddsNewItems()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read();
            var record = Controller.AddInventoryRecord(user, items.First());
            Assert.IsNotNull(record);
            Assert.AreEqual(items.First(), record.Item);
            Assert.AreEqual(user.TwitchId, record.UserId);
        }

        [TestMethod]
        public void AddInventoryRecordIncrementsOwnedItems()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read(x => x.Name.Equals("Potion"));
            Controller.AddInventoryRecord(user, items.First());
            db.Commit();
            var record = Controller.AddInventoryRecord(user, items.First());
            Assert.IsNotNull(record);
            Assert.AreEqual(items.First(), record.Item);
            Assert.AreEqual(2, record.Count);
            Assert.AreEqual(user.TwitchId, record.UserId);
        }

        [TestMethod]
        public void AddInventoryRecordCapsQuantityAtItemMax()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read(x => x.Name.Equals("Potion"));
            var record = Controller.AddInventoryRecord(user, items.First());
            db.Commit();
            record.Count = record.Item.Max;
            var result = Controller.AddInventoryRecord(user, items.First());
            Assert.IsNull(result);
            Assert.AreEqual(record.Item.Max, record.Count);
        }

        [TestMethod]
        public void RemoveInventoryRecordRemovesItems()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read();
            var record = Controller.AddInventoryRecord(user, items.First());
            Controller.RemoveInventoryRecord(record);
            db.Commit();
            var result = Controller.GetInventoryRecord(user, items.First());
            Assert.IsNull(result);
        }

        [TestMethod]
        public void RemoveDuplicatesRemovesAllDuplicateItemEntries()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read();
            db.Inventories.Create(new Inventory()
            {
                Item = items.First(),
                UserId = user.TwitchId
            });
            db.Inventories.Create(new Inventory()
            {
                Item = items.First(),
                UserId = user.TwitchId
            });
            db.Commit();
            var deleted = Controller.RemoveDuplicates();
            db.Commit();
            Assert.AreEqual(1, deleted.Count());
            var record = Controller.GetInventoryRecord(user, items.ElementAt(1));
            Assert.IsNull(record);
            Assert.AreEqual(1, db.Inventories.Read().Count());
        }

        [TestMethod]
        public void UnequipDuplicatesUnequipsItemsOverSlotMax()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var slot = db.ItemSlotData.Read().First();
            var items = db.ItemData.Read(x => x.SlotId.Equals(slot.Id));
            var inventory = new Inventory()
            {
                Item = items.First(),
                UserId = user.TwitchId,
                IsEquipped = true
            };
            var inventory2 = new Inventory()
            {
                Item = items.Last(),
                UserId = user.TwitchId,
                IsEquipped = true
            };
            db.Inventories.Create(inventory);
            db.Inventories.Create(inventory2);
            db.Commit();
            var unequipped = Controller.UnequipDuplicates();
            db.Commit();
            var player = db.PlayerCharacters.Read(x => x.UserId.Equals(user.TwitchId)).First();
            var equipped = Controller.GetEquippedGear(player).ToList();
            Assert.AreEqual(1, unequipped.Count());
            Assert.AreEqual(1, equipped.Count());
        }

        [TestMethod]
        public void UnequipDuplicatesDoesNotUnequipItemsUnderSlotMax()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var slot = db.ItemSlotData.Read().First();
            slot.MaxEquipped = 2;
            var items = db.ItemData.Read(x => x.SlotId.Equals(slot.Id));
            var inventory = new Inventory()
            {
                Item = items.First(),
                UserId = user.TwitchId,
                IsEquipped = true
            };
            var inventory2 = new Inventory()
            {
                Item = items.Last(),
                UserId = user.TwitchId,
                IsEquipped = true
            };
            db.Inventories.Create(inventory);
            db.Inventories.Create(inventory2);
            db.Commit();
            var unequipped = Controller.UnequipDuplicates();
            var equipped = Controller.GetEquippedGear(db.PlayerCharacters.Read(x => x.UserId.Equals(user.TwitchId)).First());
            slot.MaxEquipped = 1;
            Assert.AreEqual(0, unequipped.Count());
            Assert.AreEqual(2, equipped.Count());
        }


        [TestMethod]
        public void FixCountErrorsSetsQuantityToMaxIfOver()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read(x => x.Name.Equals("Potion"));
            var record = Controller.AddInventoryRecord(user, items.First());
            db.Commit();
            record.Count = record.Item.Max + 1;
            var errors = Controller.FixCountErrors();
            Assert.AreEqual(1, errors.Count());
            Assert.IsTrue(errors.Any(x => x.Item.Equals(items.First())));
            Assert.AreEqual(record.Item.Max, errors.First().Count);
        }

        [TestMethod]
        public void GetItemByIdGetsItemData()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read(x => x.Name.Equals("Potion"));
            var item = Controller.GetItemById(items.First().Id);
            Assert.AreEqual(items.First(), item);
        }

        [TestMethod]
        public void GetItemByNameGetsItemData()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var items = db.ItemData.Read(x => x.Name.Equals("Potion"));
            var item = Controller.GetItemByName(items.First().Name);
            Assert.AreEqual(items.First(), item);
        }
    }
}
