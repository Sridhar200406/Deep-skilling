using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthenticationService.DTOs;
using AuthenticationService.Interfaces;
using AuthenticationService.Models;

namespace AuthenticationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            IJwtService jwtService, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>Register a new user.</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(new { Success = false, Message = "Invalid request." });

            if (await _userManager.FindByNameAsync(request.Username) != null)
                return BadRequest(new { Success = false, Message = "Username already exists." });

            var user = new ApplicationUser { UserName = request.Username, Email = request.Email, FullName = request.FullName };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(new { Success = false, Message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            var role = string.IsNullOrWhiteSpace(request.Role) ? "Employee" : request.Role;
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
            await _userManager.AddToRoleAsync(user, role);

            _logger.LogInformation("User '{Username}' registered with role '{Role}'", request.Username, role);
            return Ok(new { Success = true, Message = "User registered successfully." });
        }

        /// <summary>Login and receive a JWT token.</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(new { Success = false, Message = "Invalid credentials." });

            var user = await _userManager.FindByNameAsync(request.Username)
                       ?? await _userManager.FindByEmailAsync(request.Username);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized(new { Success = false, Message = "Invalid username or password." });

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user.UserName!, roles);

            _logger.LogInformation("User '{Username}' logged in successfully", user.UserName);
            return Ok(new LoginResponseDto
            {
                Token = token,
                ExpiresAt = _jwtService.GetExpiryTime(),
                Username = user.UserName!,
                Role = roles.FirstOrDefault() ?? "Employee"
            });
        }
    }
}
