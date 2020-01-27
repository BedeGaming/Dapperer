using System.Collections.Generic;

namespace Dapperer
{
    public interface IQueryBuilder
    {
        string GetByPrimaryKeyQuery<TEntity>()
            where TEntity : class;

        string GetByPrimaryKeysQuery<TEntity>()
            where TEntity : class;

        string GetAll<TEntity>()
            where TEntity : class;

        PagingSql PageQuery<TEntity>(long skip, long take, string orderByQuery = null, string filterQuery = null, ICollection<string> additionalTableColumns = null)
            where TEntity : class;

        string InsertQuery<TEntity, TPrimaryKey>(bool multiple = false)
             where TEntity : class;

        string InsertQuery<TEntity, TPrimaryKey>(bool multiple = false, bool identityInsert = false)
            where TEntity : class;

        string InsertQueryBatch<TEntity>(IEnumerable<TEntity> entities, string tableName, string[] columnNames, bool identityInsert = false)
             where TEntity : class;

        string UpdateQuery<TEntity>()
            where TEntity : class;

        string DeleteQuery<TEntity>()
            where TEntity : class;

        string DeleteQuery<TEntity>(string filterQuery)
            where TEntity : class;

        ITableInfoBase GetBaseTableInfo<TEntity>()
            where TEntity : class;
    }
}