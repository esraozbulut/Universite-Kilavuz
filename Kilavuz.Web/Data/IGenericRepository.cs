using Kilavuz.Web.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kilavuz.Web.Data
{
    public interface IGenericRepository<T> where T : class, IEntity
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<int> InsertAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> SoftDeleteAsync(int id, int deletedByUserId);
        Task<bool> ReorderAsync(int id, int newSortOrder);
    }
}
