using Microsoft.AspNetCore.Authorization;

namespace Week3_EmployeeManagementAPI.Authentication
{
    public class EmailDomainRequirement : IAuthorizationRequirement
    {
        public string RequiredDomain { get; }

        public EmailDomainRequirement(string requiredDomain)
        {
            RequiredDomain = requiredDomain;
        }
    }
}
