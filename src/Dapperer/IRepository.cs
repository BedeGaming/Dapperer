using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dapperer
{
    public interface IRepository<TEntity, TPrimaryKey> 
        where TEntity : class, IIdentifier<TPrimaryKey>, new() 
    {
        TEntity GetSingleOrDefault(TPrimaryKey primaryKey);
        IList<TEntity> GetByKeys(IEnumerable<TPrimaryKey> primaryKeys);
        IList<TEntity> GetAll();
        Page<TEntity> Page(int skip, int take);
        TEntity Create(TEntity entity, bool identityInsert = false);
        int Create(IEnumerable<TEntity> entities, bool identityInsert = false);
        int Update(TEntity entity);
        int Delete(TPrimaryKey primaryKey);
        int Delete(string filterQuery, object filterParams = null);
        Page<TEntity> Page(string query, string countQuery, int skip, int take, object queryParams = null, string orderByQuery = null);
        Task<TEntity> GetSingleOrDefaultAsync(TPrimaryKey primaryKey);
        Task<IList<TEntity>> GetByKeysAsync(IEnumerable<TPrimaryKey> primaryKeys);
        Task<IList<TEntity>> GetAllAsync();
        Task<Page<TEntity>> PageAsync(int skip, int take);
        Task<TEntity> CreateAsync(TEntity entity, bool identityInsert = false);
        Task<int> CreateAsync(IEnumerable<TEntity> entities, bool identityInsert = false);
        Task<int> UpdateAsync(TEntity entity);
        Task<int> DeleteAsync(TPrimaryKey primaryKey);
        Task<int> DeleteAsync(string filterQuery, object filterParams = null);
        Task<Page<TEntity>> PageAsync(string query, string countQuery, int skip, int take, object queryParams = null, string orderByQuery = null);
    }
}