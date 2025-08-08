using System.ComponentModel.DataAnnotations;

namespace UnityHub.Application.DTOs
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, ErrorMessage = "Username cannot exceed 20 characters")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits")]
        public string PhoneNumber { get; set; }

        public string ProfilePictureUrl { get; set; }
        public List<int> RoleIds { get; set; } = new();
        public long CreatedBy { get; set; }

    }
}
