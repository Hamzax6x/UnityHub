using UnityHub.Domain.Entities;

namespace UnityHub.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(long id);
        Task<IEnumerable<User>> GetAllActiveUsersPagedAsync(int pageNumber, int pageSize, DateTime? startDate, DateTime? endDate);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(long id, long updatedBy);
        Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername);
        Task<User?> GetByEmailAsync(string email);
        Task ConfirmUserEmailAsync(long userId);   
        Task UpdateEmailConfirmationTokenAsync(long userId, string token);
        Task IncrementAccessFailedCountAsync(long userId);
        Task ResetAccessFailedCountAsync(long userId);
        Task SavePasswordResetTokenAsync(long userId, string token, DateTime expiry);
        Task UpdatePasswordAsync(long userId, string newPasswordHash);
        Task LogAuditAsync(long userId, string action, string logType, string clientIpAddress, string browserInfo);
        Task DeactivateUserAsync(long userId);
        Task ReactivateUserAfterLockoutAsync(long userId);
        Task AdminUnblockUserAsync(long userId, string ip, string browserInfo);

    }
}
