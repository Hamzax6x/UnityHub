namespace UnityHub.Application.DTOs
{
    public class UserDto
    {
        public long Id { get; set; }
        public string UserName { get; set; } = default!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

    }
}
