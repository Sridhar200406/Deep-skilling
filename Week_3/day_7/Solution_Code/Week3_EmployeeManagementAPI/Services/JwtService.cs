using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Generates signed JWT tokens with claims: sub, name, role, jti, iat, exp.
    /// Reads all settings from appsettings.json → JwtSettings section.
    /// Registered as Singleton — stateless, safe to share across requests.
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

        /// <summary>Generates a signed JWT token for the given username and role.</summary>
        public string GenerateToken(string username, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey   = jwtSettings["SecretKey"]    ?? throw new InvalidOperationException("JWT SecretKey not configured.");
            var issuer      = jwtSettings["Issuer"]       ?? throw new InvalidOperationException("JWT Issuer not configured.");
            var audience    = jwtSettings["Audience"]     ?? throw new InvalidOperationException("JWT Audience not configured.");
            var expiryMins  = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            _logger.LogInformation("JwtService: Generating token for '{Username}' role '{Role}'.", username, role);

            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(expiryMins);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  username),
                new Claim(JwtRegisteredClaimNames.Name, username),
                new Claim(ClaimTypes.Role,              role),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            expiry,
                signingCredentials: creds);

            _logger.LogInformation("JwtService: Token generated, expires {Expiry} UTC.", expiry);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Returns the expiry UTC time for a token generated right now.</summary>
        public DateTime GetExpiryTime()
        {
            var mins = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");
            return DateTime.UtcNow.AddMinutes(mins);
        }
    }
}
