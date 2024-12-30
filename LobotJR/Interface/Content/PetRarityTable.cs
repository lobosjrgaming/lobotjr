using LobotJR.Command.Model.Pets;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class PetRarityTable : IContentTable
    {
        public Type ContentType => typeof(PetRarity);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.PetRarityData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(PetRarity.Id), true),
                InterfaceUtils.CreateColumn(nameof(PetRarity.Name)),
                InterfaceUtils.CreateColumn(nameof(PetRarity.DropRate)),
                InterfaceUtils.CreateColumn(nameof(PetRarity.Color)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<PetRarity>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.PetRarityData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.DropRate = source.DropRate;
                    dest.Color = source.Color;
                });
            }
        }
    }
}
