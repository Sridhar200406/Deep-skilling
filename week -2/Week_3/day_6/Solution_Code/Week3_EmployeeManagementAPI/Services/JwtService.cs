using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Service responsible for generating JWT tokens.
    /// Reads configuration from appsettings.json → "JwtSettings" section.
    /// 
    /// JWT Structure:
    ///   Header   — algorithm (HS256) + type (JWT)
    ///   Payload  — claims: sub, name, role, jti, iat, exp
    ///   Signature — HMACSHA256(base64(header) + "." + base64(payload), SecretKey)
    ///
    /// All three parts are base64url-encoded and joined with dots:
    ///   eyJ...  .  eyJ...  .  abc...
    /// </summary>
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger        = logger;
        }

        /// <summary>
        /// Generates a signed JWT token for the given username and role.
        /// </summary>
        /// <param name="username">The authenticated user's name.</param>
        /// <param name="role">The user's role (e.g. "Admin", "Viewer").</param>
        /// <returns>A signed JWT token string.</returns>
        public string GenerateToken(string username, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var secretKey   = jwtSettings["SecretKey"]    ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var issuer      = jwtSettings["Issuer"]       ?? throw new InvalidOperationException("JWT Issuer is not configured.");
            var audience    = jwtSettings["Audience"]     ?? throw new InvalidOperationException("JWT Audience is not configured.");
            var expiryMins  = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            _logger.LogInformation("JwtService: Generating token for user '{Username}' with role '{Role}'.", username, role);

            // Create the signing key from the secret
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Define claims — these are the payload data embedded in the token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  username),               // subject (user identifier)
                new Claim(JwtRegisteredClaimNames.Name, username),               // display name
                new Claim(ClaimTypes.Role,              role),                   // role for [Authorize(Roles="Admin")]
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()), // unique token ID (prevent replay)
                new Claim(JwtRegisteredClaimNames.Iat,                           // issued at
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var expiry = DateTime.UtcNow.AddMinutes(expiryMins);

            // Build the token
            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            expiry,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation("JwtService: Token generated. Expires at {Expiry} UTC.", expiry);

            return tokenString;
        }

        /// <summary>
        /// Returns the UTC expiry time for a token generated right now.
        /// Used to populate LoginResponse.ExpiresAt.
        /// </summary>
        public DateTime GetExpiryTime()
        {
            var expiryMins = int.Parse(
                _configuration["JwtSettings:ExpiryMinutes"] ?? "60");
            return DateTime.UtcNow.AddMinutes(expiryMins);
        }
    }
}
