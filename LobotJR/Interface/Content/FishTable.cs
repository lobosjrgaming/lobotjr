using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class FishTable : IContentTable
    {
        public Type ContentType => typeof(Fish);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.FishData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(Fish.Id), true),
                InterfaceUtils.CreateColumn(nameof(Fish.Name)),
                InterfaceUtils.CreateColumn(nameof(Fish.FlavorText)),
                InterfaceUtils.CreateColumn(nameof(Fish.Rarity), database.FishRarityData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Fish.SizeCategory), database.FishSizeData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Fish.MinimumLength)),
                InterfaceUtils.CreateColumn(nameof(Fish.MaximumLength)),
                InterfaceUtils.CreateColumn(nameof(Fish.MinimumWeight)),
                InterfaceUtils.CreateColumn(nameof(Fish.MaximumWeight))
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<Fish>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.FishData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.FlavorText = source.FlavorText;
                    dest.Rarity = database.FishRarityData.ReadById(source.Rarity.Id);
                    dest.SizeCategory = database.FishSizeData.ReadById(source.SizeCategory.Id);
                    dest.MinimumLength = source.MinimumLength;
                    dest.MaximumLength = source.MaximumLength;
                    dest.MinimumWeight = source.MinimumWeight;
                    dest.MaximumWeight = source.MaximumWeight;
                });
            }
        }
    }
}
