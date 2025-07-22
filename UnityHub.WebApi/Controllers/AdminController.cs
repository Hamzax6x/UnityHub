using Microsoft.AspNetCore.Mvc;
using UnityHub.Application.Interfaces.Repositories;

namespace UnityHub.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public AdminController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("unblock-user/{userId}")]
        public async Task<IActionResult> UnblockUser(long userId)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var browser = Request.Headers["User-Agent"].ToString() ?? "Unknown Browser";

            await _userRepository.AdminUnblockUserAsync(userId, ip, browser);

            return Ok("User unblocked successfully.");
        }


    }
}
