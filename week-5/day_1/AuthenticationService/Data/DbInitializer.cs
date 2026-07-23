using Microsoft.AspNetCore.Identity;
using AuthenticationService.Models;

namespace AuthenticationService.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Manager", "Employee" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var usersToSeed = new List<(string Username, string Email, string Password, string Role, string FullName)>
            {
                ("admin",    "admin@company.com",    "Admin@123",    "Admin",    "System Administrator"),
                ("manager",  "manager@company.com",  "Manager@123",  "Manager",  "Operations Manager"),
                ("employee", "employee@company.com", "Employee@123", "Employee", "Regular Employee")
            };

            foreach (var userData in usersToSeed)
            {
                if (await userManager.FindByNameAsync(userData.Username) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userData.Username,
                        Email = userData.Email,
                        FullName = userData.FullName,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, userData.Password);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, userData.Role);
                }
            }
        }
    }
}
