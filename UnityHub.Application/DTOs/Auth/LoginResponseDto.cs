namespace UnityHub.Application.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public long UserId { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}
