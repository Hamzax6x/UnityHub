using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnityHub.Application.DTOs.Auth;
using UnityHub.Application.Interfaces.Services;

namespace UnityHub.WebApi.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthController(IAuthService authService, IRefreshTokenRepository refreshTokenRepository)
        {
            _authService = authService;
            _refreshTokenRepository = refreshTokenRepository;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });  // 👈 send JSON
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });  // 👈 send JSON
            }
        }



        // ✅ New: Email Confirmation Endpoint
        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
        {
            var isConfirmed = await _authService.ConfirmEmailAsync(email, token);
            if (isConfirmed)
                return Ok("✅ Email confirmed successfully.");
            else
                return BadRequest("❌ Invalid or expired token.");
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(dto.RefreshToken);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok("Password reset link has been sent to your email.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
            return Ok("Password has been reset successfully.");
        }


    }
}
