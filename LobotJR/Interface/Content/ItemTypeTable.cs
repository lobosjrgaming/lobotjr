using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class ItemTypeTable : IContentTable
    {
        public Type ContentType => typeof(ItemType);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.ItemTypeData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(ItemType.Id), true),
                InterfaceUtils.CreateColumn(nameof(ItemType.Name)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<ItemType>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.ItemTypeData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                });
            }
        }
    }
}
