namespace EmployeeService.Infrastructure.Middleware;

/// <summary>
/// Processes APIM-injected headers so downstream services have access to
/// authenticated user context without re-validating the JWT themselves.
///
/// When requests flow through Azure API Management:
///   Client → APIM (validates JWT) → Employee Service
///
/// APIM injects these headers after validation:
///   X-User-Id       → JWT sub claim (user ID)
///   X-User-Role     → JWT role claim
///   X-Username      → JWT unique_name claim
///   X-Correlation-ID → Request tracing ID
///   X-API-Version   → Requested API version
///
/// These are trusted because they come from APIM, which sits on the
/// internal network. External clients cannot spoof them since APIM
/// strips and re-adds them after JWT validation.
/// </summary>
public class ApimHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApimHeaderMiddleware> _logger;

    public ApimHeaderMiddleware(RequestDelegate next, ILogger<ApimHeaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Read APIM-injected headers
        var userId        = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var userRole      = context.Request.Headers["X-User-Role"].FirstOrDefault();
        var username      = context.Request.Headers["X-Username"].FirstOrDefault();
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();
        var apiVersion    = context.Request.Headers["X-API-Version"].FirstOrDefault() ?? "v1";

        // Store in HttpContext items for controllers to use
        if (!string.IsNullOrEmpty(userId))       context.Items["ApimUserId"]   = userId;
        if (!string.IsNullOrEmpty(userRole))     context.Items["ApimUserRole"] = userRole;
        if (!string.IsNullOrEmpty(username))     context.Items["ApimUsername"] = username;
        context.Items["CorrelationId"] = correlationId;
        context.Items["ApiVersion"]    = apiVersion;

        // Echo correlation ID in response for tracing
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });

        _logger.LogDebug(
            "APIM headers: UserId={UserId}, Role={Role}, CorrelationId={CorrelationId}, Version={Version}",
            userId ?? "none", userRole ?? "none", correlationId, apiVersion);

        await _next(context);
    }
}

public static class ApimHeaderMiddlewareExtensions
{
    /// <summary>Register APIM header processing middleware.</summary>
    public static IApplicationBuilder UseApimHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<ApimHeaderMiddleware>();
}
