using System.Net.Http.Headers;
using System.Text.Json;

namespace EmployeeService.HttpClients
{
    public class DepartmentServiceClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger<DepartmentServiceClient> _logger;

        public DepartmentServiceClient(HttpClient http, IHttpContextAccessor accessor,
            ILogger<DepartmentServiceClient> logger)
        { _http = http; _accessor = accessor; _logger = logger; }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
        {
            var token = _accessor.HttpContext?.Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");
            if (!string.IsNullOrEmpty(token))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var res = await _http.GetAsync($"/api/departments/{id}");
                if (!res.IsSuccessStatusCode) return null;
                var json = await res.Content.ReadAsStringAsync();
                var env = JsonSerializer.Deserialize<DeptEnvelope>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return env?.Data;
            }
            catch (Exception ex) { _logger.LogError(ex, "DepartmentServiceClient failed for id {Id}", id); return null; }
        }

        private class DeptEnvelope { public bool Success { get; set; } public DepartmentDto? Data { get; set; } }
    }

    public class DepartmentDto
    {
        public int    DepartmentId { get; set; }
        public string Name         { get; set; } = string.Empty;
        public string? Location    { get; set; }
        public bool   IsActive     { get; set; }
    }
}
