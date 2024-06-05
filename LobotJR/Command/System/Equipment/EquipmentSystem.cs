using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.System.Equipment
{
    /// <summary>
    /// Runs the logic for the equipment system.
    /// </summary>
    public class EquipmentSystem : ISystem
    {
        private readonly IConnectionManager ConnectionManager;

        public EquipmentSystem(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        /// <summary>
        /// Gets the full inventory for a given user, sorted by the order the
        /// items were acquired.
        /// </summary>
        /// <param name="user">A user object.</param>
        /// <returns>A collection of inventory records for the user.</returns>
        public IEnumerable<Inventory> GetInventoryByUser(User user)
        {
            return ConnectionManager.CurrentConnection.Inventories.Read(x => x.UserId.Equals(user.TwitchId)).OrderBy(x => x.Id);
        }

        /// <summary>
        /// Gets the inventory records for items equipped by a user.
        /// </summary>
        /// <param name="user">A user object.</param>
        /// <returns>A collection of inventory records for items the user has
        /// equipped.</returns>
        public IEnumerable<Item> GetEquippedGear(User user)
        {
            return ConnectionManager.CurrentConnection.Inventories.Read(x => x.UserId.Equals(user.TwitchId) && x.IsEquipped).Select(x => x.Item);
        }

        /// <summary>
        /// Checks whether a user already has an item.
        /// </summary>
        /// <param name="user">The user of the inventory to check.</param>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the user has the item in their inventory.</returns>
        public bool HasItem(User user, Item item)
        {
            return ConnectionManager.CurrentConnection.Inventories.Any(x => x.UserId.Equals(user.TwitchId) && x.Item.Equals(item));
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
            return ConnectionManager.CurrentConnection.Inventories.FirstOrDefault(x => x.UserId.Equals(user.TwitchId) && x.Item.Equals(item));
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
            var toDelete = dupes.SelectMany(x => x.Skip(1));
            ConnectionManager.CurrentConnection.Inventories.DeleteRange(toDelete);
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
            var toUnequip = dupes.SelectMany(x => x.Skip(x.First().Item.Slot.MaxEquipped));
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
            var errors = ConnectionManager.CurrentConnection.Inventories.Read(x => x.Count > x.Item.Max);
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

        /// <summary>
        /// This system does not have any per-frame logic.
        /// </summary>
        /// <returns>A completed task.</returns>
        public Task Process()
        {
            return Task.CompletedTask;
        }
    }
}
