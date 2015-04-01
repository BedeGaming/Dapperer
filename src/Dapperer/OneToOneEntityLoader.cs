using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;

namespace Dapperer
{
    public class OneToOneEntityLoader<TEntity, TPrimaryKey, TForeignEntity, TForeignEntityPrimaryKey> : EntityLoader<TEntity, TPrimaryKey>
        where TEntity : class, IIdentifier<TPrimaryKey>, new()
        where TForeignEntity : class, IIdentifier<TForeignEntityPrimaryKey>, new()
    {
        private readonly Func<IDbConnection> _getConnection;
        private readonly string _sql;
        private readonly Func<TEntity, TForeignEntityPrimaryKey> _getForeignKey;
        private readonly Action<TEntity, TForeignEntity> _setter;

        public OneToOneEntityLoader(
            Func<IDbConnection> getConnection,
            IQueryBuilder queryBuilder,
            Expression<Func<TEntity, TForeignEntityPrimaryKey>> foreignKey,
            Expression<Func<TEntity, TForeignEntity>> foreignEntity)
        {
            _getConnection = getConnection;
            _sql = queryBuilder.GetByPrimaryKeysQuery<TForeignEntity>();
            _getForeignKey = foreignKey.Compile();
            _setter = GetSetter(foreignEntity);
        }

        public void Populate(params TEntity[] entities)
        {
            IEnumerable<TForeignEntityPrimaryKey> keys = GetKeys(entities);

            IList<TForeignEntity> foreignEntities;
            using (IDbConnection connection = _getConnection())
            {
                foreignEntities = connection.Query<TForeignEntity>(_sql, new { Keys = keys }).ToList();
            }

            PopulateEntities(entities, foreignEntities);
        }

        public async Task PopulateAsync(params TEntity[] entities)
        {
            IEnumerable<TForeignEntityPrimaryKey> keys = GetKeys(entities);

            IList<TForeignEntity> foreignEntities;
            using (IDbConnection connection = _getConnection())
            {
                foreignEntities = (await connection.QueryAsync<TForeignEntity>(_sql, new { Keys = keys }).ConfigureAwait(false)).ToList();
            }

            PopulateEntities(entities, foreignEntities);
        }

        private void PopulateEntities(IEnumerable<TEntity> entities, IList<TForeignEntity> foreignEntities)
        {
            foreach (TEntity entity in entities)
            {
                TForeignEntityPrimaryKey foreignEntityKey = _getForeignKey(entity);
                _setter(entity, foreignEntities.FirstOrDefault(fe => Equals(foreignEntityKey, fe.GetIdentity())));
            }
        }

        private IEnumerable<TForeignEntityPrimaryKey> GetKeys(IEnumerable<TEntity> entities)
        {
            return entities.Select(_getForeignKey);
        }
    }
}