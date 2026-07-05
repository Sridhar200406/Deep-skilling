using System.ComponentModel.DataAnnotations;

namespace Week3_EmployeeManagementAPI.Authentication
{
    /// <summary>
    /// Request body for POST /api/auth/login.
    /// The client sends Username and Password; the server validates and returns a JWT token.
    /// </summary>
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
