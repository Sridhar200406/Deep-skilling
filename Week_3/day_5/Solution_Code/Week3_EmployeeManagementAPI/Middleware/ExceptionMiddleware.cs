using System.Net;
using System.Text.Json;
using Week3_EmployeeManagementAPI.Responses;

namespace Week3_EmployeeManagementAPI.Middleware
{
    /// <summary>
    /// Global Exception Handling Middleware.
    /// 
    /// How it works:
    ///   Every HTTP request passes through this middleware first.
    ///   It wraps the rest of the pipeline in a try/catch.
    ///   Any unhandled exception anywhere in the pipeline is caught here,
    ///   logged, and converted into a clean ApiResponse JSON body.
    /// 
    /// Benefits:
    ///   - No try/catch needed in controllers or services for unexpected errors.
    ///   - All error responses have the same ApiResponse shape.
    ///   - Stack traces never leak to the client in production.
    ///   - One central place to add monitoring/alerting hooks.
    /// 
    /// Registration order matters — must be the FIRST middleware in Program.cs
    /// so it wraps everything else.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next   = next;
            _logger = logger;
            _env    = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Pass control to the next middleware/controller
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the full exception with stack trace
                _logger.LogError(ex,
                    "Unhandled exception for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Determine status code and message based on exception type
            var (statusCode, message) = exception switch
            {
                ArgumentNullException      => (HttpStatusCode.BadRequest,      "A required argument was missing."),
                ArgumentException          => (HttpStatusCode.BadRequest,      exception.Message),
                KeyNotFoundException       => (HttpStatusCode.NotFound,        exception.Message),
                InvalidOperationException  => (HttpStatusCode.BadRequest,      exception.Message),
                UnauthorizedAccessException=> (HttpStatusCode.Unauthorized,    "You are not authorized to perform this action."),
                _                          => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)statusCode;

            // In Development, include the exception detail for debugging.
            // In Production, only show a safe message — never expose stack traces.
            var detail = _env.IsDevelopment()
                ? exception.ToString()
                : null;

            var response = new
            {
                Success    = false,
                StatusCode = (int)statusCode,
                Message    = message,
                Data       = (object?)null,
                Errors     = detail != null ? new[] { detail } : null
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null  // Keep PascalCase consistent with API
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
