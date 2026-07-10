using Microsoft.AspNetCore.Identity;

namespace Week3_EmployeeManagementAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}
