namespace AuthenticationService.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(string username, IEnumerable<string> roles);
        DateTime GetExpiryTime();
    }
}
