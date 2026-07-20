using System.Text;
using ApiGateway.Middleware;
using Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Monitoring;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

Log.Logger = new Serilog.LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    // ── Serilog (Day 5) ───────────────────────────────────────────────────────
    builder.Host.UseSerilog(SerilogConfiguration.Configure("ApiGateway"));

    var jwt = builder.Configuration.GetSection("JwtSettings");
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer("Bearer", opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)),
                ValidateIssuer = true,  ValidIssuer   = jwt["Issuer"],
                ValidateAudience = true, ValidAudience = jwt["Audience"],
                ValidateLifetime = true, ClockSkew     = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddCors(o => o.AddPolicy("GatewayCors",
        p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    // ── OpenTelemetry + Metrics (Day 5) ───────────────────────────────────────
    builder.Services.AddAppTracing("ApiGateway");
    builder.Services.AddAppMetrics("ApiGateway");

    builder.Services.AddOcelot(builder.Configuration);

    var app = builder.Build();

    app.UseCors("GatewayCors");

    // ── Serilog request logging ───────────────────────────────────────────────
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "GATEWAY {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms";
        opts.EnrichDiagnosticContext = (dc, ctx) =>
        {
            dc.Set("CorrelationId",
                ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? ctx.TraceIdentifier);
        };
    });

    app.UseMiddleware<LoggingMiddleware>();
    app.UseMiddleware<GatewayExceptionMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    await app.UseOcelot();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ApiGateway failed to start.");
}
finally
{
    Log.CloseAndFlush();
}
