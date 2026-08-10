using Dapper;
using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Domain.Interfaces;
using Kilavuz.Web.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Kilavuz.Web.Data
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntity
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly string _tableName;

        public GenericRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            // Varsayılan olarak sınıf adının sonuna 's' ekliyoruz, veritabanına uyum için
            _tableName = typeof(T).Name + "s";
            if (_tableName == "Categorys") _tableName = "Categories";
        }

        private System.Data.IDbConnection GetConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            using var connection = GetConnection();
            var hasSoftDelete = typeof(ISoftDeletable).IsAssignableFrom(typeof(T));
            var query = $"SELECT * FROM {_tableName} WHERE Id = @Id" + (hasSoftDelete ? " AND IsDeleted = 0" : "");
            return await connection.QuerySingleOrDefaultAsync<T>(query, new { Id = id });
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using var connection = GetConnection();
            var hasSoftDelete = typeof(ISoftDeletable).IsAssignableFrom(typeof(T));
            var query = $"SELECT * FROM {_tableName}" + (hasSoftDelete ? " WHERE IsDeleted = 0" : "");
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

        public async Task<bool> SoftDeleteAsync(int id, int deletedByUserId, string currentUserRole)
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                throw new NotSupportedException($"Entity of type {typeof(T).Name} does not support soft deletion.");
            }

            using var connection = GetConnection();
            var hasAuditable = typeof(IAuditable).IsAssignableFrom(typeof(T));
            
            var query = $"UPDATE {_tableName} SET IsDeleted = 1";
            if (hasAuditable)
            {
                query += ", UpdatedAt = GETUTCDATE()";
            }
            query += " WHERE Id = @Id";

            var result = await connection.ExecuteAsync(query, new { Id = id });
            return result > 0;
        }

        public async Task<bool> ReorderAsync(int id, int newSortOrder)
        {
            if (!typeof(IOrderable).IsAssignableFrom(typeof(T)))
            {
                throw new NotSupportedException($"Entity of type {typeof(T).Name} does not support ordering.");
            }

            using var connection = GetConnection();
            var query = $"UPDATE {_tableName} SET SortOrder = @SortOrder WHERE Id = @Id";
            var result = await connection.ExecuteAsync(query, new { Id = id, SortOrder = newSortOrder });
            return result > 0;
        }
    }
}
