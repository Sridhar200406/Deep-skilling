using System.Text;
using EmployeeService.Data;
using EmployeeService.HttpClients;
using EmployeeService.Interfaces;
using EmployeeService.Mapping;
using EmployeeService.Messaging;
using EmployeeService.Middleware;
using EmployeeService.Repositories;
using EmployeeService.Services;
using Messaging.Producer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<EmployeeDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repository + Service + AutoMapper ────────────────────────────────────────
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeBusinessService>();
builder.Services.AddAutoMapper(typeof(EmployeeProfile));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// ── Typed HTTP Client for DepartmentService ───────────────────────────────────
var deptUrl = builder.Configuration["ServiceUrls:DepartmentService"] ?? "http://localhost:5003";
builder.Services.AddHttpClient<DepartmentServiceClient>(c =>
{
    c.BaseAddress = new Uri(deptUrl);
    c.Timeout     = TimeSpan.FromSeconds(10);
});

// ── RabbitMQ Producer (Day 3) ─────────────────────────────────────────────────
var rabbitSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()
                    ?? new RabbitMQSettings();
builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddSingleton<RabbitMQProducer>();
builder.Services.AddScoped<EmployeeEventPublisher>();

// ── JWT ───────────────────────────────────────────────────────────────────────
var jwt      = builder.Configuration.GetSection("JwtSettings");
var secret   = jwt["Secret"]!;
var issuer   = jwt["Issuer"]!;
var audience = jwt["Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer           = true, ValidIssuer  = issuer,
            ValidateAudience         = true, ValidAudience = audience,
            ValidateLifetime         = true, ClockSkew    = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EmployeeService API (Day 3 — Messaging)", Version = "v1" });
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EmployeeService v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
