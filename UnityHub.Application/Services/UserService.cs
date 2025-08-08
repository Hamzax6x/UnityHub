using UnityHub.Application.DTOs;
using UnityHub.Application.Interfaces.Repositories;
using UnityHub.Application.Interfaces.Services;
using UnityHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace UnityHub.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly ICurrentUserService _currentUserService;

        // Constructor: Removed IAuditLogRepository as per your provided code
        public UserService(
            IUserRepository userRepository,
            IEmailSender emailSender,
            ICurrentUserService currentUserService)
        {
            _userRepository = userRepository;
            _emailSender = emailSender;
            _currentUserService = currentUserService;
        }

        private string GenerateEmailConfirmationToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        public async Task SendEmailVerificationAsync(string email)
        {
            var user = await _userRepository.GetByEmailOrUsernameAsync(email);
            if (user == null) throw new Exception("User not found.");

            var token = GenerateEmailConfirmationToken();
            await _userRepository.UpdateEmailConfirmationTokenAsync(user.Id, token);

            var verificationLink = $"https://localhost:7296/api/Auth/confirm?email={email}&token={token}";
            var body = $"Click to verify your email: <a href='{verificationLink}'>Verify Email</a>";

            await _emailSender.SendEmailAsync(email, "Verify your email", body);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(int pageNumber, int pageSize, DateTime? startDate, DateTime? endDate)
        {
            var users = await _userRepository.GetAllActiveUsersPagedAsync(pageNumber, pageSize, startDate, endDate);
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                ProfilePictureUrl = u.ProfilePictureUrl,
                IsActive = u.IsActive
            });
        }

        public async Task<UserDto?> GetByIdAsync(long id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsActive = user.IsActive
            };
        }

        // IMPORTANT: The original 'CreateAsync' method that calls _userRepository.CreateUserWithRolesAsync(dto)
        // This is the method that needs to correctly generate and pass the token.
        public async Task CreateAsync(UserCreateDto dto)
        {
            var token = GenerateEmailConfirmationToken();
            var currentUserId = _currentUserService.UserId;

            var hashedPassword = new PasswordHasher<string>().HashPassword(null, dto.Password);

            // You were calling _userRepository.CreateUserWithRolesAsync(dto) here.
            // This needs to pass the generated token.
            dto.CreatedBy = currentUserId; // Ensure CreatedBy is set for the DTO
            await _userRepository.CreateUserWithRolesAsync(dto, token); // <--- MODIFIED TO PASS TOKEN

            // The 'user' variable below was populated locally, but it's not needed
            // if CreateUserWithRolesAsync directly creates the user in the DB.
            // However, the email sending part still relies on 'user.Email',
            // so we'll ensure the token is correctly used for email verification link.

            var verificationLink = $"https://localhost:7296/api/Auth/confirm?email={dto.Email}&token={token}"; // Use dto.Email
            var body = $"Click to verify your email: <a href='{verificationLink}'>Verify Email</a>";

            await _emailSender.SendEmailAsync(dto.Email!, "Verify your email", body); // Use dto.Email!

            // Log audit using _userRepository.LogAuditAsync as you have it
            await _userRepository.LogAuditAsync(currentUserId, $"Created user {dto.UserName}", "UserCreation", _currentUserService.ClientIpAddress, _currentUserService.BrowserInfo);
        }

        public async Task UpdateAsync(UserUpdateDto dto)
        {
            var currentUserId = _currentUserService.UserId;

            var user = new User
            {
                Id = dto.Id,
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                UpdatedBy = currentUserId
            };

            await _userRepository.UpdateAsync(user);

            await _userRepository.LogAuditAsync(currentUserId, $"Updated user {dto.UserName}", "UserUpdate", _currentUserService.ClientIpAddress, _currentUserService.BrowserInfo);
        }

        public async Task DeleteAsync(long id, long updatedBy)
        {
            await _userRepository.DeleteAsync(id, updatedBy);

            await _userRepository.LogAuditAsync(updatedBy, $"Soft deleted user with ID {id}", "UserDelete", _currentUserService.ClientIpAddress, _currentUserService.BrowserInfo);
        }

        // IMPORTANT: This method now explicitly calls _userRepository.CreateUserWithRolesAsync(dto, emailConfirmationToken);
        // This is the primary method for creating users with roles.
        // I am assuming your UsersController calls THIS method.
        public async Task<long> CreateUserWithRolesAsync(UserCreateDto dto)
        {
            dto.CreatedBy = _currentUserService.UserId;
            var emailConfirmationToken = GenerateEmailConfirmationToken(); // Generate token here

            // Call the repository with both the DTO and the generated token
            var newUserId = await _userRepository.CreateUserWithRolesAsync(dto, emailConfirmationToken); // <--- MODIFIED TO PASS TOKEN

            // If user creation was successful, send email and log audit
            if (newUserId > 0)
            {
                var verificationLink = $"https://localhost:7296/api/Auth/confirm?email={dto.Email}&token={emailConfirmationToken}";
                var body = $"Click to verify your email: <a href='{verificationLink}'>Verify Email</a>";
                await _emailSender.SendEmailAsync(dto.Email!, "Verify your email", body);

                await _userRepository.LogAuditAsync( // Use _userRepository.LogAuditAsync
                    newUserId,
                    $"Created user '{dto.UserName}' with roles [{string.Join(",", dto.RoleIds)}]",
                    "UserCreation",
                    _currentUserService.ClientIpAddress,
                    _currentUserService.BrowserInfo
                );
            }
            else
            {
                // Handle the case where user creation failed (e.g., duplicate username/email from repo)
                await _userRepository.LogAuditAsync( // Use _userRepository.LogAuditAsync
                    null, // User ID might not be available if creation failed
                    $"Failed to create user '{dto.UserName}' with roles [{string.Join(",", dto.RoleIds)}]",
                    "Error",
                    _currentUserService.ClientIpAddress,
                    _currentUserService.BrowserInfo
                );
                throw new Exception("User creation failed.");
            }

            return newUserId;
        }
    }
}