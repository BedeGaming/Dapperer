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

        protected IDbConnection CreateConnection() => _dbFactory.CreateConnection();

        public virtual TEntity GetSingleOrDefault(TPrimaryKey primaryKey)
        {
            var sql = _queryBuilder.GetByPrimaryKeyQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return connection.Query<TEntity>(sql, new { Key = primaryKey }).SingleOrDefault();
            }
        }

        public virtual IList<TEntity> GetByKeys(IEnumerable<TPrimaryKey> primaryKeys)
        {
            var sql = _queryBuilder.GetByPrimaryKeysQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return connection.Query<TEntity>(sql, new { Keys = primaryKeys }).ToList();
            }
        }

        public virtual IList<TEntity> GetAll()
        {
            var sql = _queryBuilder.GetAll<TEntity>();

            using (var connection = CreateConnection())
            {
                return connection.Query<TEntity>(sql).ToList();
            }
        }

        public virtual Page<TEntity> Page(int skip, int take) => Page(skip, take, null);

        public virtual TEntity Create(TEntity entity)
        {
            var sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>();

            using (var connection = CreateConnection())
            {
                if (_queryBuilder.GetBaseTableInfo<TEntity>().AutoIncrement)
                {
                    var identity = connection.Query<TPrimaryKey>(sql, entity).SingleOrDefault();
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
            var sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>();

            using (var connection = CreateConnection())
            {
                return connection.Execute(sql, entities);
            }
        }

        public virtual int Update(TEntity entity)
        {
            var sql = _queryBuilder.UpdateQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return connection.Execute(sql, entity);
            }
        }

        public virtual int Delete(TPrimaryKey primaryKey)
        {
            var sql = _queryBuilder.DeleteQuery<TEntity>();

            using (var connection = CreateConnection())
            {
                return connection.Execute(sql, new { Key = primaryKey });
            }
        }

        public virtual int Delete(string filterQuery, object filterParams = null)
        {
            var sql = _queryBuilder.DeleteQuery<TEntity>(filterQuery);

            using (var connection = CreateConnection())
            {
                return connection.Execute(sql, filterParams);
            }
        }

        protected ITableInfoBase GetTableInfo() => _queryBuilder.GetBaseTableInfo<TEntity>();

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
            var pagingSql = GetPagingSql(skip, take, filterQuery, orderByQuery);

            using (var connection = CreateConnection())
            {
                var totalItems = connection.Query<int>(pagingSql.Count, filterParams).SingleOrDefault();
                var items = connection.Query<TEntity>(pagingSql.Items, filterParams).ToList();

                return PageResults(skip, take, totalItems, items);
            }
        }

        public Page<TEntity> Page(string query, string countQuery, int skip, int take, object queryParams = null, string orderByQuery = null)
        {
            using (var connection = CreateConnection())
            {
                var totalItems = connection.Query<int>(countQuery, queryParams).SingleOrDefault();
                var items =connection.Query<TEntity>(query, queryParams).ToList();

                return PageResults(skip, take, totalItems, items);
            }
        }

        protected static Page<T> PageResults<T>(int skip, int take, int totalItems, List<T> items)
            where T : class 
        {
            var totalPages = totalItems / take;
            var currentPage = skip / take;
            if ((totalItems % take) != 0)
                totalPages++;

            if (skip % take == 0)
                currentPage++;

            return new Page<T>
            {
                CurrentPage = currentPage,
                ItemsPerPage = take,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Items = items
            };
        }

        protected PagingSql GetPagingSql(int skip, int take, string filterQuery, string orderByQuery)
        {
            if (skip < 0)
                throw new ArgumentException("Invalid skip value", nameof(skip));
            if (take <= 0)
                throw new ArgumentException("Invalid take value", nameof(take));

            return _queryBuilder.PageQuery<TEntity>(skip, take, orderByQuery, filterQuery);
        }
    }
}