using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Dapperer
{
    public abstract partial class Repository<TEntity, TPrimaryKey>
        where TEntity : class, IIdentifier<TPrimaryKey>, new()
    {
        public virtual async Task<TEntity> GetSingleOrDefaultAsync(TPrimaryKey primaryKey)
        {
            string sql = _queryBuilder.GetByPrimaryKeyQuery<TEntity>();
            var parameters = new DynamicParameters();
            parameters.Add("@Key", _queryBuilder.GetPrimaryKeyParameter<TEntity, TPrimaryKey>(primaryKey));

            using (IDbConnection connection = CreateConnection())
            {

                return (await connection.QueryAsync<TEntity>(sql, parameters).ConfigureAwait(false)).SingleOrDefault();
            }
        }

        public virtual async Task<IList<TEntity>> GetByKeysAsync(IEnumerable<TPrimaryKey> primaryKeys)
        {
            string sql = _queryBuilder.GetByPrimaryKeysQuery<TEntity>();
            var parameters = new DynamicParameters();
            parameters.Add("@Keys", _queryBuilder.GetPrimaryKeyParameters<TEntity, TPrimaryKey>(primaryKeys));

            using (IDbConnection connection = CreateConnection())
            {
                return (await connection.QueryAsync<TEntity>(sql, parameters).ConfigureAwait(false)).ToList();
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
            return await CreateAsync(entity, identityInsert: false);
        }

        public virtual async Task<TEntity> CreateAsync(TEntity entity, bool identityInsert)
        {
            string sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>(identityInsert: identityInsert);

            using (IDbConnection connection = CreateConnection())
            {
                if (_queryBuilder.GetBaseTableInfo<TEntity>().AutoIncrement && !identityInsert)
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
            return await CreateAsync(entities, identityInsert: false);
        }

        public virtual async Task<int> CreateAsync(IEnumerable<TEntity> entities, bool identityInsert)
        {
            var tableInfo = (TableInfo)GetTableInfo();

            string[] columsToInsert = GetInsertColumns(tableInfo, identityInsert);

            int result = 0;
            var batches = SplitIntoBatches(entities, CalculateMaxBatchCountBasedOnColumnsCount(columsToInsert.Count()));

            using (IDbConnection connection = CreateConnection())
            {
                foreach (var batch in batches)
                {
                    string sql = _queryBuilder.InsertQueryBatch(batch, tableInfo.TableName, columsToInsert, identityInsert);

                    var parameters = ConvertEntitiesToParameters(batch.ToArray(), columsToInsert);

                    result += await connection.ExecuteAsync(sql, parameters)
                        .ConfigureAwait(false);
                }
            }

            return result;
        }

        public virtual async Task<int> UpdateAsync(TEntity entity)
        {
            string sql = _queryBuilder.UpdateQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        public virtual async Task<int> UpdateAsync(IEnumerable<TEntity> entities)
        {
            string sql = _queryBuilder.UpdateQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, entities).ConfigureAwait(false);
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

        protected async Task<Page<TEntity>> PageAsync(int skip, int take, string filterQuery, object filterParams = null, string orderByQuery = null, ICollection<string> additionalTableColumns = null, string fromQuery = null)
        {
            PagingSql pagingSql = GetPagingSql(skip, take, fromQuery, filterQuery, orderByQuery, additionalTableColumns);

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

        public async Task<Page<TEntity>> PageAsync(string query, string countQuery, int skip, int take, object queryParams = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                int totalItems = (await connection.QueryAsync<int>(countQuery, queryParams).ConfigureAwait(false)).SingleOrDefault();
                List<TEntity> items = (await connection.QueryAsync<TEntity>(query, queryParams).ConfigureAwait(false)).ToList();

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

        private List<IEnumerable<T>> SplitIntoBatches<T>(IEnumerable<T> items, int batchSize)
        {
            int currentSkip = 0;

            IEnumerable<T> currentPagedItems = items.Skip(currentSkip).Take(batchSize);

            List<IEnumerable<T>> result = new List<IEnumerable<T>>();
            while (currentPagedItems.Count() > 0)
            {
                result.Add(currentPagedItems);

                currentSkip += batchSize;
                currentPagedItems = items.Skip(currentSkip).Take(batchSize);
            }

            return result;
        }

        private Dictionary<string, object> ConvertEntitiesToParameters(TEntity[] batch, string[] columsToInsert)
        {
            var result = new Dictionary<string, object>();

            for (int i = 0; i < batch.Length; i++)
            {
                var currentItem = batch[i];

                foreach (var columnName in columsToInsert)
                {
                    string key = $"{columnName}{i}";
                    var property = typeof(TEntity).GetProperty(columnName);

                    result[key] = property.GetValue(currentItem);
                }
            }

            return result;
        }

        private int CalculateMaxBatchCountBasedOnColumnsCount(int columnsCount)
        {
            const int maxSqlParameters = 2100;
            const int maxInsertRows = 1000;

            // Max sql parameters are 2100 for a single query
            // we substract 30 for extra query parmaters
            var count = (maxSqlParameters - 30) / columnsCount;

            return count <= maxInsertRows ? count : maxInsertRows;
        }

        private string[] GetInsertColumns(TableInfo tableInfo, bool identityInsert)
        {
            string[] columsToInsert = tableInfo.ColumnInfos
                .Select(x => x.ColumnName)
                .ToArray();

            if (!identityInsert && tableInfo.AutoIncrement)
            {
                columsToInsert = columsToInsert.Where(cm => cm != tableInfo.Key)
                    .ToArray();
            }

            return columsToInsert;
        }
    }
}
