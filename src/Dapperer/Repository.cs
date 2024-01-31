using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Dapper;

namespace Dapperer
{
    /// <summary>
    /// Generic repository for basic CRUD operation
    /// Extend it per specify entity types in order to add custom methods
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TPrimaryKey">Primary key type either</typeparam>
    public abstract partial class Repository<TEntity, TPrimaryKey> : IRepository<TEntity, TPrimaryKey>
        where TEntity : class, IIdentifier<TPrimaryKey>, new()
    {
        private readonly IQueryBuilder _queryBuilder;
        private readonly IDbFactory _dbFactory;

        protected Repository(IQueryBuilder queryBuilder, IDbFactory dbFactory)
        {
            _queryBuilder = queryBuilder;
            _dbFactory = dbFactory;
        }

        protected IDbConnection CreateConnection()
        {
            return _dbFactory.CreateConnection();
        }

        public virtual TEntity GetSingleOrDefault(TPrimaryKey primaryKey)
        {
            string sql = _queryBuilder.GetByPrimaryKeyQuery<TEntity>();
            var parameters = new DynamicParameters();
            parameters.Add("@Key", _queryBuilder.GetPrimaryKeyParameter<TEntity, TPrimaryKey>(primaryKey));

            using (IDbConnection connection = CreateConnection())
            {

                return connection.Query<TEntity>(sql, parameters).SingleOrDefault();
            }
        }

        public virtual IList<TEntity> GetByKeys(IEnumerable<TPrimaryKey> primaryKeys)
        {
            string sql = _queryBuilder.GetByPrimaryKeysQuery<TEntity>();
            var parameters = new DynamicParameters();
            parameters.Add("@Keys", _queryBuilder.GetPrimaryKeyParameters<TEntity, TPrimaryKey>(primaryKeys));

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Query<TEntity>(sql, parameters).ToList();
            }
        }

        public virtual IList<TEntity> GetAll()
        {
            string sql = _queryBuilder.GetAll<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Query<TEntity>(sql).ToList();
            }
        }

        public virtual Page<TEntity> Page(int skip, int take)
        {
            return Page(skip, take, null);
        }

        public virtual TEntity Create(TEntity entity)
        {
            return Create(entity, identityInsert: false);
        }

        public virtual TEntity Create(TEntity entity, bool identityInsert)
        {
            string sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>(identityInsert: identityInsert);

            using (IDbConnection connection = CreateConnection())
            {
                if (_queryBuilder.GetBaseTableInfo<TEntity>().AutoIncrement && !identityInsert)
                {
                    TPrimaryKey identity = connection.Query<TPrimaryKey>(sql, entity).SingleOrDefault();
                    entity.SetIdentity(identity);
                }
                else
                {
                    connection.Execute(sql, entity);
                }

                return entity;
            }
        }

        public virtual int Create(IEnumerable<TEntity> entities)
        {
            return Create(entities, identityInsert: false);
        }

        public virtual int Create(IEnumerable<TEntity> entities, bool identityInsert)
        {
            var tableInfo = (TableInfo)GetTableInfo();

            string[] columsToInsert = GetInsertColumns(tableInfo, identityInsert);

            int result = 0;
            var batches = SplitIntoBatches(entities, CalculateMaxBatchCountBasedOnColumnsCount(columsToInsert.Count()));

            using (IDbConnection connection = CreateConnection())
            {
                foreach (var batch in batches)
                {
                    string sql = _queryBuilder.InsertQueryBatch(batch, tableInfo.TableName, columsToInsert);

                    var parameters = ConvertEntitiesToParameters(batch.ToArray(), columsToInsert);

                    result += connection.Execute(sql, parameters);
                }
            }

            return result;
        }

        public virtual IEnumerable<TEntity> CreateBatch(IEnumerable<TEntity> entities)
        {
            return CreateBatch(entities, identityInsert: false);
        }

        public virtual IEnumerable<TEntity> CreateBatch(IEnumerable<TEntity> entities, bool identityInsert)
        {
            var tableInfo = (TableInfo)GetTableInfo();

            string[] columsToInsert = GetInsertColumns(tableInfo, identityInsert);

            List<TEntity> results = new List<TEntity>();
            var batches = SplitIntoBatches(entities, CalculateMaxBatchCountBasedOnColumnsCount(columsToInsert.Count()));

            using (IDbConnection connection = CreateConnection())
            {
                foreach (var batch in batches)
                {
                    string sql = _queryBuilder.InsertQueryOutputBatch(batch, tableInfo.TableName, columsToInsert);

                    var parameters = ConvertEntitiesToParameters(batch.ToArray(), columsToInsert);

                    results.AddRange(connection.Query<TEntity>(sql, parameters));
                }
            }

            return results;
        }

        public virtual int Update(TEntity entity)
        {
            string sql = _queryBuilder.UpdateQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Execute(sql, entity);
            }
        }

        public virtual int Update(IEnumerable<TEntity> entities)
        {
            string sql = _queryBuilder.UpdateQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Execute(sql, entities);
            }
        }

        public virtual int Delete(TPrimaryKey primaryKey)
        {
            string sql = _queryBuilder.DeleteQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Execute(sql, new { Key = primaryKey });
            }
        }

        public virtual int Delete(string filterQuery, object filterParams = null)
        {
            string sql = _queryBuilder.DeleteQuery<TEntity>(filterQuery);

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Execute(sql, filterParams);
            }
        }

        protected ITableInfoBase GetTableInfo()
        {
            return _queryBuilder.GetBaseTableInfo<TEntity>();
        }

        protected void PopulateOneToOne<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (entities == null)
                return;

            PopulateOneToOne(foreignKey, foreignEntity, entities.ToArray());
        }

        [ObsoleteAttribute("This method is obsolete. Call PopulateOneToOne instead.", false)]
        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            PopulateOneToOne(foreignKey, foreignEntity, entities.ToArray());
        }

        protected void PopulateOneToMany<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (entities == null)
                return;

            PopulateOneToMany<TForeignEntity, TForeignEntityPrimaryKey>(foreignKey, foreignEntityCollection, entities.ToArray());
        }

        [ObsoleteAttribute("This method is obsolete. Call PopulateOneToMany instead.", false)]
        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            PopulateOneToMany<TForeignEntity, TForeignEntityPrimaryKey>(foreignKey, foreignEntityCollection, entities.ToArray());
        }

        protected void PopulateOneToOne<TForeignEntity, TForeignEntityPrimaryKey>(
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

            entityLoader.Populate(entities);
        }

        [ObsoleteAttribute("This method is obsolete. Call PopulateOneToOne instead.", false)]
        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(
            Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity,
            params TEntity[] entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            PopulateOneToOne(foreignKey, foreignEntity, entities);
        }

        protected void PopulateOneToMany<TForeignEntity, TForeignEntityPrimaryKey>(
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

            entityLoader.Populate(entities);
        }

        [ObsoleteAttribute("This method is obsolete. Call PopulateOneToMany instead.", false)]
        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(
            Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection,
            params TEntity[] entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            PopulateOneToMany<TForeignEntity, TForeignEntityPrimaryKey>(foreignKey, foreignEntityCollection, entities);
        }

        protected Page<TEntity> Page(int skip, int take, string filterQuery, object filterParams = null, string orderByQuery = null, ICollection<string> additionalTableColumns = null, string fromQuery = null)
        {
            PagingSql pagingSql = GetPagingSql(skip, take, fromQuery, filterQuery, orderByQuery, additionalTableColumns);

            using (IDbConnection connection = CreateConnection())
            {
                int totalItems = connection.Query<int>(pagingSql.Count, filterParams).SingleOrDefault();
                List<TEntity> items = connection.Query<TEntity>(pagingSql.Items, filterParams).ToList();

                return PageResults(skip, take, totalItems, items);
            }
        }

        public Page<TEntity> Page(string query, string countQuery, int skip, int take, object queryParams = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                int totalItems = connection.Query<int>(countQuery, queryParams).SingleOrDefault();
                List<TEntity> items = connection.Query<TEntity>(query, queryParams).ToList();

                return PageResults(skip, take, totalItems, items);
            }
        }

        private static int GetTotalPages(int take, int totalItems)
        {
            if (take != 0)
            {
                bool hasRemainderForAdditionalPage = totalItems % take != 0;
                int totalPages = totalItems / take;

                if (hasRemainderForAdditionalPage)
                {
                    totalPages++;
                }

                return totalPages;
            }

            return 1;
        }

        private int GetCurrentPage(int skip, int take)
        {
            if (take != 0)
            {
                bool hasNoRemainderForAdditionalPage = skip % take == 0;
                int currentPage = skip / take;

                if (hasNoRemainderForAdditionalPage || currentPage == 0)
                {
                    currentPage++;
                }

                return currentPage;
            }

            return 1;
        }

        protected Page<T> PageResults<T>(int skip, int take, int totalItems, List<T> items)
            where T : class
        {
            int totalPages = GetTotalPages(take, totalItems);
            int currentPage = GetCurrentPage(skip, take);
            int itemsPerPage = take == 0 ? totalItems : take;

            return new Page<T>
            {
                CurrentPage = currentPage,
                ItemsPerPage = itemsPerPage,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = items
            };
        }

        protected PagingSql GetPagingSql(int skip, int take, string fromQuery, string filterQuery, string orderByQuery, ICollection<string> additionalTableColumns)
        {
            if (skip < 0)
                throw new ArgumentException("Invalid skip value", "skip");
            if (take < 0)
                throw new ArgumentException("Invalid take value", "take");

            return _queryBuilder.PageQuery<TEntity>(skip, take, fromQuery, orderByQuery, filterQuery, additionalTableColumns);
        }
    }
}