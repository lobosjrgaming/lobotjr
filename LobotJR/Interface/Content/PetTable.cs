using LobotJR.Command.Model.Pets;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class PetTable : IContentTable
    {
        public Type ContentType => typeof(Pet);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.PetData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(Pet.Id), true),
                InterfaceUtils.CreateColumn(nameof(Pet.Name)),
                InterfaceUtils.CreateColumn(nameof(Pet.Description)),
                InterfaceUtils.CreateColumn(nameof(Pet.Rarity), database.PetRarityData.Read().ToList()),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<Pet>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.PetData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.Description = source.Description;
                    dest.Rarity = database.PetRarityData.ReadById(source.Rarity.Id);
                });
            }
        }
    }
}
