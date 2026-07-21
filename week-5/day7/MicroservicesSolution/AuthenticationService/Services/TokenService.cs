using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthenticationService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config) => _config = config;

        public (string token, DateTime expires) CreateToken(AppUser user)
        {
            var jwt     = _config.GetSection("JwtSettings");
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpirationMinutes"] ?? "60"));
            var claims  = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email,          user.Email!),
                new Claim(ClaimTypes.GivenName,      user.FirstName),
                new Claim(ClaimTypes.Surname,        user.LastName),
                new Claim("scope",                   "internal-service")
            };
            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"], audience: jwt["Audience"],
                claims: claims, expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
