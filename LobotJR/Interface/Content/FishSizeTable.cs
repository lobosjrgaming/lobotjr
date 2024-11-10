using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class FishSizeTable : IContentTable
    {
        public Type ContentType => typeof(FishSize);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.FishSizeData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(FishSize.Id), true),
                InterfaceUtils.CreateColumn(nameof(FishSize.Name)),
                InterfaceUtils.CreateColumn(nameof(FishSize.Message)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<FishSize>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.FishSizeData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.Message = source.Message;
                });
            }
        }
    }
}
