using UnityHub.Domain.Entities;

namespace UnityHub.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(long roleId, long updatedBy);
        Task<Role?> GetByIdAsync(int id);
        Task<IEnumerable<Role>> GetAllActivePagedAsync(int pageNumber, int pageSize, DateTime? startDate, DateTime? endDate);
    }
}
