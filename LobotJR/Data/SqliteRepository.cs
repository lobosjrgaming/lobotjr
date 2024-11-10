using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

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

        public IEnumerable<TEntity> Create(IEnumerable<TEntity> entries)
        {
            return dbSet.AddRange(entries);
        }

        public IEnumerable<TEntity> BatchCreate(IEnumerable<TEntity> entries, int batchSize, Logger logger, string name)
        {
            var entryList = entries.ToList();
            var total = entryList.Count;
            var startTime = DateTime.Now;
            var logTime = DateTime.Now;
            var processed = 0;
            var cursor = 0;
            BeginTransaction();
            do
            {
                if (DateTime.Now - logTime > TimeSpan.FromSeconds(5))
                {
                    Commit();
                    BeginTransaction();
                    var elapsed = DateTime.Now - startTime;
                    var estimate = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / processed * total) - elapsed;
                    logger.Info("{count} of {total} {name} records written. {elapsed} time elapsed, {estimate} estimated remaining.", processed, total, name, elapsed.ToString("hh\\:mm\\:ss"), estimate.ToString("hh\\:mm\\:ss"));
                    logTime = DateTime.Now;
                }
                Create(entryList.Skip(cursor).Take(batchSize));
                cursor += batchSize;
                processed += batchSize;

            } while (cursor < entryList.Count);
            Commit();
            return entryList;
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

        public IEnumerable<TEntity> DeleteAll()
        {
            var removed = dbSet.RemoveRange(dbSet);
            Commit();
            return removed;
        }

        public IEnumerable<TEntity> Read()
        {
            return dbSet;
        }

        public IEnumerable<TEntity> ReadWith<TProperty>(Expression<Func<TEntity, TProperty>> includeFilter)
        {
            return dbSet.Include(includeFilter);
        }

        public IEnumerable<TEntity> ReadWith<TProperty, TProperty2>(Expression<Func<TEntity, TProperty>> includeFilter, Expression<Func<TEntity, TProperty2>> includeFilter2)
        {
            return dbSet.Include(includeFilter).Include(includeFilter2);
        }

        public IEnumerable<TEntity> ReadWith<TProperty, TProperty2, TProperty3>(Expression<Func<TEntity, TProperty>> includeFilter, Expression<Func<TEntity, TProperty2>> includeFilter2, Expression<Func<TEntity, TProperty3>> includeFilter3)
        {
            return dbSet.Include(includeFilter).Include(includeFilter2).Include(includeFilter3);
        }

        public IEnumerable<TEntity> Read(Expression<Func<TEntity, bool>> filter)
        {
            return dbSet.Where(filter);
        }

        public TEntity Read(TEntity entry)
        {
            return dbSet.Where(x => x.Equals(entry)).FirstOrDefault();
        }

        public TEntity First(Expression<Func<TEntity, bool>> filter)
        {
            return dbSet.First(filter);
        }

        public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> filter)
        {
            return dbSet.FirstOrDefault(filter);
        }

        public bool Any(Expression<Func<TEntity, bool>> filter)
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
