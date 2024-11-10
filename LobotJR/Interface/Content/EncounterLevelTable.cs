using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class EncounterLevelTable : IContentTable
    {
        public Type ContentType => typeof(EncounterLevel);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.EncounterLevelData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(EncounterLevel.Id), true),
                InterfaceUtils.CreateColumn(nameof(EncounterLevel.Encounter), database.EncounterData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(EncounterLevel.Mode), database.DungeonModeData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(EncounterLevel.Difficulty)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<EncounterLevel>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.EncounterLevelData, typedData, (source, dest) =>
                {
                    dest.Encounter = database.EncounterData.ReadById(source.Encounter.Id);
                    dest.Mode = database.DungeonModeData.ReadById(source.Mode.Id);
                    dest.Difficulty = source.Difficulty;
                });
            }
        }
    }
}
