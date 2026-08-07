using Kilavuz.Web.Data;
using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kilavuz.Web.Application
{
    public class GenericService<T> : IGenericService<T> where T : class, IEntity
    {
        private readonly IGenericRepository<T> _repository;
        private readonly Application.Interfaces.IResourceOwnershipPolicy<T> _policy;

        public GenericService(IGenericRepository<T> repository, Application.Interfaces.IResourceOwnershipPolicy<T> policy)
        {
            _repository = repository;
            _policy = policy;
        }

        public async Task<ServiceResult<IEnumerable<T>>> GetAllAsync()
        {
            var data = await _repository.GetAllAsync();
            return ServiceResult<IEnumerable<T>>.Success(data);
        }

        public async Task<ServiceResult<T>> GetByIdAsync(int id)
        {
            var data = await _repository.GetByIdAsync(id);
            if (data == null)
            {
                return ServiceResult<T>.Failure("Kayıt bulunamadı.");
            }
            return ServiceResult<T>.Success(data);
        }

        public virtual async Task<ServiceResult<int>> CreateAsync(T entity, int currentUserId, string currentUserRole)
        {
            if (entity is IAuditable auditableEntity)
            {
                auditableEntity.CreatedByUserId = currentUserId;
                auditableEntity.CreatedAt = DateTime.UtcNow;
            }

            var result = await _repository.InsertAsync(entity);
            return ServiceResult<int>.Success(result, "Kayıt başarıyla oluşturuldu.");
        }

        public virtual async Task<ServiceResult<bool>> UpdateAsync(T entity, int currentUserId, string currentUserRole)
        {
            if (!_policy.CanModify(entity, currentUserId, currentUserRole))
            {
                return ServiceResult<bool>.Failure("Bu işlem için yetkiniz bulunmamaktadır.");
            }

            if (entity is IAuditable auditableEntity)
            {
                auditableEntity.UpdatedAt = DateTime.UtcNow;
            }

            var updated = await _repository.UpdateAsync(entity);
            if (updated)
                return ServiceResult<bool>.Success(true, "Kayıt güncellendi.");
            return ServiceResult<bool>.Failure("Güncelleme başarısız.");
        }

        public async Task<ServiceResult<bool>> SoftDeleteAsync(int id, int currentUserId, string currentUserRole)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null) return ServiceResult<bool>.Failure("Kayıt bulunamadı.");

                if (!_policy.CanModify(entity, currentUserId, currentUserRole))
                {
                    return ServiceResult<bool>.Failure("Bu işlem için yetkiniz bulunmamaktadır.");
                }

                var deleted = await _repository.SoftDeleteAsync(id, currentUserId, currentUserRole);
                if (deleted)
                    return ServiceResult<bool>.Success(true, "Kayıt silindi.");
                return ServiceResult<bool>.Failure("Silme işlemi başarısız.");
            }
            catch (NotSupportedException ex)
            {
                return ServiceResult<bool>.Failure(ex.Message);
            }
        }
    }
}
