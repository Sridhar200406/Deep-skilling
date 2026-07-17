using Microsoft.AspNetCore.Identity;

namespace AuthenticationService.Models
{
    /// <summary>
    /// Extended Identity user — adds FirstName and LastName
    /// on top of the standard IdentityUser fields.
    /// </summary>
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName  { get; set; } = string.Empty;
    }
}
