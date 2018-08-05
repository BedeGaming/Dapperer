using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;

namespace Dapperer
{
    public class OneToManyEntityLoader<TEntity, TPrimaryKey, TForeignEntity, TForeignEntityPrimaryKey> 
        : EntityLoader<TEntity, TPrimaryKey>
        where TEntity : class, IIdentifier<TPrimaryKey>, new()
        where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
    {
        private readonly Func<IDbConnection> _getConnection;
        private readonly string _sql;
        private readonly Action<TEntity, IList<TForeignEntity>> _setter;
        private readonly Func<TForeignEntity, TPrimaryKey> _getForeignKey;

        public OneToManyEntityLoader(Func<IDbConnection> getConnection, IQueryBuilder queryBuilder, Expression<Func<TForeignEntity, TPrimaryKey>> foreignKey,
            Expression<Func<TEntity, IList<TForeignEntity>>> foreignEntityCollection)
        {
            _getConnection = getConnection;

            var foreignTableInfo = queryBuilder.GetBaseTableInfo<TForeignEntity>();
            var foreignKeyColum = GetForeignKeyColumn<TForeignEntity, TForeignEntityPrimaryKey>(foreignKey);
            _sql = $"SELECT * FROM {foreignTableInfo.TableName} WHERE {foreignKeyColum} IN @ForeignKeys";
            
            _setter = GetSetter(foreignEntityCollection);
            _getForeignKey = foreignKey.Compile();
        }

        public void Populate(params TEntity[] entities)
        {
            var keys = GetKeys(entities);
            
            IList<TForeignEntity> foreignEntities;
            using (var connection = _getConnection())
            {
                foreignEntities = connection.Query<TForeignEntity>(_sql, new { ForeignKeys = keys }).ToList();
            }

            PopulateEntities(entities, foreignEntities);
        }

        public async Task PopulateAsync(params TEntity[] entities)
        {
            var keys = GetKeys(entities);

            IList<TForeignEntity> foreignEntities;
            using (var connection = _getConnection())
            {
                foreignEntities = (await connection.QueryAsync<TForeignEntity>(_sql, new { ForeignKeys = keys }).ConfigureAwait(false)).ToList();
            }

            PopulateEntities(entities, foreignEntities);
        }

        private void PopulateEntities(IEnumerable<TEntity> entities, IList<TForeignEntity> foreignEntities)
        {
            foreach (var entity in entities)
            {
                var key = entity.GetIdentity();
                _setter(entity, foreignEntities.Where(se => Equals(_getForeignKey(se), key)).ToList());
            }
        }

        private static IEnumerable<TPrimaryKey> GetKeys(IEnumerable<TEntity> entities)
        {
            return entities.Select(e => e.GetIdentity());
        }

        private static string GetForeignKeyColumn<TSubEntity, TSubEntityPrimaryKey>(Expression<Func<TSubEntity, TPrimaryKey>> foreignKey)
            where TSubEntity : class, IIdentifier<TSubEntityPrimaryKey>, new()
        {
            var memberExpr = foreignKey.Body as MemberExpression;
            if (memberExpr == null)
            {
                if (foreignKey.Body is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
                    memberExpr = unaryExpr.Operand as MemberExpression;
            }

            if (memberExpr != null && memberExpr.Member.MemberType == MemberTypes.Property)
            {
                return memberExpr.Member.Name;
            }

            throw new ArgumentException("No foreign key property reference expression was found.", nameof(foreignKey));
        }
    }
}