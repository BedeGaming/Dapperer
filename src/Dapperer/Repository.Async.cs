using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;

namespace Dapperer
{
    public abstract partial class Repository<TEntity, TPrimaryKey>
        where TEntity : class, IIdentifier<TPrimaryKey>, new()
    {
        public virtual async Task<TEntity> GetSingleOrDefaultAsync(TPrimaryKey primaryKey)
        {
            string sql = _queryBuilder.GetByPrimaryKeyQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return (await connection.QueryAsync<TEntity>(sql, new { Key = primaryKey }).ConfigureAwait(false)).SingleOrDefault();
            }
        }

        public virtual async Task<IList<TEntity>> GetByKeysAsync(IEnumerable<TPrimaryKey> primaryKeys)
        {
            string sql = _queryBuilder.GetByPrimaryKeysQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return (await connection.QueryAsync<TEntity>(sql, new { Keys = primaryKeys }).ConfigureAwait(false)).ToList();
            }
        }

        public virtual async Task<IList<TEntity>> GetAllAsync()
        {
            string sql = _queryBuilder.GetAll<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return (await connection.QueryAsync<TEntity>(sql).ConfigureAwait(false)).ToList();
            }
        }

        public virtual async Task<Page<TEntity>> PageAsync(int skip, int take)
        {
            return await PageAsync(skip, take, null).ConfigureAwait(false);
        }

        public virtual async Task<TEntity> CreateAsync(TEntity entity)
        {
            string sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>();

            using (IDbConnection connection = CreateConnection())
            {
                if (_queryBuilder.GetBaseTableInfo<TEntity>().AutoIncrement)
                {
                    TPrimaryKey identity = (await connection.QueryAsync<TPrimaryKey>(sql, entity).ConfigureAwait(false)).SingleOrDefault();
                    entity.SetIdentity(identity);
                }
                else
                {
                    await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
                }

                return entity;
            }
        }

        public virtual async Task<int> CreateAsync(IEnumerable<TEntity> entities)
        {
            string sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>(true);

            using (IDbConnection connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, entities).ConfigureAwait(false);
            }
        }

        public virtual async Task<int> UpdateAsync(TEntity entity)
        {
            string sql = _queryBuilder.UpdateQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        public virtual async Task<int> DeleteAsync(TPrimaryKey primaryKey)
        {
            string sql = _queryBuilder.DeleteQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, new { Key = primaryKey }).ConfigureAwait(false);
            }
        }

        public virtual async Task<int> DeleteAsync(string filterQuery, object filterParams = null)
        {
            string sql = _queryBuilder.DeleteQuery<TEntity>(filterQuery);

            using (IDbConnection connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, filterParams).ConfigureAwait(false);
            }
        }

        protected async Task<Page<TEntity>> PageAsync(int skip, int take, string filterQuery, object filterParams = null, string orderByQuery = null)
        {
            PagingSql pagingSql = GetPagingSql(skip, take, filterQuery, orderByQuery);

            using (IDbConnection connection = CreateConnection())
            {
                int totalItems = (await connection.QueryAsync<int>(pagingSql.Count, filterParams).ConfigureAwait(false)).SingleOrDefault();
                List<TEntity> items = (await connection.QueryAsync<TEntity>(pagingSql.Items, filterParams).ConfigureAwait(false)).ToList();

                return PageResults(skip, take, totalItems, items);
            }
        }

        protected async Task PopulateOneToOneAsync<TForeignEntity, TForeignEntityPrimaryKey>(
            Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (entities == null)
                return;

            var entityLoader = new OneToOneEntityLoader<TEntity, TPrimaryKey, TForeignEntity, TForeignEntityPrimaryKey>(
                CreateConnection,
                _queryBuilder,
                foreignKey,
                foreignEntity);

            await entityLoader.PopulateAsync(entities.ToArray()).ConfigureAwait(false);
        }

        protected async Task PopulateOneToManyAsync<TForeignEntity, TForeignEntityPrimaryKey>(
            Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (entities == null)
                return;

            var entityLoader = new OneToManyEntityLoader<TEntity, TPrimaryKey, TForeignEntity, TForeignEntityPrimaryKey>(
                CreateConnection,
                _queryBuilder,
                foreignKey,
                foreignEntityCollection);

            await entityLoader.PopulateAsync(entities.ToArray()).ConfigureAwait(false);
        }

        protected async Task PopulateOneToOneAsync<TForeignEntity, TForeignEntityPrimaryKey>(
            Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity,
            params TEntity[] entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (!entities.Any())
                return;

            var entityLoader = new OneToOneEntityLoader<TEntity, TPrimaryKey, TForeignEntity, TForeignEntityPrimaryKey>(
                CreateConnection,
                _queryBuilder,
                foreignKey,
                foreignEntity);

            await entityLoader.PopulateAsync(entities).ConfigureAwait(false);
        }

        protected async Task PopulateOneToManyAsync<TForeignEntity, TForeignEntityPrimaryKey>(
            Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection,
            params TEntity[] entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (!entities.Any())
                return;

            var entityLoader = new OneToManyEntityLoader<TEntity, TPrimaryKey, TForeignEntity, TForeignEntityPrimaryKey>(
                CreateConnection,
                _queryBuilder,
                foreignKey,
                foreignEntityCollection);

            await entityLoader.PopulateAsync(entities).ConfigureAwait(false);
        }
    }
}
