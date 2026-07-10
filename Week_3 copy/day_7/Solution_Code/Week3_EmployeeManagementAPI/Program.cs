using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
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
// 1. CORE MVC
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// ============================================================
// 2. DATABASE
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// 3. DEPENDENCY INJECTION
// ============================================================
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddAutoMapper(typeof(EmployeeProfile).Assembly);
builder.Services.AddSingleton<JwtService>();

// ============================================================
// 4. JWT AUTHENTICATION
// ============================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey missing from appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew                = TimeSpan.Zero
    };
});

// ============================================================
// 5. ROLE-BASED AUTHORIZATION POLICIES (Day 7)
// ============================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",        policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrManager",   policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("AllRoles",         policy => policy.RequireRole("Admin", "Manager", "Employee"));
});

// ============================================================
// 6. API VERSIONING (Day 7)
// ============================================================
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion                = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions                = true;   // adds api-supported-versions header
    options.ApiVersionReader                 = new UrlSegmentApiVersionReader(); // /api/v1/...
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat           = "'v'VVV";   // v1, v2, ...
    options.SubstituteApiVersionInUrl = true;
});

// ============================================================
// 7. SWAGGER WITH JWT + API VERSIONING (Day 7)
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // One Swagger doc per API version
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Employee Management API",
        Version     = "v1",
        Description =
            "## Week 3 Day 7 — Final API\n\n" +
            "### Features\n" +
            "- Full CRUD for Employees\n" +
            "- JWT Bearer Authentication\n" +
            "- Role-Based Authorization (Admin / Manager / Employee)\n" +
            "- API Versioning (/api/v1/...)\n" +
            "- Global Exception Handling\n" +
            "- Standardized ApiResponse<T> wrapper\n\n" +
            "### Role Permissions\n" +
            "| Role     | GET | POST | PUT | DELETE |\n" +
            "|----------|-----|------|-----|--------|\n" +
            "| Admin    | ✅  | ✅   | ✅  | ✅     |\n" +
            "| Manager  | ✅  | ✅   | ✅  | ❌     |\n" +
            "| Employee | ✅  | ❌   | ❌  | ❌     |\n\n" +
            "### Demo Credentials\n" +
            "| Username | Password      | Role     |\n" +
            "|----------|---------------|----------|\n" +
            "| admin    | Admin@123     | Admin    |\n" +
            "| manager  | Manager@123   | Manager  |\n" +
            "| employee | Employee@123  | Employee |",
        Contact = new OpenApiContact
        {
            Name  = "Deep Skilling Week 3",
            Email = "admin@company.com"
        }
    });

    // JWT Bearer security definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  =
            "**Step 1:** Call `POST /api/v1/auth/login` with your credentials.\n\n" +
            "**Step 2:** Copy the `Token` value from the response.\n\n" +
            "**Step 3:** Click the **Authorize** button and paste your token.\n\n" +
            "**Format:** `Bearer eyJhbGciOiJIUzI1NiIs...`"
    });

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

// ============================================================
// 8. BUILD
// ============================================================
var app = builder.Build();

// ============================================================
// 9. MIDDLEWARE PIPELINE — ORDER MATTERS
// ============================================================

// 1st — global exception handler wraps everything
app.UseMiddleware<ExceptionMiddleware>();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Get versioned descriptions
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(d => d.ApiVersion))
    {
        c.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"Employee Management API {description.GroupName.ToUpper()}");
    }
    c.RoutePrefix              = string.Empty;   // Swagger at root /
    c.DocumentTitle            = "Employee Management API";
    c.DisplayRequestDuration();                  // shows response time in Swagger UI
});

app.UseHttpsRedirection();
app.UseAuthentication();   // validate JWT token
app.UseAuthorization();    // enforce [Authorize] + role checks
app.MapControllers();

// ============================================================
// 10. AUTO-MIGRATE DATABASE ON STARTUP
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
