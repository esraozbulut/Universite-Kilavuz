using Dapper;
using Kilavuz.Web.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Kilavuz.Web.Data
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntity
    {
        private readonly IConfiguration _configuration;
        private readonly string _tableName;

        public GenericRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            // Varsayılan olarak sınıf adının sonuna 's' ekliyoruz, veritabanına uyum için
            // Gerçek uygulamada [Table("Name")] attribute okuması da eklenebilir.
            _tableName = typeof(T).Name + "s";
            if (_tableName == "Categorys") _tableName = "Categories";
            if (_tableName == "ContentPermissions") _tableName = "ContentPermissions";
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<T> GetByIdAsync(int id)
        {
            using var connection = GetConnection();
            var query = $"SELECT * FROM {_tableName} WHERE Id = @Id AND IsDeleted = 0";
            return await connection.QuerySingleOrDefaultAsync<T>(query, new { Id = id });
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using var connection = GetConnection();
            var query = $"SELECT * FROM {_tableName} WHERE IsDeleted = 0";
            return await connection.QueryAsync<T>(query);
        }

        public async Task<int> InsertAsync(T entity)
        {
            using var connection = GetConnection();
            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
            var columns = string.Join(",", properties.Select(p => p.Name));
            var parameters = string.Join(",", properties.Select(p => "@" + p.Name));

            var query = $"INSERT INTO {_tableName} ({columns}) OUTPUT INSERTED.Id VALUES ({parameters})";
            return await connection.QuerySingleAsync<int>(query, entity);
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            using var connection = GetConnection();
            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id" && p.Name != "CreatedAt");
            var setClause = string.Join(",", properties.Select(p => $"{p.Name} = @{p.Name}"));

            var query = $"UPDATE {_tableName} SET {setClause} WHERE Id = @Id";
            var result = await connection.ExecuteAsync(query, entity);
            return result > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            var query = $"DELETE FROM {_tableName} WHERE Id = @Id";
            var result = await connection.ExecuteAsync(query, new { Id = id });
            return result > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id, int deletedByUserId)
        {
            using var connection = GetConnection();
            // Bu kolonların tüm entity'lerde olup olmadığını kontrol etmek gerek,
            // ISoftDeletable interface'i eklenebilir. Şimdilik Reflection yerine basit model varsayılıyor.
            var query = $"UPDATE {_tableName} SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            var result = await connection.ExecuteAsync(query, new { Id = id, UpdatedAt = DateTime.UtcNow });
            return result > 0;
        }

        public async Task<bool> ReorderAsync(int id, int newSortOrder)
        {
            using var connection = GetConnection();
            var query = $"UPDATE {_tableName} SET SortOrder = @SortOrder WHERE Id = @Id";
            var result = await connection.ExecuteAsync(query, new { Id = id, SortOrder = newSortOrder });
            return result > 0;
        }
    }
}
