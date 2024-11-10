using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class ItemTable : IContentTable
    {
        public Type ContentType => typeof(Item);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.ItemData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(Item.Id), true),
                InterfaceUtils.CreateColumn(nameof(Item.Name)),
                InterfaceUtils.CreateColumn(nameof(Item.Description)),
                InterfaceUtils.CreateColumn(nameof(Item.Max)),
                InterfaceUtils.CreateColumn(nameof(Item.Type), database.ItemTypeData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Item.Slot), database.ItemSlotData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Item.Quality), database.ItemQualityData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Item.SuccessChance)),
                InterfaceUtils.CreateColumn(nameof(Item.ItemFind)),
                InterfaceUtils.CreateColumn(nameof(Item.CoinBonus)),
                InterfaceUtils.CreateColumn(nameof(Item.XpBonus)),
                InterfaceUtils.CreateColumn(nameof(Item.PreventDeathBonus)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<Item>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.ItemData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.Description = source.Description;
                    dest.Max = source.Max;
                    dest.Type = database.ItemTypeData.ReadById(source.Type.Id);
                    dest.Slot = database.ItemSlotData.ReadById(source.Slot.Id);
                    dest.Quality = database.ItemQualityData.ReadById(source.Quality.Id);
                    dest.SuccessChance = source.SuccessChance;
                    dest.ItemFind = source.ItemFind;
                    dest.CoinBonus = source.CoinBonus;
                    dest.XpBonus = source.XpBonus;
                    dest.PreventDeathBonus = source.PreventDeathBonus;
                });
            }
        }
    }
}
