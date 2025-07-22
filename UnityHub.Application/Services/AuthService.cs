using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UnityHub.Application.DTOs.Auth;
using UnityHub.Application.Interfaces.Repositories;
using UnityHub.Application.Interfaces.Services;
using UnityHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace UnityHub.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogRepository _auditLogRepository;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration,
            IEmailSender emailSender,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogRepository auditLogRepository)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
            _auditLogRepository = auditLogRepository;
        }

        private async Task LogAsync(long? userId, string action, string logType)
        {
            var ip = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var browser = _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown Browser";
            await _auditLogRepository.LogAsync(userId, action, logType, ip, browser);
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userRepository.GetByEmailOrUsernameAsync(dto.Email);

            if (user == null)
            {
                await LogAsync(null, $"Login failed: {dto.Email} not found", "Warning");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Permanently blocked
            if (user.AccessFailedCount >= 5)
                throw new UnauthorizedAccessException("Account permanently blocked. Please contact admin.");

            // Auto-reactivate if lockout has expired
            if (user.LockoutEnabled &&
                user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value <= DateTime.UtcNow &&
                !user.IsActive &&
                user.AccessFailedCount < 5)
            {
                await _userRepository.ReactivateUserAfterLockoutAsync(user.Id);
                user.IsActive = true;
                await LogAsync(user.Id, "Account auto-reactivated after lockout", "Info");
            }

            // Still locked out
            if (user.LockoutEnabled &&
                user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTime.UtcNow)
            {
                TimeZoneInfo pakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
                DateTime lockoutEndInPakistan = TimeZoneInfo.ConvertTimeFromUtc(user.LockoutEnd.Value, pakistanTimeZone);

                throw new UnauthorizedAccessException(
                    $"User is locked out. Try again at {lockoutEndInPakistan:yyyy-MM-dd hh:mm tt} (Pakistan Time)");
            }

            // Account deactivated
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated.");

            // Email not confirmed
            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException("Please confirm your email to login.");

            // Verify password
            var hasher = new PasswordHasher<string>();
            var result = hasher.VerifyHashedPassword(null, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                await _userRepository.IncrementAccessFailedCountAsync(user.Id);
                user.AccessFailedCount += 1;

                if (user.AccessFailedCount == 3)
                {
                    await _userRepository.DeactivateUserAsync(user.Id);
                    await LogAsync(user.Id, "User deactivated after 3 failed login attempts", "Warning");
                }
                else if (user.AccessFailedCount >= 5)
                {
                    await LogAsync(user.Id, "User permanently blocked after 5 failed login attempts", "Error");
                }

                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // ✅ Correct password entered: reset AccessFailedCount
            if (user.AccessFailedCount > 0)
            {
                await _userRepository.ResetAccessFailedCountAsync(user.Id);
            }

            // Generate tokens
            string accessToken = CreateJwt(user);
            string refreshToken = GenerateRefreshToken();

            return new LoginResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Email = user.Email,
                UserId = user.Id,
                UserName = user.UserName
            };
        }


        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token == null || !token.IsActive)
                throw new UnauthorizedAccessException("Session expired. Please login again.");

            var user = await _userRepository.GetByIdAsync(token.UserId);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid user.");

            await _refreshTokenRepository.RevokeAsync(refreshToken);

            string newJwt = CreateJwt(user);
            string newRefreshToken = GenerateRefreshToken();

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });

            await LogAsync(user.Id, "Token refreshed", "Info");

            return new RefreshTokenResponseDto
            {
                AccessToken = newJwt,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<bool> ConfirmEmailAsync(string email, string token)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || string.IsNullOrWhiteSpace(user.EmailConfirmationToken))
                return false;

            if (user.EmailConfirmationToken.Trim() == token.Trim())
            {
                await _userRepository.ConfirmUserEmailAsync(user.Id);
                await LogAsync(user.Id, "Email confirmed", "Info");
                return true;
            }

            return false;
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("User not found");

            string token = Guid.NewGuid().ToString();
            DateTime expiry = DateTime.UtcNow.AddMinutes(15);

            await _userRepository.SavePasswordResetTokenAsync(user.Id, token, expiry);

            var resetLink = $"https://localhost:7296/reset-password?email={email}&token={token}";
            await _emailSender.SendEmailAsync(email, "Reset Your Password", $"Reset your password using this link: <a href='{resetLink}'>{resetLink}</a>");

            await LogAsync(user.Id, "Password reset requested", "Info");
        }

        public async Task SendEmailVerificationAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("User not found");

            string token = Guid.NewGuid().ToString();
            await _userRepository.UpdateEmailConfirmationTokenAsync(user.Id, token);

            var confirmationLink = $"https://localhost:7296/api/Auth/confirm?email={email}&token={token}";
            string subject = "Confirm your email";
            string body = $"<p>Please click the link below to verify your email:</p><a href=\"{confirmationLink}\">{confirmationLink}</a>";

            await _emailSender.SendEmailAsync(email, subject, body);
            await LogAsync(user.Id, "Verification email sent", "Info");
        }

        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                await LogAsync(user?.Id ?? 0, "Password reset failed: invalid/expired token", "Error");
                throw new Exception("Invalid or expired token");
            }

            var hasher = new PasswordHasher<string>();
            string newHash = hasher.HashPassword(null, newPassword);

            await _userRepository.UpdatePasswordAsync(user.Id, newHash);
            await LogAsync(user.Id, "Password reset successfully", "Info");
        }

        private string CreateJwt(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                    new Claim("username", user.UserName),
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
