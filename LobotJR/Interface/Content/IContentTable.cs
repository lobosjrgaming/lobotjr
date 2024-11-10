using LobotJR.Data;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    /// <summary>
    /// Interface used to create the editor for different content tables in the UI.
    /// </summary>
    public interface IContentTable
    {
        /// <summary>
        /// The type of the object for this table.
        /// </summary>
        Type ContentType { get; }
        /// <summary>
        /// Creates the columns used to display the data.
        /// </summary>
        /// <param name="database">An active, open database connection.</param>
        /// <returns>A collection of columns.</returns>
        IEnumerable<DataGridColumn> CreateColumns(IDatabase database);
        /// <summary>
        /// Gets the source for the items in the table.
        /// </summary>
        /// <param name="database">An active, open database connection.</param>
        /// <returns>A collection of items in the table.</returns>
        IEnumerable<TableObject> GetSource(IDatabase database);
        /// <summary>
        /// Saves changes to the database.
        /// </summary>
        /// <param name="database">An active, open database connection.</param>
        /// <param name="data">The data in its desired state.</param>
        void SaveData(IDatabase database, IEnumerable<TableObject> data);
    }
}
