using Microsoft.AspNetCore.Http;
using UnityHub.Application.DTOs;
using UnityHub.Application.Interfaces.Repositories;
using UnityHub.Application.Interfaces.Services;
using UnityHub.Domain.Entities;
using System.Security.Claims;

namespace UnityHub.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoleService(IRoleRepository repo, IAuditLogRepository auditLogRepo, IHttpContextAccessor httpContextAccessor)
        {
            _repo = repo;
            _auditLogRepo = auditLogRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<RoleDto>> GetPagedAsync(int page, int size, DateTime? start, DateTime? end)
        {
            var roles = await _repo.GetAllActivePagedAsync(page, size, start, end);
            return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsStatic = r.IsStatic,
                IsDefault = r.IsDefault,
                IsDeleted = r.IsDeleted
            });
        }

        public async Task<RoleDto?> GetByIdAsync(int id)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return null;

            return new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsStatic = r.IsStatic,
                IsDefault = r.IsDefault,
                IsDeleted = r.IsDeleted
            };
        }

        public async Task CreateAsync(RoleCreateDto dto)
        {
            var createdBy = GetUserId();
            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedBy = createdBy ?? 0
            };

            await _repo.AddAsync(role);
            await LogAsync(createdBy, $"Created role: {dto.Name}", "Info");
        }

        public async Task UpdateAsync(RoleUpdateDto dto)
        {
            var updatedBy = GetUserId();
            var role = new Role
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                UpdatedBy = updatedBy ?? 0
            };

            await _repo.UpdateAsync(role);
            await LogAsync(updatedBy, $"Updated role: {dto.Name} (ID: {dto.Id})", "Info");
        }

        public async Task DeleteAsync(int id)
        {
            var updatedBy = GetUserId();
            await _repo.DeleteAsync(id, updatedBy ?? 0);
            await LogAsync(updatedBy, $"Deleted role ID: {id}", "Warning");
        }

        private async Task LogAsync(long? userId, string action, string logType)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var browser = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown Browser";

            await _auditLogRepo.LogAsync(userId, action, logType, ip, browser);
        }

        private long? GetUserId()
        {
            var idStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(idStr, out var id) ? id : null;
        }
    }
}
