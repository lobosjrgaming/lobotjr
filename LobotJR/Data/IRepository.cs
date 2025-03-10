﻿using NLog;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LobotJR.Data
{
    public interface IRepository<TEntity>
    {
        void BeginTransaction();
        TEntity Create(TEntity entry);
        IEnumerable<TEntity> Create(IEnumerable<TEntity> entries);
        IEnumerable<TEntity> BatchCreate(IEnumerable<TEntity> entries, int batchSize, Logger logger, string name);
        IEnumerable<TEntity> Read();
        IEnumerable<TEntity> Read(Expression<Func<TEntity, bool>> filter);
        TEntity Read(TEntity entry);
        IEnumerable<TEntity> ReadWith<TProperty>(Expression<Func<TEntity, TProperty>> includeFilter);
        IEnumerable<TEntity> ReadWith<TProperty, TProperty2>(Expression<Func<TEntity, TProperty>> includeFilter, Expression<Func<TEntity, TProperty2>> includeFilter2);
        IEnumerable<TEntity> ReadWith<TProperty, TProperty2, TProperty3>(Expression<Func<TEntity, TProperty>> includeFilter, Expression<Func<TEntity, TProperty2>> includeFilter2, Expression<Func<TEntity, TProperty3>> includeFilter3);
        TEntity First(Expression<Func<TEntity, bool>> filter);
        TEntity FirstOrDefault(Expression<Func<TEntity, bool>> filter);
        bool Any(Expression<Func<TEntity, bool>> filter);
        TEntity ReadById(int id);
        /// <summary>
        /// Attaches an object to the database context and flags it to be
        /// written to the database. This only needs to be called when an
        /// object is saved and modified after the context has been closed, or
        /// when an object is created without calling Create().
        /// </summary>
        /// <param name="entry">The object to update.</param>
        /// <returns>The updated object.</returns>
        TEntity Update(TEntity entry);
        IEnumerable<TEntity> Delete();
        TEntity Delete(TEntity entry);
        TEntity DeleteById(int id);
        IEnumerable<TEntity> DeleteRange(IEnumerable<TEntity> entries);
        IEnumerable<TEntity> DeleteAll();
        void Commit();
    }
}
