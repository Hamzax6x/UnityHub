namespace UnityHub.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string Action { get; set; } = default!;
        public string LogType { get; set; } = default!;
        public DateTime ExecutionTime { get; set; }
        public string? ClientIpAddress { get; set; }
        public string? BrowserInfo { get; set; }
    }
}
