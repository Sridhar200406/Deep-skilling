using System.Text;
using Caching;
using DepartmentService.Data;
using DepartmentService.EventHandlers;
using DepartmentService.Interfaces;
using DepartmentService.Repositories;
using HealthChecks;
using Logging;
using Messaging.Consumer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Monitoring;
using Serilog;
using Shared.Events;

Log.Logger = new Serilog.LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (Day 5) ───────────────────────────────────────────────────────
    builder.Host.UseSerilog(SerilogConfiguration.Configure("DepartmentService"));

    // ── Database ──────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<DepartmentDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── Repository + AutoMapper ───────────────────────────────────────────────
    builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
    builder.Services.AddAutoMapper(typeof(Program));

    // ── Redis Cache ───────────────────────────────────────────────────────────
    var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddStackExchangeRedisCache(opt =>
    {
        opt.Configuration = redisConn;
        opt.InstanceName  = "DepartmentService:";
    });
    builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

    // ── RabbitMQ Consumer ─────────────────────────────────────────────────────
    builder.Services.AddScoped<IEventHandler<EmployeeCreatedEvent>, EmployeeCreatedEventHandler>();
    builder.Services.AddScoped<IEventHandler<EmployeeUpdatedEvent>, EmployeeUpdatedEventHandler>();
    builder.Services.AddScoped<IEventHandler<EmployeeDeletedEvent>, EmployeeDeletedEventHandler>();

    var rabbitSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQConsumerSettings>() ?? new RabbitMQConsumerSettings();
    builder.Services.AddSingleton(rabbitSettings);
    builder.Services.AddHostedService<RabbitMQConsumerService>();

    // ── JWT ───────────────────────────────────────────────────────────────────
    var jwt = builder.Configuration.GetSection("JwtSettings");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
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

    // ── Health Checks ─────────────────────────────────────────────────────────
    builder.Services.AddAppHealthChecks(
        sqlConnectionString:   builder.Configuration.GetConnectionString("DefaultConnection")!,
        redisConnectionString: redisConn,
        rabbitMqHost:          builder.Configuration["RabbitMQ:Host"] ?? "localhost",
        rabbitMqPort:          builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
        rabbitMqUser:          builder.Configuration["RabbitMQ:UserName"] ?? "guest",
        rabbitMqPass:          builder.Configuration["RabbitMQ:Password"] ?? "guest");

    // ── OpenTelemetry + Metrics (Day 5) ───────────────────────────────────────
    builder.Services.AddAppTracing("DepartmentService");
    builder.Services.AddAppMetrics("DepartmentService");

    // ── Swagger ───────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "DepartmentService API (Day 5 — Observability)", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Bearer", Name = "Authorization", In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DepartmentDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    // ── Serilog request logging ───────────────────────────────────────────────
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        opts.EnrichDiagnosticContext = (dc, ctx) =>
        {
            dc.Set("CorrelationId",
                ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? ctx.TraceIdentifier);
        };
    });

    app.UseMiddleware<LoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DepartmentService v1"));
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapAppHealthChecks();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DepartmentService failed to start.");
}
finally
{
    Log.CloseAndFlush();
}
