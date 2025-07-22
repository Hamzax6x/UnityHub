using UnityHub.Infrastructure.DbConnection;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DbConnectionFactory _db;

    public RefreshTokenRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO RefreshTokens (UserId, Token, ExpiresAt, CreatedAt, CreatedByIp)
                            VALUES (@UserId, @Token, @ExpiresAt, @CreatedAt, @CreatedByIp)";
        cmd.Parameters.AddWithValue("@UserId", token.UserId);
        cmd.Parameters.AddWithValue("@Token", token.Token);
        cmd.Parameters.AddWithValue("@ExpiresAt", token.ExpiresAt);
        cmd.Parameters.AddWithValue("@CreatedAt", token.CreatedAt);
        cmd.Parameters.AddWithValue("@CreatedByIp", (object?)token.CreatedByIp ?? DBNull.Value);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM RefreshTokens WHERE Token = @Token";
        cmd.Parameters.AddWithValue("@Token", token);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new RefreshToken
            {
                Id = reader.GetInt64(0),
                UserId = reader.GetInt64(1),
                Token = reader.GetString(2),
                ExpiresAt = reader.GetDateTime(3),
                CreatedAt = reader.GetDateTime(4),
                CreatedByIp = reader.IsDBNull(5) ? null : reader.GetString(5),
                RevokedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                ReplacedByToken = reader.IsDBNull(7) ? null : reader.GetString(7),
            };
        }

        return null;
    }

    public async Task RevokeAsync(string token, string? replacedByToken = null)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE RefreshTokens
                            SET RevokedAt = @RevokedAt, ReplacedByToken = @ReplacedByToken
                            WHERE Token = @Token";
        cmd.Parameters.AddWithValue("@Token", token);
        cmd.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@ReplacedByToken", (object?)replacedByToken ?? DBNull.Value);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
}
