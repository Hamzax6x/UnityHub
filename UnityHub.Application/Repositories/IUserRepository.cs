// UnityHub.Application/Interfaces/Repositories/IUserRepository.cs
using UnityHub.Application.DTOs; // Ensure this is present if UserCreateDto is used
using UnityHub.Domain.Entities; // Ensure this is present for the User entity

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

        // !! THESE TWO METHODS MUST BE PRESENT IN THIS INTERFACE !!
        Task ResetPasswordAsync(long userId, string newPasswordHash); // <--- Make sure this line exists!
        Task ClearPasswordResetTokenAsync(long userId);             // <--- Make sure this line exists!
        // !! -------------------------------------------------- !!

        Task UpdatePasswordAsync(long userId, string newPasswordHash); // This one was already there, but ResetPasswordAsync is new
        Task LogAuditAsync(long? userId, string action, string logType, string clientIpAddress, string browserInfo);
        Task DeactivateUserAsync(long userId);
        Task ReactivateUserAfterLockoutAsync(long userId);
        Task AdminUnblockUserAsync(long userId, string ip, string browserInfo);
        Task<List<string>> GetUserRolesAsync(long userId);
        Task<long> CreateUserWithRolesAsync(UserCreateDto dto, string emailConfirmationToken);

    }
}