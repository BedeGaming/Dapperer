using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;

namespace Dapperer
{
    /// <summary>
    /// Generic repository for basic CRUD operation
    /// Extend it per specify entity types in order to add custom methods
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TPrimaryKey">Primary key type either</typeparam>
    public abstract partial class Repository<TEntity, TPrimaryKey>
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

        public virtual TEntity Create(TEntity entity)
        {
            string sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>();

            using (IDbConnection connection = CreateConnection())
            {
                if (_queryBuilder.GetBaseTableInfo<TEntity>().AutoIncrement)
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
            string sql = _queryBuilder.InsertQuery<TEntity, TPrimaryKey>(true);

            using (IDbConnection connection = CreateConnection())
            {
                connection.Open();
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

        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (entities == null)
                return;

            Populate(foreignKey, foreignEntity, entities.ToArray());
        }

        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection,
            IEnumerable<TEntity> entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (entities == null)
                return;

            Populate<TForeignEntity, TForeignEntityPrimaryKey>(foreignKey, foreignEntityCollection, entities.ToArray());
        }

        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity,
            params TEntity[] entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (!entities.Any())
                return;

            string sql = _queryBuilder.GetByPrimaryKeysQuery<TForeignEntity>();
            Func<TEntity, TForeignEntityPrimaryKey> getForeignKey = foreignKey.Compile();
            IEnumerable<TForeignEntityPrimaryKey> keys = entities.Select(getForeignKey);

            IList<TForeignEntity> foreignEntities;
            using (IDbConnection connection = CreateConnection())
            {
                foreignEntities = connection.Query<TForeignEntity>(sql, new { Keys = keys }).ToList();
            }

            Action<TEntity, TForeignEntity> setter = GetSetter(foreignEntity);

            foreach (TEntity entity in entities)
            {
                TForeignEntityPrimaryKey foreignEntityKey = getForeignKey(entity);
                setter(entity, foreignEntities.FirstOrDefault(fe => Equals(foreignEntityKey, fe.GetIdentity())));
            }
        }

        protected void Populate<TForeignEntity, TForeignEntityPrimaryKey>(Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection,
            params TEntity[] entities)
            where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
        {
            if (!entities.Any())
                return;

            ITableInfoBase foreignTableInfo = _queryBuilder.GetBaseTableInfo<TForeignEntity>();
            string foreignKeyColum = GetForeignKeyColumn<TForeignEntity, TForeignEntityPrimaryKey>(foreignKey);

            string sql = string.Format("SELECT * FROM {0} WHERE {1} IN @ForeignKeys", foreignTableInfo.TableName, foreignKeyColum);
            IEnumerable<TPrimaryKey> keys = entities.Select(e => e.GetIdentity());
            IList<TForeignEntity> foreignEntities;

            using (IDbConnection connection = CreateConnection())
            {
                foreignEntities = connection.Query<TForeignEntity>(sql, new { ForeignKeys = keys }).ToList();
            }

            Action<TEntity, IList<TForeignEntity>> setter = GetSetter(foreignEntityCollection);
            Func<TForeignEntity, TPrimaryKey> getForeignKey = foreignKey.Compile();

            foreach (TEntity entity in entities)
            {
                TPrimaryKey key = entity.GetIdentity();
                setter(entity, foreignEntities.Where(se => Equals(getForeignKey(se), key)).ToList());
            }
        }

        private static Action<TEntity, TReferenceEntity> GetSetter<TReferenceEntity>(Expression<Func<TEntity, TReferenceEntity>> foreignEntity)
            where TReferenceEntity : class
        {
            ParameterExpression valueParameterExpression = Expression.Parameter(typeof(TReferenceEntity));
            Expression targetExpression = foreignEntity.Body is UnaryExpression ? ((UnaryExpression)foreignEntity.Body).Operand : foreignEntity.Body;

            Expression<Action<TEntity, TReferenceEntity>> assign = Expression.Lambda<Action<TEntity, TReferenceEntity>>(
                Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
                foreignEntity.Parameters.Single(),
                valueParameterExpression);

            return assign.Compile();
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

        protected static Page<T> PageResults<T>(int skip, int take, int totalItems, List<T> items)
            where T : class 
        {
            int totalPages = totalItems / take;
            int currentPage = skip / take;
            if ((totalItems % take) != 0)
                totalPages++;

            if ((skip % take) == 0)
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

        private static string GetForeignKeyColumn<TSubEntity, TSubEntityPrimaryKey>(Expression<Func<TSubEntity, TPrimaryKey>> foreignKey)
            where TSubEntity : class, IIdentifier<TSubEntityPrimaryKey>, new()
        {
            var memberExpr = foreignKey.Body as MemberExpression;
            if (memberExpr == null)
            {
                var unaryExpr = foreignKey.Body as UnaryExpression;
                if (unaryExpr != null && unaryExpr.NodeType == ExpressionType.Convert)
                    memberExpr = unaryExpr.Operand as MemberExpression;
            }

            if (memberExpr != null && memberExpr.Member.MemberType == MemberTypes.Property)
            {
                return memberExpr.Member.Name;
            }

            throw new ArgumentException("No foreign key property reference expression was found.", "foreignKey");
        }

        private PagingSql GetPagingSql(int skip, int take, string filterQuery, string orderByQuery)
        {
            if (skip < 0)
                throw new ArgumentException("Invalid skip value", "skip");
            if (take <= 0)
                throw new ArgumentException("Invalid take value", "take");

            PagingSql pagingSql = _queryBuilder.PageQuery<TEntity>(skip, take, orderByQuery, filterQuery);
            return pagingSql;
        }
    }
}