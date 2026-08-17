using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Enums;
using Kilavuz.Web.Domain.Interfaces;
using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Data;
using Dapper;
using System.Threading.Tasks;

namespace Kilavuz.Web.Application
{
    public class ResourceOwnershipPolicy<T> : IResourceOwnershipPolicy<T> where T : class, IEntity
    {
        private readonly IDbConnectionFactory _db;

        public ResourceOwnershipPolicy(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<bool> CanModifyAsync(T entity, int currentUserId, string currentUserRole)
        {
            return await CheckAccessAsync(entity, currentUserId, currentUserRole);
        }

        public async Task<bool> CanCreateAsync(T entity, int currentUserId, string currentUserRole)
        {
            return await CheckAccessAsync(entity, currentUserId, currentUserRole);
        }

        private async Task<bool> CheckAccessAsync(T entity, int currentUserId, string currentUserRole)
        {
            // 1. SuperAdmin her şeyi değiştirebilir
            if (currentUserRole == UserRoleType.SuperAdmin.ToString()) 
                return true;
            
            // 2. Kullanici (standart üye) içerik değiştiremez
            if (currentUserRole == UserRoleType.Kullanici.ToString()) 
                return false;

            // 3. Yetkili rolü departman bazlı kontrol
            if (entity is IDepartmentOwned deptOwned)
            {
                if (!deptOwned.DepartmentId.HasValue) 
                    return false; // Ana üniversite kılavuzu (SuperAdmin yönetir)
                
                return await HasDepartmentAccessAsync(deptOwned.DepartmentId.Value, currentUserId);
            }

            if (entity is Category cat)
            {
                var deptId = await GetDeptIdByAppIdAsync(cat.ApplicationId);
                if (deptId.HasValue) 
                    return await HasDepartmentAccessAsync(deptId.Value, currentUserId);
                
                return false;
            }

            if (entity is Page page)
            {
                var deptId = await GetDeptIdByCategoryIdAsync(page.CategoryId);
                if (deptId.HasValue) 
                    return await HasDepartmentAccessAsync(deptId.Value, currentUserId);
                
                return false;
            }

            // Geri kalan (ContentPermission vb.) işlemleri SuperAdmin dışındakilere yasakla
            // IAuditable fallback:
            if (entity is IAuditable auditableEntity)
            {
                return auditableEntity.CreatedByUserId == currentUserId;
            }

            return false;
        }

        private async Task<bool> HasDepartmentAccessAsync(int departmentId, int userId)
        {
            using var connection = _db.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<bool>(
                "SELECT CAST(1 AS BIT) FROM DepartmentUsers WHERE DepartmentId = @DeptId AND UserId = @UserId", 
                new { DeptId = departmentId, UserId = userId });
        }

        private async Task<int?> GetDeptIdByAppIdAsync(int appId)
        {
            using var connection = _db.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<int?>("SELECT DepartmentId FROM Applications WHERE Id = @Id", new { Id = appId });
        }

        private async Task<int?> GetDeptIdByCategoryIdAsync(int categoryId)
        {
            using var connection = _db.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<int?>("SELECT a.DepartmentId FROM Applications a INNER JOIN Categories c ON a.Id = c.ApplicationId WHERE c.Id = @Id", new { Id = categoryId });
        }
    }
}
