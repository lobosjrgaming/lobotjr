using LobotJR.Command.Model.Equipment;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class ItemQualityTable : IContentTable
    {
        public Type ContentType => typeof(ItemQuality);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.ItemQualityData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(ItemQuality.Id), true),
                InterfaceUtils.CreateColumn(nameof(ItemQuality.Name)),
                InterfaceUtils.CreateColumn(nameof(ItemQuality.DropRatePenalty)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<ItemQuality>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.ItemQualityData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.DropRatePenalty = source.DropRatePenalty;
                });
            }
        }
    }
}
