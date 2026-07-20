using AuthenticationService.DTOs;
using AuthenticationService.Models;
using AuthenticationService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser>   _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly TokenService           _tokenService;

        public AuthController(UserManager<AppUser> um, SignInManager<AppUser> sm, TokenService ts)
        { _userManager = um; _signInManager = sm; _tokenService = ts; }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new AppUser { UserName = dto.Email, Email = dto.Email, FirstName = dto.FirstName, LastName = dto.LastName };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(new { Success = false, Errors = result.Errors.Select(e => e.Description) });
            var (token, expires) = _tokenService.CreateToken(user);
            return Ok(new AuthResponseDto { Token = token, Email = user.Email!, FullName = $"{user.FirstName} {user.LastName}", Expires = expires });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized(new { Success = false, Message = "Invalid credentials." });
            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded) return Unauthorized(new { Success = false, Message = "Invalid credentials." });
            var (token, expires) = _tokenService.CreateToken(user);
            return Ok(new AuthResponseDto { Token = token, Email = user.Email!, FullName = $"{user.FirstName} {user.LastName}", Expires = expires });
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { Status = "Healthy", Service = "AuthenticationService", Timestamp = DateTime.UtcNow });
    }
}
