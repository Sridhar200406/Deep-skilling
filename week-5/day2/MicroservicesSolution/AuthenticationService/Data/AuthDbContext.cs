using AuthenticationService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Data
{
    /// <summary>
    /// Identity DbContext for the Authentication microservice.
    /// Owns the ASP.NET Core Identity tables (Users, Roles, Claims, etc.).
    /// </summary>
    public class AuthDbContext : IdentityDbContext<AppUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
    }
}
