using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.Services
{
    /// <summary>
    /// Generates signed JWT tokens for authenticated users.
    /// The same secret / issuer / audience must be configured
    /// identically in the API Gateway's appsettings.json.
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config) => _config = config;

        public (string token, DateTime expires) CreateToken(AppUser user)
        {
            var jwtSection = _config.GetSection("JwtSettings");
            var secret     = jwtSection["Secret"]!;
            var issuer     = jwtSection["Issuer"]!;
            var audience   = jwtSection["Audience"]!;
            var expMinutes = int.Parse(jwtSection["ExpirationMinutes"] ?? "60");

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(expMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email,          user.Email!),
                new Claim(ClaimTypes.GivenName,      user.FirstName),
                new Claim(ClaimTypes.Surname,        user.LastName),
                // Service-to-service scope — allows internal gateway calls
                new Claim("scope", "internal-service")
            };

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                expires:            expires,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
