using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kilavuz.Web.Data
{
    public interface IGenericRepository<T> where T : class, IEntity
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync(object? filter = null);
        Task<int> InsertAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> SoftDeleteAsync(int id, int deletedByUserId, string currentUserRole);
        Task<bool> ReorderAsync(int id, int newSortOrder);
        Task<int> GetNextSortOrderAsync(object? filter = null);
    }
}
