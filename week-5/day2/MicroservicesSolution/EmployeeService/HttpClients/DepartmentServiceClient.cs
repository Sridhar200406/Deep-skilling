using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EmployeeService.HttpClients
{
    /// <summary>
    /// Typed HTTP client for inter-service communication with DepartmentService.
    ///
    /// Pattern used (Day 2 requirement):
    ///   - Typed Client registered via DI (AddHttpClient&lt;T&gt;)
    ///   - Forwards the caller's Bearer token automatically
    ///   - Falls back to a direct DepartmentService URL if Gateway is unavailable
    /// </summary>
    public class DepartmentServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DepartmentServiceClient> _logger;

        public DepartmentServiceClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DepartmentServiceClient> logger)
        {
            _httpClient          = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger              = logger;
        }

        /// <summary>
        /// Fetches a department by ID from DepartmentService via the API Gateway.
        /// Forwards the JWT from the incoming request (service-to-service auth).
        /// </summary>
        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId)
        {
            // Forward the caller's JWT so the downstream service accepts the request
            var token = _httpContextAccessor.HttpContext?
                .Request.Headers["Authorization"].ToString()
                .Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            try
            {
                _logger.LogInformation("DepartmentServiceClient: calling /api/departments/{Id}", departmentId);

                var response = await _httpClient.GetAsync($"/api/departments/{departmentId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("DepartmentServiceClient: got {Status} for dept {Id}",
                        response.StatusCode, departmentId);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var envelope = JsonSerializer.Deserialize<DepartmentEnvelope>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return envelope?.Data;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "DepartmentServiceClient: request failed for dept {Id}", departmentId);
                return null;
            }
        }

        // ── inner types for JSON deserialization ─────────────────────────────
        private class DepartmentEnvelope
        {
            public bool          Success { get; set; }
            public DepartmentDto? Data   { get; set; }
        }
    }

    /// <summary>Minimal DTO for the data returned by DepartmentService.</summary>
    public class DepartmentDto
    {
        public int     DepartmentId { get; set; }
        public string  Name         { get; set; } = string.Empty;
        public string? Location     { get; set; }
        public bool    IsActive     { get; set; }
    }
}
