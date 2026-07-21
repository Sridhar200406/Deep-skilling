using System.Net.Http.Headers;
using System.Text.Json;

namespace EmployeeService.HttpClients
{
    /// <summary>
    /// Typed HttpClient for DepartmentService communication.
    /// Day 4: Polly retry/circuit-breaker policies are applied at registration
    /// in Program.cs via IHttpClientFactory — no changes needed here.
    /// The client simply uses the HttpClient injected by the factory.
    /// </summary>
    public class DepartmentServiceClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger<DepartmentServiceClient> _logger;

        public DepartmentServiceClient(
            HttpClient http,
            IHttpContextAccessor accessor,
            ILogger<DepartmentServiceClient> logger)
        {
            _http     = http;
            _accessor = accessor;
            _logger   = logger;
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
        {
            // Forward the caller's JWT for service-to-service auth
            var token = _accessor.HttpContext?.Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");
            if (!string.IsNullOrEmpty(token))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("DepartmentServiceClient: GET /api/departments/{Id}", id);

            try
            {
                var res = await _http.GetAsync($"/api/departments/{id}");

                _logger.LogInformation(
                    "DepartmentServiceClient: Response {Status} for dept {Id}",
                    (int)res.StatusCode, id);

                if (!res.IsSuccessStatusCode) return null;

                var json = await res.Content.ReadAsStringAsync();
                var env  = JsonSerializer.Deserialize<DeptEnvelope>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return env?.Data;
            }
            catch (Exception ex)
            {
                // Polly fallback already handles circuit-open / timeout — this catches the rest
                _logger.LogError(ex, "DepartmentServiceClient: Request failed for dept {Id}", id);
                return null;
            }
        }

        // ── inner types ──────────────────────────────────────────────────────
        private class DeptEnvelope
        {
            public bool          Success { get; set; }
            public DepartmentDto? Data   { get; set; }
        }
    }

    public class DepartmentDto
    {
        public int     DepartmentId { get; set; }
        public string  Name         { get; set; } = string.Empty;
        public string? Location     { get; set; }
        public bool    IsActive     { get; set; }
    }
}
