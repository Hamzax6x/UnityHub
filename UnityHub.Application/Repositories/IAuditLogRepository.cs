using UnityHub.Domain.Entities;

public interface IAuditLogRepository
{
    Task LogAsync(long? userId, string action, string logType, string? ipAddress, string? browserInfo);
    Task<List<AuditLog>> GetAllAsync();
}
