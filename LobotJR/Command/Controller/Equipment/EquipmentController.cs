using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Controller.Equipment
{
    /// <summary>
    /// Runs the logic for the equipment controller.
    /// </summary>
    public class EquipmentController
    {
        private readonly IConnectionManager ConnectionManager;

        public EquipmentController(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        private IEnumerable<Inventory> GetInventoryByUserId(string userId)
        {
            return ConnectionManager.CurrentConnection.Inventories.Read(x => x.UserId.Equals(userId)).OrderBy(x => x.Id);
        }

        /// <summary>
        /// Gets the full inventory for a given user, sorted by the order the
        /// items were acquired.
        /// </summary>
        /// <param name="user">A user object.</param>
        /// <returns>A collection of inventory records for the user.</returns>
        public IEnumerable<Inventory> GetInventoryByUser(User user)
        {
            return GetInventoryByUserId(user.TwitchId);
        }

        /// <summary>
        /// Gets the full inventory for a given player, sorted by the order the
        /// items were acquired.
        /// </summary>
        /// <param name="user">A player object.</param>
        /// <returns>A collection of inventory records for the user.</returns>
        public IEnumerable<Inventory> GetInventoryByPlayer(PlayerCharacter player)
        {
            return GetInventoryByUserId(player.UserId);
        }

        /// <summary>
        /// Gets the inventory records for items equipped by a user.
        /// </summary>
        /// <param name="user">A user object.</param>
        /// <returns>A collection of inventory records for items the user has
        /// equipped.</returns>
        public IEnumerable<Item> GetEquippedGear(PlayerCharacter player)
        {
            return ConnectionManager.CurrentConnection.Inventories.Read(x => x.UserId.Equals(player.UserId) && x.IsEquipped).Select(x => x.Item);
        }

        /// <summary>
        /// Gets the inventory record for a given user and item.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="item">The item to check for.</param>
        /// <returns>The inventory record for this user and item, or null if
        /// the user doesn't have the item in their inventory.</returns>
        public Inventory GetInventoryRecord(User user, Item item)
        {
            return ConnectionManager.CurrentConnection.Inventories.FirstOrDefault(x => x.UserId.Equals(user.TwitchId) && x.ItemId == item.Id);
        }

        /// <summary>
        /// Creates an inventory record, adding the item to a user's inventory.
        /// If the user already has this item in their inventory and the item
        /// does not allow duplicates, no record will be created.
        /// </summary>
        /// <param name="user">The user to add the item to.</param>
        /// <param name="item">The item to add.</param>
        /// <returns>The inventory record that was created, or null if the user
        /// cannot receive the item.</returns>
        public Inventory AddInventoryRecord(User user, Item item)
        {
            var existing = GetInventoryRecord(user, item);
            if (existing != null)
            {
                if (existing.Count < item.Max)
                {
                    existing.Count++;
                    existing.TimeAdded = DateTime.Now;
                    return existing;
                }
                return null;
            }
            else
            {
                var inventory = new Inventory()
                {
                    UserId = user.TwitchId,
                    Item = item,
                    TimeAdded = DateTime.Now,
                    IsEquipped = false
                };
                ConnectionManager.CurrentConnection.Inventories.Create(inventory);
                return inventory;
            }
        }

        /// <summary>
        /// Removes an inventory record, deleting the item from a user's
        /// inventory.
        /// </summary>
        /// <param name="inventory">The inventory record to remove.</param>
        public void RemoveInventoryRecord(Inventory inventory)
        {
            ConnectionManager.CurrentConnection.Inventories.DeleteById(inventory.Id);
        }

        /// <summary>
        /// Deletes all duplicate inventory entries.
        /// </summary>
        /// <returns>The deleted records.</returns>
        public IEnumerable<Inventory> RemoveDuplicates()
        {
            var dupes = ConnectionManager.CurrentConnection.Inventories.Read()
                .GroupBy(x => $"{x.UserId}|{x.ItemId}")
                .Where(x => x.Count() > 1);
            var toDelete = dupes.SelectMany(x => x.Skip(1)).ToList();
            ConnectionManager.CurrentConnection.Inventories.DeleteRange(toDelete);
            ConnectionManager.CurrentConnection.Commit();
            return toDelete;
        }

        /// <summary>
        /// Unequips any items equipped beyond the max allowable for that slot.
        /// </summary>
        /// <returns>The records that were unequipped.</returns>
        public IEnumerable<Inventory> UnequipDuplicates()
        {
            var dupes = ConnectionManager.CurrentConnection.Inventories.Read()
                .Where(x => x.IsEquipped)
                .GroupBy(x => $"{x.UserId}|{x.Item.SlotId}")
                .Where(x => x.Count() > x.First().Item.Slot.MaxEquipped);
            var toUnequip = dupes.SelectMany(x => x.Skip(x.First().Item.Slot.MaxEquipped)).ToList();
            foreach (var record in toUnequip)
            {
                record.IsEquipped = false;
            }
            return toUnequip;
        }

        /// <summary>
        /// Sets the count to the item's max value for all inventory entries
        /// where the count is greater than the item's max.
        /// </summary>
        /// <returns>The records that were updated.</returns>
        public IEnumerable<Inventory> FixCountErrors()
        {
            var errors = ConnectionManager.CurrentConnection.Inventories.Read(x => x.Count > x.Item.Max).ToList();
            foreach (var error in errors)
            {
                error.Count = error.Item.Max;
            }
            return errors;
        }

        /// <summary>
        /// Gets the data for an item with a specified id.
        /// </summary>
        /// <param name="id">The id of the item to retrieve.</param>
        /// <returns>The item object with the specified id, or null if no such
        /// item exists.</returns>
        public Item GetItemById(int id)
        {
            return ConnectionManager.CurrentConnection.ItemData.FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        /// Gets the data for an item with as specified name.
        /// </summary>
        /// <param name="name">The name of the item to retrieve.</param>
        /// <returns>The item object with the specified name, or null if no
        /// such item exists.</returns>
        public Item GetItemByName(string name)
        {
            return ConnectionManager.CurrentConnection.ItemData.FirstOrDefault(x => x.Name.Equals(name));
        }
    }
}
