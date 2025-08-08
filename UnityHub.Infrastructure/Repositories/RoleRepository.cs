using System.Data;
using System.Data.SqlClient;
using UnityHub.Domain.Entities;
using UnityHub.Application.Interfaces.Repositories;
using UnityHub.Infrastructure.DbConnection;

namespace UnityHub.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly DbConnectionFactory _db;
        private readonly string _connStr;

        public RoleRepository(DbConnectionFactory dbConnectionFactory)
        {
            _db = dbConnectionFactory;
            _connStr = dbConnectionFactory.ConnectionString;
        }

        public async Task AddAsync(Role role)
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_InsertRole";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Name", role.Name);
            cmd.Parameters.AddWithValue("@Description", (object?)role.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)role.CreatedBy ?? DBNull.Value);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Role role)
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_UpdateRole";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", role.Id);
            cmd.Parameters.AddWithValue("@Name", role.Name);
            cmd.Parameters.AddWithValue("@Description", (object?)role.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedBy", (object?)role.UpdatedBy ?? DBNull.Value);
            /*cmd.Parameters.AddWithValue("@CreatedTime", role.CreatedTime);
            cmd.Parameters.AddWithValue("@UpdatedTime", role.UpdatedTime);*/

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(long roleId, long updatedBy)
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_DeleteRole";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", roleId);
            cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GetRoleById";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            return reader.Read() ? Map(reader) : null;
        }

        public async Task<IEnumerable<Role>> GetAllActivePagedAsync(int pageNumber, int pageSize, DateTime? start, DateTime? end)
        {
            var list = new List<Role>();
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GetAllActiveRolesPaged";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);
            cmd.Parameters.AddWithValue("@StartDate", (object?)start ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EndDate", (object?)end ?? DBNull.Value);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        private Role Map(IDataReader r) => new Role
        {
            Id = (int)r["Id"],
            Name = r["Name"].ToString()!,
            Description = r["Description"] as string,
            IsStatic = r["IsStatic"] != DBNull.Value && (bool)r["IsStatic"],
            IsDefault = r["IsDefault"] != DBNull.Value && (bool)r["IsDefault"],
            CreatedTime = (DateTime)r["CreatedTime"],
            CreatedBy = r["CreatedBy"] as long?,
            UpdatedTime = r["UpdatedTime"] as DateTime?,
            UpdatedBy = r["UpdatedBy"] as long?,
            IsDeleted = r["IsDeleted"] != DBNull.Value && (bool)r["IsDeleted"]
        };
    }
}
