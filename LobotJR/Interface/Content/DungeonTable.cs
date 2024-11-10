using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class DungeonTable : IContentTable
    {
        public Type ContentType => typeof(Dungeon);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.DungeonData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(Dungeon.Id), true),
                InterfaceUtils.CreateColumn(nameof(Dungeon.Name)),
                InterfaceUtils.CreateColumn(nameof(Dungeon.Description)),
                InterfaceUtils.CreateColumn(nameof(Dungeon.Introduction)),
                InterfaceUtils.CreateColumn(nameof(Dungeon.FailureText)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<Dungeon>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.DungeonData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.Description = source.Description;
                    dest.Introduction = source.Introduction;
                    dest.FailureText = source.FailureText;
                });
            }
        }
    }
}
