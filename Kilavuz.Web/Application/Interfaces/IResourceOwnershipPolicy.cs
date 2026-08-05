using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Application.Interfaces
{
    public interface IResourceOwnershipPolicy<T> where T : class, IEntity
    {
        bool CanModify(T entity, int currentUserId, string currentUserRole);
    }
}
