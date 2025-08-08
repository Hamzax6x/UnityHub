using System.Data;
using System.Data.SqlClient;
using UnityHub.Domain.Entities;
using UnityHub.Infrastructure.DbConnection;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly DbConnectionFactory _dbConnectionFactory;

    public AuditLogRepository(DbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task LogAsync(long? userId, string action, string logType, string? ipAddress, string? browserInfo)
    {
        using var conn = _dbConnectionFactory.CreateConnection();
        using var cmd = new SqlCommand("sp_InsertAuditLog", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Action", action);
        cmd.Parameters.AddWithValue("@LogType", logType);
        cmd.Parameters.AddWithValue("@ExecutionTime", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@ClientIpAddress", (object?)ipAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BrowserInfo", (object?)browserInfo ?? DBNull.Value);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<List<AuditLog>> GetAllAsync()
    {
        var logs = new List<AuditLog>();

        using var conn = _dbConnectionFactory.CreateConnection();
        using var cmd = new SqlCommand("SELECT * FROM AuditLogs ORDER BY ExecutionTime DESC", conn);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            logs.Add(new AuditLog
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetInt64(reader.GetOrdinal("UserId")),
                Action = reader.GetString(reader.GetOrdinal("Action")),
                LogType = reader.GetString(reader.GetOrdinal("LogType")),
                ExecutionTime = reader.GetDateTime(reader.GetOrdinal("ExecutionTime")),
                ClientIpAddress = reader.IsDBNull(reader.GetOrdinal("ClientIpAddress")) ? null : reader.GetString(reader.GetOrdinal("ClientIpAddress")),
                BrowserInfo = reader.IsDBNull(reader.GetOrdinal("BrowserInfo")) ? null : reader.GetString(reader.GetOrdinal("BrowserInfo"))
            });
        }

        return logs;
    }
}
