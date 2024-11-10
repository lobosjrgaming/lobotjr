using LobotJR.Data;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public interface IContentTable
    {
        Type ContentType { get; }
        IEnumerable<DataGridColumn> CreateColumns(IDatabase database);
        IEnumerable<TableObject> GetSource(IDatabase database);
        void SaveData(IDatabase database, IEnumerable<TableObject> data);
    }
}
