using System.Net;
using System.Text.Json;

namespace EmployeeService.Middleware
{
    /// <summary>Global exception handler middleware — catches unhandled exceptions and returns standardized JSON.</summary>
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
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                ArgumentNullException       => (HttpStatusCode.BadRequest,          "A required argument was missing."),
                ArgumentException           => (HttpStatusCode.BadRequest,          exception.Message),
                KeyNotFoundException        => (HttpStatusCode.NotFound,            exception.Message),
                InvalidOperationException   => (HttpStatusCode.BadRequest,          exception.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized,        "You are not authorized."),
                _                           => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            context.Response.StatusCode = (int)statusCode;

            var detail = _env.IsDevelopment() ? exception.ToString() : null;

            var response = new
            {
                Success    = false,
                StatusCode = (int)statusCode,
                Message    = message,
                Data       = (object?)null,
                Errors     = detail != null ? new[] { detail } : null
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = null }));
        }
    }
}
