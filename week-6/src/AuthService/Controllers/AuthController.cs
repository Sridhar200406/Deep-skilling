using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthDbContext db, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Login and receive JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation failed"));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login for username: {Username}", request.Username);
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid username or password."));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        var expiry = int.Parse(HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["JwtSettings:ExpirationMinutes"] ?? "60");

        _logger.LogInformation("User {Username} logged in successfully.", user.Username);
        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiry)
        }));
    }

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation failed"));

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(ApiResponse<object>.FailureResponse($"Username '{request.Username}' is taken."));

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(ApiResponse<object>.FailureResponse($"Email '{request.Email}' is registered."));

        var allowed = new[] { "User", "Manager", "Admin" };
        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = allowed.Contains(request.Role) ? request.Role : "User",
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Registered user {Username}", user.Username);
        return CreatedAtAction(nameof(Login), null,
            ApiResponse<object>.SuccessResponse(new { user.Id, user.Username, user.Email, user.Role }));
    }

    /// <summary>Validate a JWT token — used by API Gateway and other services</summary>
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
            return BadRequest(ApiResponse<object>.FailureResponse("Token is required."));

        var principal = _tokenService.ValidateToken(request.Token);
        if (principal == null)
            return Ok(ApiResponse<ValidateTokenResponse>.SuccessResponse(new ValidateTokenResponse { IsValid = false }));

        return Ok(ApiResponse<ValidateTokenResponse>.SuccessResponse(new ValidateTokenResponse
        {
            IsValid = true,
            UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst("sub")?.Value,
            Username = principal.Identity?.Name,
            Role = principal.FindFirst(ClaimTypes.Role)?.Value
        }));
    }
}
