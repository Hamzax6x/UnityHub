using UnityHub.Application.DTOs;

namespace UnityHub.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetPagedAsync(int page, int size, DateTime? start, DateTime? end);
        Task<RoleDto?> GetByIdAsync(int id);
        Task CreateAsync(RoleCreateDto dto);
        Task UpdateAsync(RoleUpdateDto dto);
        Task DeleteAsync(int id);

    }
}
