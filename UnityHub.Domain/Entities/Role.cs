namespace UnityHub.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsStatic { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedTime { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public long? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
