using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    internal class ItemSlotTable : IContentTable
    {
        public Type ContentType => typeof(ItemSlot);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.ItemSlotData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(ItemSlot.Id), true),
                InterfaceUtils.CreateColumn(nameof(ItemSlot.Name)),
                InterfaceUtils.CreateColumn(nameof(ItemSlot.MaxEquipped)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<ItemSlot>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.ItemSlotData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.MaxEquipped = source.MaxEquipped;
                });
            }
        }
    }
}
