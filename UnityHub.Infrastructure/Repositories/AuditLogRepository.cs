using System.Data;
using System.Data.SqlClient;
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
}
