using System.Text;
using Caching;
using EmployeeService.Data;
using EmployeeService.HttpClients;
using EmployeeService.Interfaces;
using EmployeeService.Logging;
using EmployeeService.Mapping;
using EmployeeService.Messaging;
using EmployeeService.Middleware;
using EmployeeService.Repositories;
using EmployeeService.Services;
using HealthChecks;
using Messaging.Producer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Resilience;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<EmployeeDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repository + Service + AutoMapper ────────────────────────────────────────
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeBusinessService>();
builder.Services.AddAutoMapper(typeof(EmployeeProfile));
builder.Services.AddHttpContextAccessor();

// ── Redis Distributed Cache (Day 4) ──────────────────────────────────────────
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = redisConn;
    opt.InstanceName  = "EmployeeService:";
});
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// ── Typed HTTP Client — DepartmentService with Polly (Day 4) ─────────────────
var deptUrl = builder.Configuration["ServiceUrls:DepartmentService"] ?? "http://localhost:5003";
var pollyLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Polly");

builder.Services.AddHttpClient<DepartmentServiceClient>(c =>
{
    c.BaseAddress = new Uri(deptUrl);
    c.Timeout     = TimeSpan.FromSeconds(30); // outer timeout (Polly inner = 10s)
})
.AddPolicyHandler(PollyConfiguration.GetFallbackPolicy(pollyLogger))
.AddPolicyHandler(PollyConfiguration.GetCircuitBreakerPolicy(pollyLogger))
.AddPolicyHandler(PollyConfiguration.GetRetryPolicy(pollyLogger))
.AddPolicyHandler(PollyConfiguration.GetTimeoutPolicy(10));

// ── RabbitMQ Producer ─────────────────────────────────────────────────────────
var rabbitSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()
                    ?? new RabbitMQSettings();
builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddSingleton<RabbitMQProducer>();
builder.Services.AddScoped<EmployeeEventPublisher>();

// ── JWT ───────────────────────────────────────────────────────────────────────
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

// ── Health Checks (Day 4) ─────────────────────────────────────────────────────
builder.Services.AddAppHealthChecks(
    sqlConnectionString:   builder.Configuration.GetConnectionString("DefaultConnection")!,
    redisConnectionString: redisConn,
    rabbitMqHost:          builder.Configuration["RabbitMQ:Host"] ?? "localhost",
    rabbitMqPort:          builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
    rabbitMqUser:          builder.Configuration["RabbitMQ:UserName"] ?? "guest",
    rabbitMqPass:          builder.Configuration["RabbitMQ:Password"] ?? "guest");

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EmployeeService API (Day 4 — Caching + Resilience)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Example: Bearer {token}",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ── Auto-create DB ────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<RequestLoggingMiddleware>();   // Day 4: structured request/response logging
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EmployeeService v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Health check endpoints (Day 4) ───────────────────────────────────────────
app.MapAppHealthChecks();

app.Run();
