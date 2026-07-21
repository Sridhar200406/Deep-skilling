using System.Diagnostics;

namespace EmployeeService.Logging
{
    /// <summary>
    /// Middleware that logs all incoming HTTP requests and outgoing responses.
    /// Logs: method, path, status code, elapsed time, and correlation ID.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        { _next = next; _logger = logger; }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                                ?? Guid.NewGuid().ToString("N")[..8];

            context.Response.Headers["X-Correlation-Id"] = correlationId;

            var sw = Stopwatch.StartNew();
            _logger.LogInformation(
                "→ REQUEST  {Method} {Path} CorrelationId={CorrelationId}",
                context.Request.Method, context.Request.Path, correlationId);

            await _next(context);

            sw.Stop();
            _logger.LogInformation(
                "← RESPONSE {StatusCode} {Method} {Path} {ElapsedMs}ms CorrelationId={CorrelationId}",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds,
                correlationId);
        }
    }
}
