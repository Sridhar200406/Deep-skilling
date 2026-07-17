using System.Text.Json;

namespace EmployeeService.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        { _next = next; _logger = logger; _env = env; }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try { await _next(ctx); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception at {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
                ctx.Response.ContentType = "application/json";
                var (code, msg) = ex switch
                {
                    ArgumentException           => (400, ex.Message),
                    KeyNotFoundException        => (404, ex.Message),
                    UnauthorizedAccessException => (401, "Unauthorized."),
                    _                           => (500, "An unexpected error occurred.")
                };
                ctx.Response.StatusCode = code;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false, statusCode = code, message = msg,
                    detail = _env.IsDevelopment() ? ex.ToString() : null
                }));
            }
        }
    }
}
