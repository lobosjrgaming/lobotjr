using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Model.Equipment;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Equipment
{
    /// <summary>
    /// View containing commands for retrieving player inventory and managing
    /// equipped items.
    /// </summary>
    public class EquipmentView : ICommandView
    {
        private readonly EquipmentController EquipmentController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Equipment";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public EquipmentView(EquipmentController equipmentController)
        {
            EquipmentController = equipmentController;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("Inventory", this, CommandMethod.GetInfo(GetInventory), CommandMethod.GetInfo(GetInventoryCompact), "inventory", "inv"),
                new CommandHandler("DescribeItem", this, CommandMethod.GetInfo<int>(DescribeItem), "item"),
                new CommandHandler("EquipItem", this, CommandMethod.GetInfo<int>(EquipItem), "activate", "equip"),
                new CommandHandler("UnequipItem", this, CommandMethod.GetInfo<int>(UnequipItem), "deactivate", "unequip"),
            };
        }

        private string PrintPercent(float value)
        {
            return $"{(int)Math.Round(value * 100)}%";
        }

        private IEnumerable<string> DescribeItem(Item item, int index, bool isEquipped)
        {
            var equipString = isEquipped ? "Equipped" : "Unequipped";
            var output = new List<string>() { $"{index}: {item.Name} ({item.Quality.Name + item.Slot.Name}) ({equipString})" };
            if (item.SuccessChance > 0)
            {
                output.Add($"+{PrintPercent(item.SuccessChance)}% Success Chance");
            }
            if (item.XpBonus > 0)
            {
                output.Add($"+{PrintPercent(item.XpBonus)}% XP Bonus");
            }
            if (item.CoinBonus > 0)
            {
                output.Add($"+{PrintPercent(item.CoinBonus)}% Wolfcoin Bonus");
            }
            if (item.ItemFind > 0)
            {
                output.Add($"+{PrintPercent(item.ItemFind)}% Item Find");
            }
            if (item.PreventDeathBonus > 0)
            {
                output.Add($"+{PrintPercent(item.PreventDeathBonus)}% to Prevent Death");
            }
            return output;
        }

        public CompactCollection<Inventory> GetInventoryCompact(User user)
        {
            var inventory = EquipmentController.GetInventoryByUser(user);
            return new CompactCollection<Inventory>(inventory, x => $"{x.Item.Name}|{x.Item.Description}|{(x.IsEquipped ? "E" : "U")}|{PrintPercent(x.Item.SuccessChance)}|{PrintPercent(x.Item.XpBonus)}|{PrintPercent(x.Item.CoinBonus)}|{PrintPercent(x.Item.ItemFind)}|{PrintPercent(x.Item.PreventDeathBonus)};");
        }

        public CommandResult GetInventory(User user)
        {
            var inventory = EquipmentController.GetInventoryByUser(user);
            if (inventory.Any())
            {
                var responses = new List<string>() { $"You have {inventory.Count()} items: " };
                var index = 1;
                foreach (var item in inventory)
                {
                    responses.AddRange(DescribeItem(item.Item, index++, item.IsEquipped));
                }
                return new CommandResult(responses.ToArray());
            }
            return new CommandResult("You have no items.");
        }

        public CommandResult DescribeItem(User user, int index)
        {
            var inventory = EquipmentController.GetInventoryByUser(user);
            if (inventory.Any())
            {
                var item = EquipmentController.GetInventoryByUser(user).ElementAtOrDefault(index - 1);
                if (item != null)
                {
                    return new CommandResult($"{item.Item.Name} -- {item.Item.Description}");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {inventory.Count()}.");
            }
            return new CommandResult("You have no items.");
        }

        public CommandResult EquipItem(User user, int index)
        {
            var inventory = EquipmentController.GetInventoryByUser(user);
            if (inventory.Any())
            {
                var toEquip = inventory.ElementAtOrDefault(index - 1);
                if (toEquip != null)
                {
                    if (toEquip.Item.Slot.MaxEquipped == 0)
                    {
                        return new CommandResult($"{toEquip.Item.Name} cannot be equipped.");
                    }
                    else if (toEquip.Item.Slot.MaxEquipped == 1)
                    {
                        var toUnequip = inventory.FirstOrDefault(x => x.IsEquipped && x.Item.Slot.Equals(toEquip.Item.Slot));
                        var unequipString = "";
                        if (toUnequip != null)
                        {
                            if (toUnequip.Equals(toEquip))
                            {
                                return new CommandResult($"{toEquip.Item.Name} is already equipped.");
                            }
                            toUnequip.IsEquipped = false;
                            unequipString = $", and unequipped {toUnequip.Item.Name}";
                        }
                        toEquip.IsEquipped = true;
                        return new CommandResult($"Equipped {toEquip.Item.Name}{unequipString}.");
                    }
                    else
                    {
                        var equipped = inventory.Where(x => x.IsEquipped && x.Item.Slot.Equals(toEquip.Item.SlotId));
                        if (equipped.Count() < toEquip.Item.Slot.MaxEquipped)
                        {
                            toEquip.IsEquipped = true;
                            return new CommandResult($"Equipped {toEquip.Item.Name}.");
                        }
                        else
                        {
                            if (equipped.Any(x => x.Item.Equals(toEquip.Item)))
                            {
                                return new CommandResult($"{toEquip.Item.Name} is already equipped.");
                            }
                            return new CommandResult($"You already have {equipped.Count()} {toEquip.Item.Slot.Name}s equipped. You must unequip one before you can equip {toEquip.Item.Name}");
                        }
                    }
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {inventory.Count()}.");
            }
            return new CommandResult("You have no items.");
        }

        public CommandResult UnequipItem(User user, int index)
        {
            var inventory = EquipmentController.GetInventoryByUser(user);
            if (inventory.Any())
            {
                var toEquip = inventory.ElementAtOrDefault(index - 1);
                if (toEquip != null)
                {
                    if (toEquip.IsEquipped)
                    {
                        toEquip.IsEquipped = false;
                        return new CommandResult($"Unequipped {toEquip.Item.Name}.");
                    }
                    return new CommandResult($"{toEquip.Item.Name} is not equipped.");
                }
                return new CommandResult($"Invalid index, please specify a number between 1 and {inventory.Count()}.");
            }
            return new CommandResult("You have no items.");
        }
    }
}
