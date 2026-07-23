using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class RegisterRequestDto
    {
        [Required][StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required][EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required][StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? Role { get; set; }
    }
}
