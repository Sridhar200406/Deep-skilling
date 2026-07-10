using Microsoft.AspNetCore.Identity;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Define default roles
            string[] roles = { "Admin", "Manager", "Employee" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Define default users
            var usersToSeed = new List<(string Username, string Email, string Password, string Role, string FullName)>
            {
                ("admin",    "admin@company.com",    "Admin@123",    "Admin",    "System Administrator"),
                ("manager",  "manager@company.com",  "Manager@123",  "Manager",  "Operations Manager"),
                ("employee", "employee@company.com", "Employee@123", "Employee", "Regular Employee")
            };

            foreach (var userData in usersToSeed)
            {
                var user = await userManager.FindByNameAsync(userData.Username);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = userData.Username,
                        Email = userData.Email,
                        FullName = userData.FullName,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, userData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to seed user '{userData.Username}': {errors}");
                    }
                }
            }
        }
    }
}
