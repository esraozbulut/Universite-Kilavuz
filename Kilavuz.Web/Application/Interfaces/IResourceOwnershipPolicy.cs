using Kilavuz.Web.Domain.Interfaces;
using System.Threading.Tasks;

namespace Kilavuz.Web.Application.Interfaces
{
    public interface IResourceOwnershipPolicy<T> where T : class, IEntity
    {
        Task<bool> CanModifyAsync(T entity, int currentUserId, string currentUserRole);
        Task<bool> CanCreateAsync(T entity, int currentUserId, string currentUserRole);
    }
}
