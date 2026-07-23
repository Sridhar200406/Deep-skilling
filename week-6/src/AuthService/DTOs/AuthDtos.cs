using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RegisterRequest
{
    [Required][StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required][EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required][StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "User";
}

public class ValidateTokenRequest
{
    [Required] public string Token { get; set; } = string.Empty;
}

public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public string? UserId { get; set; }
}
