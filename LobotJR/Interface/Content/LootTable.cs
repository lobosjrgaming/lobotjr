using LobotJR.Command.Model.Dungeons;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class LootTable : IContentTable
    {
        public Type ContentType => typeof(Loot);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.LootData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(Loot.Id), true),
                InterfaceUtils.CreateColumn(nameof(Loot.Dungeon), database.DungeonData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Loot.Mode), database.DungeonModeData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Loot.Item), database.ItemData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Loot.DropChance)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<Loot>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.LootData, typedData, (source, dest) =>
                {
                    dest.Dungeon = database.DungeonData.ReadById(source.Dungeon.Id);
                    dest.Mode = database.DungeonModeData.ReadById(source.Mode.Id);
                    dest.Item = database.ItemData.ReadById(source.Item.Id);
                    dest.DropChance = source.DropChance;
                });
            }
        }
    }
}
