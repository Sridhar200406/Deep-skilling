using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Mapping;
using Week3_EmployeeManagementAPI.Middleware;
using Week3_EmployeeManagementAPI.Repositories;
using Week3_EmployeeManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. REGISTER SERVICES — Dependency Injection Container
// ============================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Register AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository and Service
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(EmployeeProfile).Assembly);

// ── Day 6: Register JwtService ────────────────────────────────
// Singleton — stateless, only reads config and generates tokens
builder.Services.AddSingleton<JwtService>();
// ─────────────────────────────────────────────────────────────

// ── Day 6: Configure JWT Authentication ──────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is missing from appsettings.json");

builder.Services.AddAuthentication(options =>
{
    // Set JWT Bearer as the default scheme for both Authentication and Challenge
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,   // reject expired tokens
        ValidateIssuerSigningKey = true,   // verify signature
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew                = TimeSpan.Zero  // no tolerance for expiry
    };
});

builder.Services.AddAuthorization();
// ─────────────────────────────────────────────────────────────

// ── Day 6: Configure Swagger with JWT Bearer support ─────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Employee Management API",
        Version     = "v1",
        Description = "Week 3 Day 6 — JWT Authentication and Authorization"
    });

    // Define the Bearer security scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  =
            "Enter your JWT token below.\n\n" +
            "Example: eyJhbGciOiJIUzI1NiIs..."
    });

    // Apply Bearer security globally to all endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});
// ─────────────────────────────────────────────────────────────

// ============================================================
// 2. BUILD THE APP
// ============================================================
var app = builder.Build();

// ============================================================
// 3. CONFIGURE HTTP REQUEST PIPELINE — ORDER MATTERS
// ============================================================

// Global exception handler — MUST be first
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

// Authentication BEFORE Authorization — order is critical
app.UseAuthentication();   // ← Day 6: validates the JWT token
app.UseAuthorization();    // ← Day 6: enforces [Authorize] attributes

app.MapControllers();

// ============================================================
// 4. AUTO-MIGRATE DATABASE ON STARTUP
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
