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

        PagingSql PageQuery<TEntity>(long skip, long take, string orderByQuery = null, string filterQuery = null)
            where TEntity : class;

        string InsertQuery<TEntity, TPrimaryKey>(bool multiple = false)
             where TEntity : class;

        string InsertQuery<TEntity, TPrimaryKey>(bool multiple = false, bool identityInsert = false)
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