using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class EncounterTable : IContentTable
    {
        public Type ContentType => typeof(Encounter);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.EncounterData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(Encounter.Id), true),
                InterfaceUtils.CreateColumn(nameof(Encounter.Dungeon), database.DungeonData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Encounter.Enemy)),
                InterfaceUtils.CreateColumn(nameof(Encounter.SetupText)),
                InterfaceUtils.CreateColumn(nameof(Encounter.CompleteText)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<Encounter>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.EncounterData, typedData, (source, dest) =>
                {
                    dest.Dungeon = database.DungeonData.ReadById(source.Dungeon.Id);
                    dest.Enemy = source.Enemy;
                    dest.SetupText = source.SetupText;
                    dest.CompleteText = source.CompleteText;
                });
            }
        }
    }
}
