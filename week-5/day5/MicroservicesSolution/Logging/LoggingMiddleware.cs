using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Logging
{
    /// <summary>
    /// HTTP request/response logging middleware.
    /// Logs every incoming request and outgoing response with:
    ///   Method | Path | StatusCode | ElapsedMs | CorrelationId | RequestId
    ///
    /// Injects CorrelationId into log context and response headers.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Resolve or generate CorrelationId
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                                ?? Guid.NewGuid().ToString("N")[..12];

            var requestId = context.TraceIdentifier;

            context.Response.Headers["X-Correlation-Id"] = correlationId;

            // Push both IDs into Serilog's LogContext so they appear in every log line
            using var correlationProp = Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId);
            using var requestProp     = Serilog.Context.LogContext.PushProperty("RequestId",     requestId);

            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "→ {Method} {Path}{Query} CorrelationId={CorrelationId} RequestId={RequestId}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                correlationId,
                requestId);

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var level = context.Response.StatusCode >= 500
                    ? Microsoft.Extensions.Logging.LogLevel.Error
                    : context.Response.StatusCode >= 400
                        ? Microsoft.Extensions.Logging.LogLevel.Warning
                        : Microsoft.Extensions.Logging.LogLevel.Information;

                _logger.Log(level,
                    "← {StatusCode} {Method} {Path} {ElapsedMs}ms CorrelationId={CorrelationId}",
                    context.Response.StatusCode,
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds,
                    correlationId);
            }
        }
    }
}
