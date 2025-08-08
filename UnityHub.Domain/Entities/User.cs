namespace UnityHub.Domain.Entities
{
    public class User
    {
        public long Id { get; set; }
        public string UserName { get; set; } = default!;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; } = false;
        public string? PasswordHash { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; } = true;
        public int AccessFailedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public long? CreatedBy { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public long? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
