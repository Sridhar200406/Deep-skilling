using System.Net;
using System.Text.Json;

namespace EmployeeService.Middleware
{
    /// <summary>Global exception handler — returns standardized JSON for any unhandled exception.</summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next   = next;
            _logger = logger;
            _env    = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try { await _next(context); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception at {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleAsync(context, ex);
            }
        }

        private async Task HandleAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            var (code, msg) = ex switch
            {
                ArgumentException           => (400, ex.Message),
                KeyNotFoundException        => (404, ex.Message),
                UnauthorizedAccessException => (401, "Unauthorized."),
                _                           => (500, "An unexpected error occurred.")
            };
            context.Response.StatusCode = code;
            var body = new
            {
                Success    = false,
                StatusCode = code,
                Message    = msg,
                Detail     = _env.IsDevelopment() ? ex.ToString() : null
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
