using LobotJR.Command.Model.Equipment;
using LobotJR.Command.System.Equipment;
using LobotJR.Command.System.Twitch;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.Module.Equipment
{
    /// <summary>
    /// Module containing commands for debugging player inventories and fixing
    /// inventories in invalid states.
    /// </summary>
    public class EquipmentAdmin : ICommandModule
    {
        private readonly EquipmentSystem EquipmentSystem;
        private readonly UserSystem UserSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Equipment.Admin";
        /// <summary>
        /// This module does not issue any push notifications.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands for managing player experience.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public EquipmentAdmin(EquipmentSystem equipmentSystem, UserSystem userSystem)
        {
            EquipmentSystem = equipmentSystem;
            UserSystem = userSystem;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("ClearItems", this, CommandMethod.GetInfo<string>(ClearItems), "clearitems"),
                new CommandHandler("GiveItem", this, CommandMethod.GetInfo<string, string>(GiveItem), "giveitem"),
                new CommandHandler("FixInventory", this, CommandMethod.GetInfo<string>(FixInventory), "fixinventory")
            };
        }

        private CommandResult CreateDefaultResult(string user)
        {
            return new CommandResult($"Unable to find player record for user {user}.");
        }

        public CommandResult ClearItems(string target)
        {
            var user = UserSystem.GetUserByName(target);
            if (user != null)
            {
                var inventory = EquipmentSystem.GetInventoryByUser(user);
                var count = inventory.Count();
                foreach (var item in inventory)
                {
                    EquipmentSystem.RemoveInventoryRecord(item);
                }
                return new CommandResult($"Removed {count} item(s) from {user.Username}'s inventory.");
            }
            return CreateDefaultResult(target);
        }

        public CommandResult GiveItem(string target, string item)
        {
            var user = UserSystem.GetUserByName(target);
            if (user != null)
            {
                var inventory = EquipmentSystem.GetInventoryByUser(user);
                Item itemObject = null;
                if (int.TryParse(item, out var itemId))
                {
                    itemObject = EquipmentSystem.GetItemById(itemId);
                }
                else
                {
                    itemObject = EquipmentSystem.GetItemByName(item);
                }
                if (itemObject != null)
                {
                    var record = EquipmentSystem.AddInventoryRecord(user, itemObject);
                    if (record != null)
                    {
                        return new CommandResult($"Gave {user.Username} a {itemObject.Name}.");
                    }
                    if (itemObject.Max == 1)
                    {
                        return new CommandResult($"{user.Username} already has {itemObject.Name}.");
                    }
                    return new CommandResult($"{user.Username} already has the max {itemObject.Name} allowed.");
                }
                return new CommandResult($"Unable to find item {item} by id or name.");
            }
            return CreateDefaultResult(target);
        }

        public CommandResult FixInventory(string target)
        {
            var dupes = EquipmentSystem.RemoveDuplicates();
            var overages = EquipmentSystem.FixCountErrors();
            var equipDupes = EquipmentSystem.UnequipDuplicates();
            if (dupes.Any() || overages.Any() || equipDupes.Any())
            {
                return new CommandResult($"Removed {dupes.Count()} duplicate entries, reduced count to max for {overages.Count()}, and unequipped {equipDupes.Count()} invalid equipped items.");
            }
            return CreateDefaultResult(target);
        }
    }
}
