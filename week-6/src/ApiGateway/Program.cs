using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Serilog.Events;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext().Enrich.WithEnvironmentName()
    .WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Resolve environment variable placeholders in ocelot.json ──────────
    // Container Apps injects service hostnames via env vars at runtime
    ResolveOcelotEnvVars(builder);

    // ── Key Vault ──────────────────────────────────────────────────────────
    var vaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
    if (!string.IsNullOrEmpty(vaultUri))
        builder.Configuration.AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());

    // ── Serilog ────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, _, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext().Enrich.WithEnvironmentName().WriteTo.Console());

    // ── App Insights ───────────────────────────────────────────────────────
    var aiConn = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrEmpty(aiConn))
        builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = aiConn);

    // ── JWT Authentication (forwarded to downstream services) ──────────────
    var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
    if (!string.IsNullOrEmpty(jwtKey))
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer("Bearer", opt => opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true, ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidateAudience = true, ValidAudience = builder.Configuration["JwtSettings:Audience"],
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            });
    }
    builder.Services.AddAuthorization();

    // ── Health Checks ──────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddCheck("gateway-self",
            () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Gateway running"),
            tags: new[] { "api" });

    // ── Ocelot ─────────────────────────────────────────────────────────────
    builder.Services.AddOcelot(builder.Configuration);

    builder.Services.AddCors(o => o.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health/live", () => Results.Ok(new
    {
        status = "Healthy",
        service = "ApiGateway",
        timestamp = DateTime.UtcNow
    }));

    app.MapGet("/health/ready", () => Results.Ok(new
    {
        status = "Ready",
        service = "ApiGateway",
        timestamp = DateTime.UtcNow
    }));

    await app.UseOcelot();

    Log.Information("ApiGateway started.");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException) { Log.Fatal(ex, "ApiGateway crashed."); throw; }
finally { Log.CloseAndFlush(); }

// ── Helper: replace ${VAR} placeholders in ocelot.json with env vars ────────
static void ResolveOcelotEnvVars(WebApplicationBuilder builder)
{
    var ocelotPath = Path.Combine(Directory.GetCurrentDirectory(), "ocelot.json");
    if (!File.Exists(ocelotPath)) return;

    var content = File.ReadAllText(ocelotPath);

    // Replace ${ENV_VAR} with actual environment variable values
    var envVars = new[]
    {
        ("AUTH_SERVICE_HOST",       "auth-service"),
        ("EMPLOYEE_SERVICE_HOST",   "employee-service"),
        ("DEPARTMENT_SERVICE_HOST", "department-service")
    };

    foreach (var (key, defaultVal) in envVars)
    {
        var value = Environment.GetEnvironmentVariable(key) ?? defaultVal;
        content = content.Replace($"${{{key}}}", value);
    }

    var resolved = Path.Combine(Path.GetTempPath(), "ocelot.resolved.json");
    File.WriteAllText(resolved, content);

    builder.Configuration.AddJsonFile(resolved, optional: false, reloadOnChange: false);
}
