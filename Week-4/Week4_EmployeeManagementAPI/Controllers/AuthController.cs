using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.Authentication;
using Week3_EmployeeManagementAPI.Models;
using Week3_EmployeeManagementAPI.Responses;
using Week3_EmployeeManagementAPI.Services;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Authentication Controller — v1.
    /// Provides user registration and JWT token issuance using ASP.NET Core Identity.
    /// All endpoints are public (AllowAnonymous).
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            JwtService jwtService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        // ===================================================================
        // POST /api/v1/auth/register
        // ===================================================================
        /// <summary>Register a new user and assign a role.</summary>
        /// <param name="request">User details (username, email, password, etc.).</param>
        /// <returns>200 OK on success, or 400 Bad Request if validation or creation fails.</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("Register attempt — user: '{Username}', email: '{Email}'", request.Username, request.Email);

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.BadRequestResponse("Invalid request payload."));
            }

            var userExists = await _userManager.FindByNameAsync(request.Username);
            if (userExists != null)
            {
                return BadRequest(ApiResponse<object>.BadRequestResponse("Username already exists."));
            }

            var emailExists = await _userManager.FindByEmailAsync(request.Email);
            if (emailExists != null)
            {
                return BadRequest(ApiResponse<object>.BadRequestResponse("Email already in use."));
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for '{Username}': {Errors}", request.Username, errors);
                return BadRequest(ApiResponse<object>.BadRequestResponse($"User creation failed: {errors}"));
            }

            // Assign role (default to Employee)
            var roleToAssign = string.IsNullOrWhiteSpace(request.Role) ? "Employee" : request.Role;
            
            if (!await _roleManager.RoleExistsAsync(roleToAssign))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleToAssign));
            }

            await _userManager.AddToRoleAsync(user, roleToAssign);

            _logger.LogInformation("Registration success — user '{Username}' registered with role '{Role}'", request.Username, roleToAssign);
            return Ok(ApiResponse<string>.SuccessResponse(request.Username, "User registered successfully."));
        }

        // ===================================================================
        // POST /api/v1/auth/login
        // ===================================================================
        /// <summary>Authenticate and receive a JWT Bearer token.</summary>
        /// <param name="request">Username and password.</param>
        /// <remarks>
        /// ### Core Roles
        ///
        /// | Role     | Default User  | Password      | Description                  |
        /// |----------|---------------|---------------|------------------------------|
        /// | Admin    | admin         | Admin@123     | Full CRUD permissions        |
        /// | Manager  | manager       | Manager@123   | GET + POST + PUT permissions |
        /// | Employee | employee      | Employee@123  | GET permissions only         |
        /// </remarks>
        /// <returns>200 OK with JWT token, expiry, username, and role.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt — user: '{Username}'", request.Username);

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.BadRequestResponse("Invalid credentials layout."));
            }

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                // Try finding by Email as well
                user = await _userManager.FindByEmailAsync(request.Username);
            }

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Login failed — invalid credentials for '{Username}'", request.Username);
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Invalid username or password."
                });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user.UserName!, roles);
            var expiry = _jwtService.GetExpiryTime();

            _logger.LogInformation("Login success — user '{Username}', roles '{Roles}', expires {Expiry} UTC",
                user.UserName, string.Join(",", roles), expiry);

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresAt = expiry,
                Username = user.UserName!,
                Role = roles.FirstOrDefault() ?? "Employee"
            }, "Login successful."));
        }
    }
}
