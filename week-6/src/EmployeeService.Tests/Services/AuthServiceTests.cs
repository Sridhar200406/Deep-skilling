using EmployeeService.Application.DTOs;
using EmployeeService.Application.Services;
using EmployeeService.Infrastructure.Auth;
using EmployeeService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeService.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly EmployeeDbContext _context;
    private readonly IAuthAppService _authService;
    private readonly IJwtTokenService _jwtService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new EmployeeDbContext(options);

        // Build a minimal IConfiguration for JWT
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]        = "TestSecret-Min32Chars-ForUnitTests!!",
                ["JwtSettings:Issuer"]           = "TestIssuer",
                ["JwtSettings:Audience"]         = "TestAudience",
                ["JwtSettings:ExpirationMinutes"] = "60"
            })
            .Build();

        _jwtService = new JwtTokenService(config);
        _authService = new AuthAppService(
            _context,
            _jwtService,
            config,
            new Mock<ILogger<AuthAppService>>().Object);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_CreatesUserSuccessfully()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser",
            Email = "testuser@test.com",
            Password = "SecurePass@123",
            Role = "User"
        };

        // Act
        var user = await _authService.RegisterAsync(request);

        // Assert
        user.Should().NotBeNull();
        user.Username.Should().Be("testuser");
        user.Email.Should().Be("testuser@test.com");
        user.Role.Should().Be("User");
        user.PasswordHash.Should().NotBe("SecurePass@123"); // must be hashed
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "dupeuser",
            Email = "dupe1@test.com",
            Password = "SecurePass@123"
        };
        await _authService.RegisterAsync(request);

        // Act & Assert
        var duplicate = new RegisterRequestDto
        {
            Username = "dupeuser",
            Email = "dupe2@test.com",   // different email, same username
            Password = "SecurePass@123"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(duplicate));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequestDto
        {
            Username = "loginuser",
            Email = "login@test.com",
            Password = "MyPassword@1"
        });

        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "loginuser",
            Password = "MyPassword@1"
        });

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("loginuser");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequestDto
        {
            Username = "wrongpwduser",
            Email = "wrongpwd@test.com",
            Password = "CorrectPass@1"
        });

        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "wrongpwduser",
            Password = "WrongPassword!"
        });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ReturnsNull()
    {
        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "nobody",
            Password = "anything"
        });
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
