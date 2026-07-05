using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.Authentication;
using Week3_EmployeeManagementAPI.Responses;
using Week3_EmployeeManagementAPI.Services;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Authentication Controller — v1.
    /// Provides JWT token issuance. All endpoints are public (AllowAnonymous).
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Seeded demo users.
        /// In production these come from a database with hashed passwords.
        /// </summary>
        private static readonly Dictionary<string, (string Password, string Role)> _users = new()
        {
            { "admin",    ("Admin@123",    "Admin")    },
            { "manager",  ("Manager@123",  "Manager")  },
            { "employee", ("Employee@123", "Employee") }
        };

        public AuthController(JwtService jwtService, ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _logger     = logger;
        }

        // ===================================================================
        // POST /api/v1/auth/login
        // ===================================================================
        /// <summary>Authenticate and receive a JWT Bearer token.</summary>
        /// <param name="request">Username and password.</param>
        /// <remarks>
        /// ### Demo Credentials
        ///
        /// | Username | Password      | Role     | Permissions                    |
        /// |----------|---------------|----------|-------------------------------|
        /// | admin    | Admin@123     | Admin    | Full CRUD                      |
        /// | manager  | Manager@123   | Manager  | GET + POST + PUT               |
        /// | employee | Employee@123  | Employee | GET only                       |
        ///
        /// ### How to use the token in Swagger
        /// 1. Execute this endpoint and copy the `Token` from the response.
        /// 2. Click the 🔓 **Authorize** button at the top of Swagger UI.
        /// 3. Enter: `Bearer {your-token-here}`
        /// 4. Click **Authorize** then **Close**.
        /// 5. All protected endpoints will now include your token automatically.
        /// </remarks>
        /// <returns>200 OK with JWT token, expiry, username, and role.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>),        StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>),        StatusCodes.Status400BadRequest)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt — user: '{Username}'", request.Username);

            if (!_users.TryGetValue(request.Username.ToLower(), out var userInfo)
                || userInfo.Password != request.Password)
            {
                _logger.LogWarning("Login failed — invalid credentials for '{Username}'", request.Username);
                return Unauthorized(new ApiResponse<object>
                {
                    Success    = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message    = "Invalid username or password."
                });
            }

            var token  = _jwtService.GenerateToken(request.Username, userInfo.Role);
            var expiry = _jwtService.GetExpiryTime();

            _logger.LogInformation("Login success — user '{Username}', role '{Role}', expires {Expiry} UTC",
                request.Username, userInfo.Role, expiry);

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
            {
                Token     = token,
                TokenType = "Bearer",
                ExpiresAt = expiry,
                Username  = request.Username,
                Role      = userInfo.Role
            }, "Login successful."));
        }
    }
}
