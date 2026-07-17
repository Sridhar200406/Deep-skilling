using System.Text;
using DepartmentService.Data;
using DepartmentService.EventHandlers;
using DepartmentService.Interfaces;
using DepartmentService.Repositories;
using Messaging.Consumer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Events;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<DepartmentDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repository + AutoMapper ───────────────────────────────────────────────────
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddAutoMapper(typeof(Program));

// ── Event Handlers (consumers) ────────────────────────────────────────────────
builder.Services.AddScoped<IEventHandler<EmployeeCreatedEvent>, EmployeeCreatedEventHandler>();
builder.Services.AddScoped<IEventHandler<EmployeeUpdatedEvent>, EmployeeUpdatedEventHandler>();
builder.Services.AddScoped<IEventHandler<EmployeeDeletedEvent>, EmployeeDeletedEventHandler>();

// ── RabbitMQ Consumer (BackgroundService) ─────────────────────────────────────
var rabbitSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQConsumerSettings>()
                    ?? new RabbitMQConsumerSettings();
builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddHostedService<RabbitMQConsumerService>();

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
            ValidateIssuer           = true, ValidIssuer   = issuer,
            ValidateAudience         = true, ValidAudience = audience,
            ValidateLifetime         = true, ClockSkew     = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DepartmentService API (Day 3 — Messaging)", Version = "v1" });
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
    var db = scope.ServiceProvider.GetRequiredService<DepartmentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DepartmentService v1"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
