using UnityHub.Application.DTOs.Auth;

namespace UnityHub.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
        Task SendEmailVerificationAsync(string email);
        Task<bool> ConfirmEmailAsync(string email, string token);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string email, string token, string newPassword);



    }
}
