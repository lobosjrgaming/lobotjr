using LobotJR.Data;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Utils
{
    /// <summary>
    /// An expression used to copy data from one object to another of the
    /// same type.
    /// </summary>
    /// <typeparam name="T">The type of the objects to copy.</typeparam>
    /// <param name="source">The object containing the data to be copied.</param>
    /// <param name="destination">The object that data will be copied to.</param>
    public delegate void CopyFunction<T>(T source, T destination);

    public static class DataUtils
    {
        /// <summary>
        /// Syncs the data in a collection to the underlying database.
        /// </summary>
        /// <typeparam name="T">The type being synced, must be a TableObject
        /// or subclass.</typeparam>
        /// <param name="table">The repository for the data in the database.</param>
        /// <param name="data">A collection of the updated data to copy to the
        /// database.</param>
        /// <param name="copyFunc">A lambda expression used to copy data from
        /// the collection to its matching object in the database.</param>
        public static void SyncTable<T>(IRepository<T> table, IEnumerable<T> data, CopyFunction<T> copyFunc) where T : TableObject
        {
            var dbData = table.Read().ToList();
            var dataIds = data.Select(x => x.Id).ToList();
            var toAdd = data.Where(x => x.Id == 0).ToList();
            var toRemove = dbData.Where(x => !dataIds.Contains(x.Id)).ToList();
            var toSync = data.Except(toAdd).ToList();

            foreach (var item in toAdd)
            {
                table.Create(item);
            }
            foreach (var item in toRemove)
            {
                table.Delete(item);
            }
            foreach (var item in toSync)
            {
                var dbItem = table.ReadById(item.Id);
                copyFunc(item, dbItem);
            }
            table.Commit();
        }
    }
}
