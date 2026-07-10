using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Authentication
{
    public class EmailDomainHandler : AuthorizationHandler<EmailDomainRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public EmailDomainHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            EmailDomainRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var username = context.User.Identity.Name;
            if (username == null)
            {
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByNameAsync(username);
                if (user != null && user.Email != null && user.Email.EndsWith("@" + requirement.RequiredDomain, StringComparison.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
