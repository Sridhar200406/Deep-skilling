using System.ComponentModel.DataAnnotations;

namespace Week3_EmployeeManagementAPI.Authentication
{
    /// <summary>Request body for POST /api/v1/auth/login.</summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string Password { get; set; } = string.Empty;
    }
}
