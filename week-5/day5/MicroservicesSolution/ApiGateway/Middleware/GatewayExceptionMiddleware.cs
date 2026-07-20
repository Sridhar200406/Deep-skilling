using System.Text.Json;

namespace ApiGateway.Middleware
{
    public class GatewayExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GatewayExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GatewayExceptionMiddleware(RequestDelegate next,
            ILogger<GatewayExceptionMiddleware> logger, IHostEnvironment env)
        { _next = next; _logger = logger; _env = env; }

        public async Task InvokeAsync(HttpContext context)
        {
            try { await _next(context); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gateway exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                if (!context.Response.HasStarted)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode  = 500;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        success = false, statusCode = 500,
                        message = "API Gateway encountered an error.",
                        detail  = _env.IsDevelopment() ? ex.ToString() : null
                    }));
                }
            }
        }
    }
}
