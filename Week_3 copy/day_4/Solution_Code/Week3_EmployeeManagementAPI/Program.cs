using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Mapping;
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

// Register AppDbContext — Scoped (one per HTTP request)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository — Scoped
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Register Service — Scoped
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// ── Day 4: Register AutoMapper ────────────────────────────────
// AddAutoMapper scans the assembly for all Profile classes (EmployeeProfile)
// and registers IMapper as a Singleton automatically.
builder.Services.AddAutoMapper(typeof(EmployeeProfile).Assembly);
// ─────────────────────────────────────────────────────────────

// Register Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "Employee Management API",
        Version     = "v1",
        Description = "Week 3 Day 4 — DTOs, AutoMapper, and Model Validation",
        Contact     = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name  = "Deep Skilling Week 3",
            Email = "admin@company.com"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ============================================================
// 2. BUILD THE APP
// ============================================================
var app = builder.Build();

// ============================================================
// 3. CONFIGURE HTTP REQUEST PIPELINE
// ============================================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthorization();
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
