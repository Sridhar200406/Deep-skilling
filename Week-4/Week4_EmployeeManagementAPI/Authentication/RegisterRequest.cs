using System.ComponentModel.DataAnnotations;

namespace Week3_EmployeeManagementAPI.Authentication
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Role { get; set; } // Can default to "Employee" if not provided
    }
}
