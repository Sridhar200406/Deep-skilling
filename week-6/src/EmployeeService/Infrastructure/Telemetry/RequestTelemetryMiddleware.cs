using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace EmployeeService.Infrastructure.Telemetry;

/// <summary>
/// Custom middleware that enriches Application Insights request telemetry with:
///   - Authenticated user identity
///   - Request correlation ID
///   - Custom properties for filtering in Azure Portal
///
/// Placed after UseAuthentication so the user claim is available.
/// </summary>
public class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<RequestTelemetryMiddleware> _logger;

    public RequestTelemetryMiddleware(
        RequestDelegate next,
        TelemetryClient telemetryClient,
        ILogger<RequestTelemetryMiddleware> logger)
    {
        _next = next;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Enrich Application Insights telemetry
            var requestTelemetry = context.Features.Get<RequestTelemetry>();
            if (requestTelemetry != null)
            {
                // Tag with authenticated user
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var username = context.User.Identity.Name ?? "unknown";
                    requestTelemetry.Context.User.AuthenticatedUserId = username;
                    requestTelemetry.Properties["AuthenticatedUser"] = username;

                    var role = context.User.Claims
                        .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                    if (role != null)
                        requestTelemetry.Properties["UserRole"] = role;
                }

                // Add correlation ID for tracing across services
                var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                    ?? Activity.Current?.TraceId.ToString()
                    ?? Guid.NewGuid().ToString();

                requestTelemetry.Properties["CorrelationId"] = correlationId;
                context.Response.Headers["X-Correlation-ID"] = correlationId;

                // Tag slow requests for easy filtering
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    requestTelemetry.Properties["SlowRequest"] = "true";
                    _logger.LogWarning("Slow request detected: {Method} {Path} took {Elapsed}ms",
                        context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
                }
            }
        }
    }
}

public static class RequestTelemetryMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTelemetryEnrichment(this IApplicationBuilder app)
        => app.UseMiddleware<RequestTelemetryMiddleware>();
}
