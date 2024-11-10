using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class FishRarityTable : IContentTable
    {
        public Type ContentType => typeof(FishRarity);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.FishRarityData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(FishRarity.Id), true),
                InterfaceUtils.CreateColumn(nameof(FishRarity.Name)),
                InterfaceUtils.CreateColumn(nameof(FishRarity.Weight)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<FishRarity>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.FishRarityData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.Weight = source.Weight;
                });
            }
        }
    }
}
