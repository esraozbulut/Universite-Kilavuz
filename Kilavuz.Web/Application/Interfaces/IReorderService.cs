using Kilavuz.Web.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kilavuz.Web.Application.Interfaces
{
    public interface IReorderService<T> where T : class, IEntity, IOrderable
    {
        Task<ServiceResult<bool>> UpdateSortOrdersAsync(Dictionary<int, int> itemOrders, int currentUserId, string currentUserRole);
    }
}
