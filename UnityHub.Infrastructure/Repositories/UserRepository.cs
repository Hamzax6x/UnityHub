using System.Data;
using System.Data.SqlClient;
using UnityHub.Domain.Entities;
using UnityHub.Application.Interfaces.Repositories;
using UnityHub.Infrastructure.DbConnection;
using UnityHub.Application.DTOs;
using Microsoft.AspNetCore.Identity; // Needed for PasswordHasher

namespace UnityHub.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;
        private readonly string _connectionString;
        private readonly IAuditLogRepository _auditLogRepository; // Ensure this is correctly injected

        public UserRepository(DbConnectionFactory dbConnectionFactory, IAuditLogRepository auditLogRepository)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _connectionString = dbConnectionFactory.ConnectionString;
            _auditLogRepository = auditLogRepository;
        }

        public async Task AddAsync(User user)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "sp_InsertUser";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@UserName", user.UserName));
            command.Parameters.Add(new SqlParameter("@Email", (object?)user.Email ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@PasswordHash", (object?)user.PasswordHash ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@PhoneNumber", (object?)user.PhoneNumber ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@ProfilePictureUrl", (object?)user.ProfilePictureUrl ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@CreatedBy", (object?)user.CreatedBy ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@EmailConfirmationToken", (object?)user.EmailConfirmationToken ?? DBNull.Value));

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Username or email already exists.");
            }
        }

        public async Task<long> CreateUserWithRolesAsync(UserCreateDto dto, string emailConfirmationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_CreateUserWithRoles", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@UserName", dto.UserName);
            cmd.Parameters.AddWithValue("@Email", dto.Email ?? (object)DBNull.Value);
            var hasher = new PasswordHasher<string>();
            var hashedPassword = hasher.HashPassword(null, dto.Password);

            cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            cmd.Parameters.AddWithValue("@PhoneNumber", dto.PhoneNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePictureUrl", dto.ProfilePictureUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);
            cmd.Parameters.AddWithValue("@EmailConfirmationToken", emailConfirmationToken ?? (object)DBNull.Value);

            var table = new DataTable();
            table.Columns.Add("RoleId", typeof(int));
            foreach (var id in dto.RoleIds.Distinct())
            {
                table.Rows.Add(id);
            }

            var tvpParam = new SqlParameter("@RoleIds", SqlDbType.Structured)
            {
                TypeName = "dbo.IntList",
                Value = table
            };
            cmd.Parameters.Add(tvpParam);

            var userIdOut = new SqlParameter("@NewUserId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(userIdOut);

            await connection.OpenAsync();
            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Username or email already exists.");
            }
            if (userIdOut.Value != null && userIdOut.Value != DBNull.Value)
            {
                return (long)userIdOut.Value;
            }
            else
            {
                throw new Exception("Failed to retrieve new user ID after creation. Stored procedure might not have returned a user ID.");
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT * FROM Users WHERE Email = @Email AND IsDeleted = 0";
            command.CommandType = CommandType.Text;
            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            return reader.Read() ? MapReaderToUser(reader) : null;
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "sp_GetUserById";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Id", id));

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            return reader.Read() ? MapReaderToUser(reader) : null;
        }

        public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "sp_GetUserByEmailOrUsername";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@EmailOrUsername", emailOrUsername));

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            return reader.Read() ? MapReaderToUser(reader) : null;
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersPagedAsync(int pageNumber, int pageSize, DateTime? startDate, DateTime? endDate)
        {
            var users = new List<User>();

            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "sp_GetAllActiveUsersPaged";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@PageNumber", pageNumber));
            command.Parameters.Add(new SqlParameter("@PageSize", pageSize));
            command.Parameters.Add(new SqlParameter("@StartDate", (object?)startDate ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@EndDate", (object?)endDate ?? DBNull.Value));

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                users.Add(MapReaderToUser(reader));

            return users;
        }

        public async Task UpdateAsync(User user)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "sp_UpdateUser";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Id", user.Id));
            command.Parameters.Add(new SqlParameter("@UserName", user.UserName));
            command.Parameters.Add(new SqlParameter("@Email", (object?)user.Email ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@PhoneNumber", (object?)user.PhoneNumber ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@ProfilePictureUrl", (object?)user.ProfilePictureUrl ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@UpdatedBy", (object?)user.UpdatedBy ?? DBNull.Value));

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(long id, long updatedBy)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "sp_DeleteUser";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Id", id));
            command.Parameters.Add(new SqlParameter("@UpdatedBy", updatedBy));

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task ResetAccessFailedCountAsync(long userId)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "UPDATE Users SET AccessFailedCount = 0 WHERE Id = @Id";
            command.CommandType = CommandType.Text;
            command.Parameters.Add(new SqlParameter("@Id", userId));

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task IncrementAccessFailedCountAsync(long userId)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
DECLARE @NewAccessFailedCount INT;

-- Get current failed count
SELECT @NewAccessFailedCount = AccessFailedCount + 1 FROM Users WHERE Id = @UserId;

UPDATE Users
SET
    AccessFailedCount = @NewAccessFailedCount,

    IsActive = CASE
        WHEN @NewAccessFailedCount = 3 THEN 0
        WHEN @NewAccessFailedCount >= 5 THEN 0
        ELSE IsActive
    END,

    LockoutEnd = CASE
        WHEN @NewAccessFailedCount = 3 THEN DATEADD(MINUTE, 2, GETUTCDATE())
        ELSE LockoutEnd
    END,

    LockoutEnabled = CASE
        WHEN @NewAccessFailedCount = 3 THEN 1
        ELSE LockoutEnabled
    END,

    UpdatedTime = GETDATE()
WHERE Id = @UserId;
";
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeactivateUserAsync(long userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("sp_DeactivateUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", userId);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task ReactivateUserAfterLockoutAsync(long userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("sp_ReactivateUserAfterLockout", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserId", userId);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AdminUnblockUserAsync(long userId, string ip, string browserInfo)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
        UPDATE Users
        SET
            AccessFailedCount = 0,
            IsActive = 1,
            LockoutEnd = NULL,
            LockoutEnabled = 1,
            UpdatedTime = GETDATE()
        WHERE Id = @UserId;
    ";

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            await _auditLogRepository.LogAsync(userId, "User Activated", "Info", ip, browserInfo);
        }

        public async Task UpdateEmailConfirmationTokenAsync(long userId, string token)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Users
                SET EmailConfirmationToken = @Token,
                    UpdatedTime = GETDATE()
                WHERE Id = @UserId AND IsDeleted = 0";

            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task ConfirmUserEmailAsync(long userId)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Users
                SET EmailConfirmed = 1,
                    EmailConfirmationToken = NULL,
                    UpdatedTime = GETDATE()
                WHERE Id = @UserId AND IsDeleted = 0";

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task SavePasswordResetTokenAsync(long userId, string token, DateTime expiry)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_SavePasswordResetToken", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Expiry", expiry);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePasswordAsync(long userId, string newPasswordHash)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdatePasswordAndClearResetToken", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // --- NEWLY ADDED METHODS TO IMPLEMENT THE IUserRepository INTERFACE ---

        // This method was previously named UpdatePasswordAsync, but the interface had ResetPasswordAsync.
        // It's better to align names or ensure the correct one is called.
        // If sp_UpdatePasswordAndClearResetToken does both, then this is fine.
        public async Task ResetPasswordAsync(long userId, string newPasswordHash)
        {
            // You can reuse the logic from UpdatePasswordAsync if it fits the purpose.
            // Assuming `sp_UpdatePasswordAndClearResetToken` handles resetting the password
            // and clearing the token, we can call it here.
            await UpdatePasswordAsync(userId, newPasswordHash);
            // Additionally, ensure the token is cleared if the SP doesn't handle it
            // directly or if you want to be explicit.
            // If the SP is reliable for clearing the token, you might not need the explicit call below.
            // For now, let's assume UpdatePasswordAsync's SP handles clearing.
            // If it doesn't, you would call:
            // await ClearPasswordResetTokenAsync(userId);
        }

        public async Task ClearPasswordResetTokenAsync(long userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                UPDATE Users
                SET PasswordResetToken = NULL,
                    PasswordResetTokenExpiry = NULL,
                    UpdatedTime = GETDATE()
                WHERE Id = @UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // --- END NEWLY ADDED METHODS ---

        private User MapReaderToUser(IDataReader reader)
        {
            return new User
            {
                Id = (long)reader["Id"],
                UserName = reader["UserName"].ToString()!,
                Email = reader["Email"] as string,
                EmailConfirmed = reader["EmailConfirmed"] != DBNull.Value && (bool)reader["EmailConfirmed"],
                PasswordHash = reader["PasswordHash"] as string,
                PhoneNumber = reader["PhoneNumber"] as string,
                LockoutEnd = reader["LockoutEnd"] as DateTime?,
                LockoutEnabled = reader["LockoutEnabled"] != DBNull.Value && (bool)reader["LockoutEnabled"],
                AccessFailedCount = reader["AccessFailedCount"] != DBNull.Value ? (int)reader["AccessFailedCount"] : 0,
                IsActive = reader["IsActive"] != DBNull.Value && (bool)reader["IsActive"],
                ProfilePictureUrl = reader["ProfilePictureUrl"] as string,
                CreatedTime = reader["CreatedTime"] != DBNull.Value ? (DateTime)reader["CreatedTime"] : DateTime.MinValue,
                CreatedBy = reader["CreatedBy"] as long?,
                UpdatedTime = reader["UpdatedTime"] as DateTime?,
                UpdatedBy = reader["UpdatedBy"] as long?,
                IsDeleted = reader["IsDeleted"] != DBNull.Value && (bool)reader["IsDeleted"],
                EmailConfirmationToken = reader["EmailConfirmationToken"] as string,
                PasswordResetToken = reader["PasswordResetToken"] as string,
                // FIX FOR PasswordResetTokenExpiry MAPPING
                PasswordResetTokenExpiry = reader["PasswordResetTokenExpiry"] != DBNull.Value
                                            ? (DateTime?)reader["PasswordResetTokenExpiry"]
                                            : null
            };
        }

        public async Task LogAuditAsync(long? userId, string action, string logType, string clientIpAddress, string browserInfo)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "sp_InsertAuditLog";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Action", action);
            command.Parameters.AddWithValue("@LogType", logType);
            command.Parameters.AddWithValue("@ClientIpAddress", clientIpAddress ?? "N/A");
            command.Parameters.AddWithValue("@BrowserInfo", browserInfo ?? "N/A");
            command.Parameters.AddWithValue("@ExecutionTime", DateTime.UtcNow);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> GetUserRolesAsync(long userId)
        {
            var roles = new List<string>();

            using (var conn = _dbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT r.Name
                    FROM UserRoles ur
                    JOIN Roles r ON ur.RoleId = r.Id
                    WHERE ur.UserId = @UserId AND ur.IsDeleted = 0";
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            roles.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return roles;
        }
    }
}