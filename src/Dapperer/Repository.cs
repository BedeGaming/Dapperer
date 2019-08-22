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

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Query<TEntity>(sql, new { Key = primaryKey }).SingleOrDefault();
            }
        }

        public virtual IList<TEntity> GetByKeys(IEnumerable<TPrimaryKey> primaryKeys)
        {
            string sql = _queryBuilder.GetByPrimaryKeysQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Query<TEntity>(sql, new { Keys = primaryKeys }).ToList();
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

        public virtual Page<TEntity> Page(int skip, int? take)
        {
            return Page(skip, take ?? 10);
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
            string sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>(true, identityInsert: identityInsert);

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Execute(sql, entities);
            }
        }

        public virtual int Update(TEntity entity)
        {
            string sql = _queryBuilder.UpdateQuery<TEntity>();

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Execute(sql, entity);
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

        protected Page<TEntity> Page(int skip, int take, string filterQuery, object filterParams = null, string orderByQuery = null)
        {
            PagingSql pagingSql = GetPagingSql(skip, take, filterQuery, orderByQuery);

            using (IDbConnection connection = CreateConnection())
            {
                int totalItems = connection.Query<int>(pagingSql.Count, filterParams).SingleOrDefault();
                List<TEntity> items = connection.Query<TEntity>(pagingSql.Items, filterParams).ToList();

                return PageResults(skip, take, totalItems, items);
            }
        }

        public Page<TEntity> Page(string query, string countQuery, int skip, int take, object queryParams = null, string orderByQuery = null)
        {
            using (IDbConnection connection = CreateConnection())
            {
                int totalItems = connection.Query<int>(countQuery, queryParams).SingleOrDefault();
                List<TEntity> items =connection.Query<TEntity>(query, queryParams).ToList();

                return PageResults(skip, take, totalItems, items);
            }
        }

        public Page<TEntity> Page(string query, string countQuery, int skip, int? take, object queryParams = null, string orderByQuery = null)
        {
            return Page(query, countQuery, skip, take ?? 10, queryParams, orderByQuery);
        }

        protected static Page<T> PageResults<T>(int skip, int take, int totalItems, List<T> items)
            where T : class 
        {
            int totalPages = take == 0 ? 1 : totalItems / take;
            int currentPage = take == 0 ? 1 : skip / take;
            if (take != 0 && (totalItems % take) != 0)
                totalPages++;

            if (take != 0 && (skip % take) == 0)
                currentPage++;

            return new Page<T>
            {
                CurrentPage = currentPage,
                ItemsPerPage = take == 0 ? totalItems : take,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = items
            };
        }

        protected PagingSql GetPagingSql(int skip, int take, string filterQuery, string orderByQuery)
        {
            if (skip < 0)
                throw new ArgumentException("Invalid skip value", "skip");
            if (take < 0)
                throw new ArgumentException("Invalid take value", "take");

            return _queryBuilder.PageQuery<TEntity>(skip, take, orderByQuery, filterQuery);
        }
    }
}