using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;

namespace Dapperer
{
    public abstract partial class Repository<TEntity, TPrimaryKey>
        where TEntity : class, IIdentifier<TPrimaryKey>, new()
    {
        public async Task<TEntity> GetSingleOrDefaultAsync(TPrimaryKey primaryKey)
        {
            var sql = _queryBuilder.GetByPrimaryKeyQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return (await connection.QueryAsync<TEntity>(sql, new { Key = primaryKey }).ConfigureAwait(false)).SingleOrDefault();
            }
        }

        public async Task<IList<TEntity>> GetByKeysAsync(IEnumerable<TPrimaryKey> primaryKeys)
        {
            var sql = _queryBuilder.GetByPrimaryKeysQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return (await connection.QueryAsync<TEntity>(sql, new { Keys = primaryKeys }).ConfigureAwait(false)).ToList();
            }
        }

        public async Task<IList<TEntity>> GetAllAsync()
        {
            var sql = _queryBuilder.GetAll<TEntity>();

            using (var connection = CreateConnection())
            {
                return (await connection.QueryAsync<TEntity>(sql).ConfigureAwait(false)).ToList();
            }
        }

        public async Task<Page<TEntity>> PageAsync(int skip, int take)
        {
            return await PageAsync(skip, take, null).ConfigureAwait(false);
        }

        public async Task<TEntity> CreateAsync(TEntity entity)
        {
            var sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>();

            using (var connection = CreateConnection())
            {
                if (_queryBuilder.GetBaseTableInfo<TEntity>().AutoIncrement)
                {
                    var identity = (await connection.QueryAsync<TPrimaryKey>(sql, entity).ConfigureAwait(false)).SingleOrDefault();
                    entity.SetIdentity(identity);
                }
                else
                {
                    await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
                }

                return entity;
            }
        }

        public async Task<int> CreateAsync(IEnumerable<TEntity> entities)
        {
            var sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>();

            using (var connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, entities).ConfigureAwait(false);
            }
        }

        public async Task<int> UpdateAsync(TEntity entity)
        {
            var sql = _queryBuilder.UpdateQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        public async Task<int> DeleteAsync(TPrimaryKey primaryKey)
        {
            var sql = _queryBuilder.DeleteQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, new { Key = primaryKey }).ConfigureAwait(false);
            }
        }

        public async Task<int> DeleteAsync(string filterQuery, object filterParams = null)
        {
            var sql = _queryBuilder.DeleteQuery<TEntity>(filterQuery);

            using (var connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, filterParams).ConfigureAwait(false);
            }
        }

        protected async Task<Page<TEntity>> PageAsync(int skip, int take, string filterQuery, object filterParams = null, string orderByQuery = null)
        {
            var pagingSql = GetPagingSql(skip, take, filterQuery, orderByQuery);

            using (var connection = CreateConnection())
            {
                var totalItems = (await connection.QueryAsync<int>(pagingSql.Count, filterParams).ConfigureAwait(false)).SingleOrDefault();
                var items = (await connection.QueryAsync<TEntity>(pagingSql.Items, filterParams).ConfigureAwait(false)).ToList();

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

        public async Task<Page<TEntity>> PageAsync(string query, string countQuery, int skip, int take, object queryParams = null, string orderByQuery = null)
        {
            using (var connection = CreateConnection())
            {
                var totalItems = (await connection.QueryAsync<int>(countQuery, queryParams).ConfigureAwait(false)).SingleOrDefault();
                var items = (await connection.QueryAsync<TEntity>(query, queryParams).ConfigureAwait(false)).ToList();

                return PageResults(skip, take, totalItems, items);
            }
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
