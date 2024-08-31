using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace LobotJR.Data
{
    public class SqliteRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly DbContext context;
        private readonly DbSet<TEntity> dbSet;
        private DbContextTransaction transaction;

        public SqliteRepository(DbContext context)
        {
            this.context = context;
            dbSet = context.Set<TEntity>();
        }

        public void BeginTransaction()
        {
            context.Configuration.AutoDetectChangesEnabled = false;
            transaction = context.Database.BeginTransaction();
        }

        public void Commit()
        {
            if (transaction != null)
            {
                transaction.Commit();
                transaction.Dispose();
                transaction = null;
                context.Configuration.AutoDetectChangesEnabled = true;
            }
            context.SaveChanges();
        }

        public TEntity Create(TEntity entry)
        {
            return dbSet.Add(entry);
        }

        public IEnumerable<TEntity> Delete()
        {
            return dbSet.RemoveRange(dbSet);
        }

        public TEntity Delete(TEntity entry)
        {
            return dbSet.Remove(entry);
        }

        public TEntity DeleteById(int id)
        {
            var toRemove = dbSet.Find(id);
            if (toRemove != null)
            {
                return dbSet.Remove(toRemove);
            }
            return null;
        }

        public IEnumerable<TEntity> DeleteRange(IEnumerable<TEntity> entries)
        {
            return dbSet.RemoveRange(entries);
        }

        public IEnumerable<TEntity> Read()
        {
            return dbSet;
        }

        public IEnumerable<TEntity> Read(Func<TEntity, bool> filter)
        {
            return dbSet.Where(filter);
        }

        public TEntity Read(TEntity entry)
        {
            return dbSet.Where(x => x.Equals(entry)).FirstOrDefault();
        }

        public TEntity First(Func<TEntity, bool> filter)
        {
            return dbSet.First(filter);
        }

        public TEntity FirstOrDefault(Func<TEntity, bool> filter)
        {
            return dbSet.FirstOrDefault(filter);
        }

        public bool Any(Func<TEntity, bool> filter)
        {
            return dbSet.Any(filter);
        }

        public TEntity ReadById(int id)
        {
            return dbSet.Find(id);
        }

        /// <summary>
        /// Attaches an object to the database context and flags it to be
        /// written to the database. This only needs to be called when an
        /// object is saved and modified after the context has been closed, or
        /// when an object is created without calling Create().
        /// </summary>
        /// <param name="entry">The object to update.</param>
        /// <returns>The updated object.</returns>
        public TEntity Update(TEntity entry)
        {
            var output = dbSet.Attach(entry);
            context.Entry(entry).State = EntityState.Modified;
            return output;
        }
    }
}
