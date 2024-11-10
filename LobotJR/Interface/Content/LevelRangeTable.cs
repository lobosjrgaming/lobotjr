using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class LevelRangeTable : IContentTable
    {
        public Type ContentType => typeof(LevelRange);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.LevelRangeData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(LevelRange.Id), true),
                InterfaceUtils.CreateColumn(nameof(LevelRange.Dungeon), database.DungeonData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(LevelRange.Mode), database.DungeonModeData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(LevelRange.Minimum)),
                InterfaceUtils.CreateColumn(nameof(LevelRange.Maximum)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<LevelRange>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.LevelRangeData, typedData, (source, dest) =>
                {
                    dest.Dungeon = database.DungeonData.ReadById(source.Dungeon.Id);
                    dest.Mode = database.DungeonModeData.ReadById(source.Mode.Id);
                    dest.Minimum = source.Minimum;
                    dest.Maximum = source.Maximum;
                });
            }
        }
    }
}
