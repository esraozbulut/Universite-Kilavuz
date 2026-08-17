using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Data;
using Kilavuz.Web.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kilavuz.Web.Application
{
    public class ReorderService<T> : IReorderService<T> where T : class, IEntity, IOrderable
    {
        private readonly IGenericRepository<T> _repository;
        private readonly IResourceOwnershipPolicy<T> _policy;

        public ReorderService(IGenericRepository<T> repository, IResourceOwnershipPolicy<T> policy)
        {
            _repository = repository;
            _policy = policy;
        }

        public async Task<ServiceResult<bool>> UpdateSortOrdersAsync(Dictionary<int, int> itemOrders, int currentUserId, string currentUserRole)
        {
            if (itemOrders == null || itemOrders.Count == 0)
                return ServiceResult<bool>.Failure("Güncellenecek veri bulunamadı.");

            // Ön kontrol: Kullanıcının tüm bu nesneleri düzenleme yetkisi var mı?
            foreach (var kvp in itemOrders)
            {
                var id = kvp.Key;
                var entity = await _repository.GetByIdAsync(id);
                
                if (entity == null)
                    return ServiceResult<bool>.Failure($"ID: {id} bulunamadı.");

                if (!await _policy.CanModifyAsync(entity, currentUserId, currentUserRole))
                {
                    return ServiceResult<bool>.Failure("Bu işlem için yetkiniz bulunmamaktadır (Başka bir kullanıcının içeriği değiştirilemez).");
                }
            }

            // Yetki kontrolü başarılı, şimdi güncelleyebiliriz
            bool allSuccess = true;
            foreach (var kvp in itemOrders)
            {
                var success = await _repository.ReorderAsync(kvp.Key, kvp.Value);
                if (!success) allSuccess = false;
            }

            if (allSuccess)
                return ServiceResult<bool>.Success(true, "Sıralama başarıyla güncellendi.");
            
            return ServiceResult<bool>.Failure("Sıralama güncellenirken bazı kayıtlarda hata oluştu.");
        }
    }
}
