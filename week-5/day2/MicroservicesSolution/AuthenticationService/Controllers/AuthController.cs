using AuthenticationService.DTOs;
using AuthenticationService.Models;
using AuthenticationService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controllers
{
    /// <summary>
    /// Handles user registration and login.
    /// Issues JWT tokens that are accepted by the API Gateway
    /// and forwarded to downstream services.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser>  _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly TokenService           _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<AppUser>   userManager,
            SignInManager<AppUser> signInManager,
            TokenService           tokenService,
            ILogger<AuthController> logger)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _tokenService  = tokenService;
            _logger        = logger;
        }

        // ── POST /api/auth/register ──────────────────────────────────────────
        /// <summary>Register a new user account.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            _logger.LogInformation("Register attempt for {Email}", dto.Email);

            var user = new AppUser
            {
                UserName  = dto.Email,
                Email     = dto.Email,
                FirstName = dto.FirstName,
                LastName  = dto.LastName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogWarning("Registration failed for {Email}: {Errors}", dto.Email, string.Join(", ", errors));
                return BadRequest(new { Success = false, Errors = errors });
            }

            var (token, expires) = _tokenService.CreateToken(user);
            _logger.LogInformation("Registered and issued token for {Email}", dto.Email);

            return Ok(new AuthResponseDto
            {
                Token    = token,
                Email    = user.Email!,
                FullName = $"{user.FirstName} {user.LastName}",
                Expires  = expires
            });
        }

        // ── POST /api/auth/login ─────────────────────────────────────────────
        /// <summary>Authenticate and receive a JWT token.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            _logger.LogInformation("Login attempt for {Email}", dto.Email);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed — user {Email} not found", dto.Email);
                return Unauthorized(new { Success = false, Message = "Invalid credentials." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed — wrong password for {Email}", dto.Email);
                return Unauthorized(new { Success = false, Message = "Invalid credentials." });
            }

            var (token, expires) = _tokenService.CreateToken(user);
            _logger.LogInformation("Token issued for {Email}", dto.Email);

            return Ok(new AuthResponseDto
            {
                Token    = token,
                Email    = user.Email!,
                FullName = $"{user.FirstName} {user.LastName}",
                Expires  = expires
            });
        }

        // ── GET /api/auth/health ─────────────────────────────────────────────
        /// <summary>Simple health-check for the authentication service.</summary>
        [HttpGet("health")]
        public IActionResult Health() =>
            Ok(new { Status = "Healthy", Service = "AuthenticationService", Timestamp = DateTime.UtcNow });
    }
}
