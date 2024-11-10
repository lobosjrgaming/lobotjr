using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class EquippablesTable : IContentTable
    {
        public Type ContentType => typeof(Equippables);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.EquippableData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(Equippables.Id), true),
                InterfaceUtils.CreateColumn(nameof(Equippables.CharacterClass), database.CharacterClassData.Read().ToList()),
                InterfaceUtils.CreateColumn(nameof(Equippables.ItemType), database.ItemTypeData.Read().ToList()),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<Equippables>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.EquippableData, typedData, (source, dest) =>
                {
                    dest.CharacterClass = database.CharacterClassData.ReadById(source.CharacterClass.Id);
                    dest.ItemType = database.ItemTypeData.ReadById(source.ItemType.Id);
                });
            }
        }
    }
}
