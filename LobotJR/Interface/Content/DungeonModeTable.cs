using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class DungeonModeTable : IContentTable
    {
        public Type ContentType => typeof(DungeonMode);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.DungeonModeData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(DungeonMode.Id), true),
                InterfaceUtils.CreateColumn(nameof(DungeonMode.Name)),
                InterfaceUtils.CreateColumn(nameof(DungeonMode.Flag)),
                InterfaceUtils.CreateColumnCheckBox(nameof(DungeonMode.IsDefault)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<DungeonMode>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.DungeonModeData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.Flag = source.Flag;
                    dest.IsDefault = source.IsDefault;
                });
            }
        }
    }
}
