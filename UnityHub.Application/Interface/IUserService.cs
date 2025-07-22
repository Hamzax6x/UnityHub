using UnityHub.Application.DTOs;

namespace UnityHub.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync(int pageNumber, int pageSize, DateTime? startDate, DateTime? endDate);
        Task<UserDto?> GetByIdAsync(long id);
        Task CreateAsync(UserCreateDto dto);
        Task UpdateAsync(UserUpdateDto dto);
        Task DeleteAsync(long id, long updatedBy);
    }
}
