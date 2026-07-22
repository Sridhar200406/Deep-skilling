using EmployeeService.Application.DTOs;
using EmployeeService.Domain.Entities;
using EmployeeService.Infrastructure.Auth;
using EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeService.Application.Services;

public interface IAuthAppService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<ApplicationUser> RegisterAsync(RegisterRequestDto dto);
}

public class AuthAppService : IAuthAppService
{
    private readonly EmployeeDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthAppService> _logger;

    public AuthAppService(
        EmployeeDbContext context,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<AuthAppService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("Login failed: user '{Username}' not found.", dto.Username);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid password for user '{Username}'.", dto.Username);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(user);
        var expiration = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60");

        _logger.LogInformation("User '{Username}' logged in successfully.", dto.Username);

        return new LoginResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiration)
        };
    }

    public async Task<ApplicationUser> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        // Only allow Admin role assignment from trusted context — default to "User"
        var allowedRoles = new[] { "User", "Manager", "Admin" };
        var role = allowedRoles.Contains(dto.Role) ? dto.Role : "User";

        var user = new ApplicationUser
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registered new user '{Username}' with role '{Role}'.", user.Username, user.Role);
        return user;
    }
}
