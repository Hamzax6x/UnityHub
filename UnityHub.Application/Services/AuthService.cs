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

            if (user.AccessFailedCount >= 5)
                throw new UnauthorizedAccessException("Account permanently blocked. Please contact admin.");

            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTime.UtcNow && !user.IsActive && user.AccessFailedCount < 5)
            {
                await _userRepository.ReactivateUserAfterLockoutAsync(user.Id);
                user.IsActive = true;
                await LogAsync(user.Id, "Account auto-reactivated after lockout", "Info");
            }

            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                TimeZoneInfo pakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
                DateTime lockoutEndInPakistan = TimeZoneInfo.ConvertTimeFromUtc(user.LockoutEnd.Value, pakistanTimeZone);
                throw new UnauthorizedAccessException($"User is locked out. Try again at {lockoutEndInPakistan:yyyy-MM-dd hh:mm tt} (Pakistan Time)");
            }

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated.");

            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException("Please confirm your email to login.");

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

            if (user.AccessFailedCount > 0)
            {
                await _userRepository.ResetAccessFailedCountAsync(user.Id);
            }

            // ✅ Generate tokens
            string accessToken = await CreateJwtAsync(user);
            string refreshToken = GenerateRefreshToken();
            var now = DateTime.UtcNow;
            // ✅ Save refresh token
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10) // ⏳ Adjust to your policy
            });

            await LogAsync(user.Id, $"Login Successful: {dto.Email}", "Success");

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

            string newJwt = await CreateJwtAsync(user);
            string newRefreshToken = GenerateRefreshToken();
            var now = DateTime.UtcNow;
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                CreatedAt = now,
                ExpiresAt = now.AddHours(2)
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

            // Fetch URL from configuration for deployment flexibility
            var frontendUrl = _configuration["ApplicationUrls:FrontendUrl"];
            if (string.IsNullOrEmpty(frontendUrl))
            {
                // Fallback or throw if URL not configured
                frontendUrl = "http://localhost:4200"; // Fallback for development
            }
            var resetLink = $"{frontendUrl}/auth/reset-password?email={email}&token={token}";

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

            // Fetch URL from configuration for deployment flexibility
            var backendUrl = _configuration["ApplicationUrls:BackendUrl"];
            if (string.IsNullOrEmpty(backendUrl))
            {
                // Fallback or throw if URL not configured
                backendUrl = "https://localhost:7296"; // Fallback for development
            }
            var confirmationLink = $"{backendUrl}/api/Auth/confirm?email={email}&token={token}";
            string subject = "Confirm your email";
            string body = $"<p>Please click the link below to verify your email:</p><a href=\"{confirmationLink}\">{confirmationLink}</a>";

            await _emailSender.SendEmailAsync(email, subject, body);
            await LogAsync(user.Id, "Verification email sent", "Info");
        }

        // --- ADD THIS NEW METHOD TO RESOLVE CS0535 ERROR ---
        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordResetToken) || user.PasswordResetToken != token || !user.PasswordResetTokenExpiry.HasValue || user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
            {
                await LogAsync(user?.Id, $"Password reset failed for {email}: Invalid or expired token", "Warning");
                throw new UnauthorizedAccessException("Invalid or expired password reset token.");
            }

            var hasher = new PasswordHasher<string>();
            var newPasswordHash = hasher.HashPassword(null, newPassword);

            await _userRepository.ResetPasswordAsync(user.Id, newPasswordHash);
            await _userRepository.ClearPasswordResetTokenAsync(user.Id); // Clear the token after use
            await LogAsync(user.Id, "Password reset successfully", "Info");
        }


        private async Task<string> CreateJwtAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            var roles = await _userRepository.GetUserRolesAsync(user.Id);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("username", user.UserName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var now = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = now.AddHours(2),
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