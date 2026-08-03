using Kilavuz.Web.Data;
using Kilavuz.Web.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kilavuz.Web.Application
{
    public class GenericService<T> : IGenericService<T> where T : class, IEntity
    {
        private readonly IGenericRepository<T> _repository;

        public GenericService(IGenericRepository<T> repository)
        {
            _repository = repository;
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

        public async Task<ServiceResult<int>> CreateAsync(T entity, int currentUserId)
        {
            // İlgili property'leri (CreatedByUserId vs.) reflection ile doldurabiliriz.
            var createdByProp = typeof(T).GetProperty("CreatedByUserId");
            if (createdByProp != null && createdByProp.CanWrite)
            {
                createdByProp.SetValue(entity, currentUserId);
            }

            var result = await _repository.InsertAsync(entity);
            return ServiceResult<int>.Success(result, "Kayıt başarıyla oluşturuldu.");
        }

        public async Task<ServiceResult<bool>> UpdateAsync(T entity, int currentUserId)
        {
            var updated = await _repository.UpdateAsync(entity);
            if (updated)
                return ServiceResult<bool>.Success(true, "Kayıt güncellendi.");
            return ServiceResult<bool>.Failure("Güncelleme başarısız.");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id, int currentUserId)
        {
            // Soft delete işlemi
            var deleted = await _repository.SoftDeleteAsync(id, currentUserId);
            if (deleted)
                return ServiceResult<bool>.Success(true, "Kayıt silindi.");
            return ServiceResult<bool>.Failure("Silme işlemi başarısız.");
        }
    }
}
