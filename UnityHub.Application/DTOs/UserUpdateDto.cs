namespace UnityHub.Application.DTOs
{
    public class UserUpdateDto
    {
        public long Id { get; set; }
        public string UserName { get; set; } = default!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public long UpdatedBy { get; set; }
    }
}
