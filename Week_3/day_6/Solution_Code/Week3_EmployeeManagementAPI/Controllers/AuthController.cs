using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.Authentication;
using Week3_EmployeeManagementAPI.Responses;
using Week3_EmployeeManagementAPI.Services;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Authentication controller.
    /// Handles login and returns a JWT bearer token.
    /// All endpoints here are [AllowAnonymous] — no token required to log in.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]   // entire controller is public — login doesn't require a token
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        // Hard-coded demo users.
        // In a real app these would come from a Users table / ASP.NET Core Identity.
        private static readonly Dictionary<string, (string Password, string Role)> _users = new()
        {
            { "admin",  ("Admin@123",  "Admin")  },
            { "viewer", ("Viewer@123", "Viewer") }
        };

        public AuthController(JwtService jwtService, ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _logger     = logger;
        }

        // ===================================================================
        // POST /api/auth/login
        // ===================================================================
        /// <summary>Authenticate a user and receive a JWT Bearer token.</summary>
        /// <param name="request">Username and password credentials.</param>
        /// <remarks>
        /// Demo credentials:
        /// - Username: admin   | Password: Admin@123   | Role: Admin
        /// - Username: viewer  | Password: Viewer@123  | Role: Viewer
        ///
        /// Copy the returned Token value and paste it into Swagger's Authorize dialog:
        ///   Value: Bearer {token}
        /// </remarks>
        /// <returns>200 OK with LoginResponse containing the JWT token.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>),        StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>),        StatusCodes.Status400BadRequest)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("POST /api/auth/login — login attempt for user '{Username}'.", request.Username);

            // Validate credentials against the demo user store
            if (!_users.TryGetValue(request.Username.ToLower(), out var userInfo)
                || userInfo.Password != request.Password)
            {
                _logger.LogWarning("POST /api/auth/login — invalid credentials for '{Username}'.", request.Username);

                return Unauthorized(new ApiResponse<object>
                {
                    Success    = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message    = "Invalid username or password."
                });
            }

            // Generate JWT token
            var token   = _jwtService.GenerateToken(request.Username, userInfo.Role);
            var expiry  = _jwtService.GetExpiryTime();

            var response = new LoginResponse
            {
                Token     = token,
                TokenType = "Bearer",
                ExpiresAt = expiry,
                Username  = request.Username,
                Role      = userInfo.Role
            };

            _logger.LogInformation(
                "POST /api/auth/login — user '{Username}' authenticated successfully. Token expires at {Expiry} UTC.",
                request.Username, expiry);

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "Login successful."));
        }
    }
}
