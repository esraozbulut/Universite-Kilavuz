using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kilavuz.Web.Application
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        
        public static ServiceResult<T> Success(T data, string message = null) => new ServiceResult<T> { IsSuccess = true, Data = data, Message = message };
        public static ServiceResult<T> Failure(string message) => new ServiceResult<T> { IsSuccess = false, Message = message };
    }

    public interface IGenericService<T> where T : class, IEntity
    {
        Task<ServiceResult<T>> GetByIdAsync(int id);
        Task<ServiceResult<IEnumerable<T>>> GetAllAsync();
        Task<ServiceResult<int>> CreateAsync(T entity, int currentUserId);
        Task<ServiceResult<bool>> UpdateAsync(T entity, int currentUserId);
        Task<ServiceResult<bool>> SoftDeleteAsync(int id, int currentUserId);
    }
}
