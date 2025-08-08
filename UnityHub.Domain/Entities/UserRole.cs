namespace UnityHub.Domain.Entities
{
    public class UserRole
    {
        public long UserId { get; set; }
        public long RoleId { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties (if you were using an ORM like EF Core)
        public User User { get; set; }
        public Role Role { get; set; }
    }
}