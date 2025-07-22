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

        public UserService(IUserRepository userRepository, IEmailSender emailSender, ICurrentUserService currentUserService)
        {
            _userRepository = userRepository;
            _emailSender = emailSender;
            _currentUserService = currentUserService;
        }

        private string GenerateEmailConfirmationToken()
        {
            return Guid.NewGuid().ToString("N"); // Removes dashes
        }

        public async Task SendEmailVerificationAsync(string email)
        {
            var user = await _userRepository.GetByEmailOrUsernameAsync(email);
            if (user == null)
                throw new Exception("User not found.");

            var token = GenerateEmailConfirmationToken();
            await _userRepository.UpdateEmailConfirmationTokenAsync(user.Id, token);

            var verificationLink = $"https://localhost:7296/api/Auth/confirm?email={email}&token={token}";
            var body = $"Click to verify your email: <a href='{verificationLink}'>Verify Email</a>";

            await _emailSender.SendEmailAsync(email, "Verify your email", body);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(int pageNumber, int pageSize, DateTime? startDate, DateTime? endDate)
        {
            var users = await _userRepository.GetAllActiveUsersPagedAsync(pageNumber, pageSize, startDate, endDate);
            if (users == null)
                throw new Exception("No User found");
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

        public async Task CreateAsync(UserCreateDto dto)
        {
            var token = GenerateEmailConfirmationToken();
            var currentUserId = _currentUserService.UserId;

            var hashedPassword = new PasswordHasher<string>().HashPassword(null, dto.Password);

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                PhoneNumber = dto.PhoneNumber,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                CreatedBy = currentUserId,
                EmailConfirmed = false,
                IsActive = true,
                EmailConfirmationToken = token
            };

            await _userRepository.AddAsync(user);

            var verificationLink = $"https://localhost:7296/api/Auth/confirm?email={user.Email}&token={token}";
            var body = $"Click to verify your email: <a href='{verificationLink}'>Verify Email</a>";

            await _emailSender.SendEmailAsync(user.Email!, "Verify your email", body);
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
        }

        public async Task DeleteAsync(long id, long updatedBy)
        {
            await _userRepository.DeleteAsync(id, updatedBy);
        }
    }
}
