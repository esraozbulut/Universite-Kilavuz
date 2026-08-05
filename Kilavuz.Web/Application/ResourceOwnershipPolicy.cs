using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Enums;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Application
{
    public class ResourceOwnershipPolicy<T> : IResourceOwnershipPolicy<T> where T : class, IEntity
    {
        public bool CanModify(T entity, int currentUserId, string currentUserRole)
        {
            // 1. SuperAdmin her şeyi değiştirebilir
            if (currentUserRole == UserRoleType.SuperAdmin.ToString()) 
                return true;
            
            // 2. Kullanici (standart üye) içerik değiştiremez
            if (currentUserRole == UserRoleType.Kullanici.ToString()) 
                return false;

            // 3. Yetkili rolündekiler SADECE kendi ürettikleri içeriği değiştirebilir
            if (entity is IAuditable auditableEntity)
            {
                return auditableEntity.CreatedByUserId == currentUserId;
            }

            // IAuditable olmayan (veya tanımlanmayan) nesneler için varsayılan davranış: Red
            return false;
        }
    }
}
