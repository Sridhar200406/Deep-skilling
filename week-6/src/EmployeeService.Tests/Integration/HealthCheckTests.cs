using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests that spin up the full ASP.NET Core pipeline in-memory.
/// These verify routing and middleware without hitting real Azure services.
/// Note: These require all dependencies (DB, Blob) to be reachable.
/// In CI they run against the InMemory DB configured via environment.
/// </summary>
public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        // Override configuration for testing
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                "Server=(localdb)\\mssqllocaldb;Database=EmployeeTestDb;Trusted_Connection=True");
            builder.UseSetting("JwtSettings:SecretKey",
                "IntegrationTest-Secret-Key-Min32Chars!!");
            builder.UseSetting("AzureKeyVault:VaultUri", ""); // skip Key Vault in tests
        }).CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithoutBody_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/auth/login",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Should return 401 (invalid credentials) not 500
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Employees_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/employees");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SwaggerJson_IsAccessible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
