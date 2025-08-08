using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using UnityHub.Application.Interfaces.Services;

namespace UnityHub.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var id))
                    throw new UnauthorizedAccessException("User is not authenticated.");
                return id;
            }
        }

        public string ClientIpAddress =>
            _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown IP";

        public string BrowserInfo =>
            _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown Browser";
    }
}
