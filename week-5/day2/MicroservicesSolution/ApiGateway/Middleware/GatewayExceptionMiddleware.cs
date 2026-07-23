using System.Net;
using System.Text.Json;

namespace ApiGateway.Middleware
{
    /// <summary>
    /// Centralized error handling at the API Gateway level.
    /// Catches any unhandled exception in the Ocelot pipeline and returns
    /// a consistent JSON error envelope so clients always get the same shape.
    /// </summary>
    public class GatewayExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GatewayExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;  

        public GatewayExceptionMiddleware(
            RequestDelegate next,
            ILogger<GatewayExceptionMiddleware> logger,
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
                await _next(context);

                // Surface upstream 5xx errors as a friendly gateway response
                if (context.Response.StatusCode >= 500 && !context.Response.HasStarted)
                {
                    await WriteErrorAsync(context, context.Response.StatusCode,
                        "An upstream service returned an error.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in API Gateway for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                if (!context.Response.HasStarted)
                    await WriteErrorAsync(context, 500,
                        "The API Gateway encountered an unexpected error.",
                        _env.IsDevelopment() ? ex.ToString() : null);
            }
        }

        // ── helpers ─────────────────────────────────────────────────────────
        private static async Task WriteErrorAsync(
            HttpContext context, int statusCode, string message, string? detail = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = statusCode;

            var body = new
            {
                Success    = false,
                StatusCode = statusCode,
                Message    = message,
                Detail     = detail
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }
    }
}
