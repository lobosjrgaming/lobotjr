using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Equipment;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Equipment
{
    /// <summary>
    /// View containing commands for debugging player inventories and fixing
    /// inventories in invalid states.
    /// </summary>
    public class EquipmentAdmin : ICommandView
    {
        private readonly EquipmentController EquipmentController;
        private readonly UserController UserController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Equipment.Admin";
        /// <summary>
        /// A collection of commands for managing player experience.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public EquipmentAdmin(EquipmentController equipmentController, UserController userController)
        {
            EquipmentController = equipmentController;
            UserController = userController;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("ClearItems", this, CommandMethod.GetInfo<string>(ClearItems), "clearitems"),
                new CommandHandler("GiveItem", this, CommandMethod.GetInfo<string, string>(GiveItem), "giveitem"),
                new CommandHandler("FixInventory", this, CommandMethod.GetInfo(FixInventory), "fixinventory")
            };
        }

        private CommandResult CreateDefaultResult(string user)
        {
            return new CommandResult($"Unable to find player record for user {user}.");
        }

        public CommandResult ClearItems(string target)
        {
            var user = UserController.GetUserByName(target);
            if (user != null)
            {
                var inventory = EquipmentController.GetInventoryByUser(user);
                var count = inventory.Count();
                foreach (var item in inventory)
                {
                    EquipmentController.RemoveInventoryRecord(item);
                }
                return new CommandResult($"Removed {count} item(s) from {user.Username}'s inventory.");
            }
            return CreateDefaultResult(target);
        }

        public CommandResult GiveItem(string target, string item)
        {
            var user = UserController.GetUserByName(target);
            if (user != null)
            {
                Item itemObject;
                if (int.TryParse(item, out var itemId))
                {
                    itemObject = EquipmentController.GetItemById(itemId);
                }
                else
                {
                    itemObject = EquipmentController.GetItemByName(item);
                }
                if (itemObject != null)
                {
                    var record = EquipmentController.AddInventoryRecord(user, itemObject);
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

        public CommandResult FixInventory()
        {
            var dupes = EquipmentController.RemoveDuplicates();
            var overages = EquipmentController.FixCountErrors();
            var equipDupes = EquipmentController.UnequipDuplicates();
            if (dupes.Any() || overages.Any() || equipDupes.Any())
            {
                return new CommandResult($"Removed {dupes.Count()} duplicate entries, reduced count to max for {overages.Count()}, and unequipped {equipDupes.Count()} invalid equipped items.");
            }
            return new CommandResult("No duplicates, overages, or extra equipped items to fix!");
        }
    }
}
