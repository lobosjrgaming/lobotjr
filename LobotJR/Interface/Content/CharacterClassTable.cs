using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public class CharacterClassTable : IContentTable
    {
        public Type ContentType => typeof(CharacterClass);

        public IEnumerable<TableObject> GetSource(IDatabase database)
        {
            return database.CharacterClassData.Read();
        }

        public IEnumerable<DataGridColumn> CreateColumns(IDatabase database)
        {
            return new List<DataGridColumn>()
            {
                InterfaceUtils.CreateColumn(nameof(CharacterClass.Id), true),
                InterfaceUtils.CreateColumn(nameof(CharacterClass.Name)),
                InterfaceUtils.CreateColumnCheckBox(nameof(CharacterClass.CanPlay)),
                InterfaceUtils.CreateColumn(nameof(CharacterClass.SuccessChance)),
                InterfaceUtils.CreateColumn(nameof(CharacterClass.ItemFind)),
                InterfaceUtils.CreateColumn(nameof(CharacterClass.CoinBonus)),
                InterfaceUtils.CreateColumn(nameof(CharacterClass.XpBonus)),
                InterfaceUtils.CreateColumn(nameof(CharacterClass.PreventDeathBonus)),
            };
        }

        public void SaveData(IDatabase database, IEnumerable<TableObject> data)
        {
            var typedData = data.Cast<CharacterClass>();
            if (typedData != null)
            {
                DataUtils.SyncTable(database.CharacterClassData, typedData, (source, dest) =>
                {
                    dest.Name = source.Name;
                    dest.CanPlay = source.CanPlay;
                    dest.SuccessChance = source.SuccessChance;
                    dest.ItemFind = source.ItemFind;
                    dest.CoinBonus = source.CoinBonus;
                    dest.XpBonus = source.XpBonus;
                    dest.PreventDeathBonus = source.PreventDeathBonus;
                });
            }
        }
    }
}
