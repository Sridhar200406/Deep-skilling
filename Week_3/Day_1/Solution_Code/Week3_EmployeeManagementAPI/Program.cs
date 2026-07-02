using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. REGISTER SERVICES (Dependency Injection Container)
// ============================================================

// Register controllers (scans for [ApiController] classes)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Preserve property names as-is (PascalCase) in JSON output
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        // Prevent infinite loops from circular navigation properties
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Register AppDbContext with SQL Server provider
// Connection string is read from appsettings.json → "DefaultConnection"
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register EmployeeService with Scoped lifetime
// (one instance per HTTP request — correct for DbContext-dependent services)
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Register Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "Employee Management API",
        Version     = "v1",
        Description = "Week 3 Day 1 — ASP.NET Core Web API for Employee Management System",
        Contact     = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name  = "Deep Skilling Week 3",
            Email = "admin@company.com"
        }
    });

    // Include XML comments in Swagger UI (generated from /// summaries)
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
// 3. CONFIGURE HTTP REQUEST PIPELINE (Middleware)
// ============================================================

// Enable Swagger in Development AND Production for demo purposes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
    c.RoutePrefix = string.Empty;   // Swagger UI available at root URL "/"
});

app.UseHttpsRedirection();

app.UseAuthorization();

// Map controller routes (e.g. [Route("api/[controller]")])
app.MapControllers();

// ============================================================
// 4. AUTO-MIGRATE DATABASE ON STARTUP (optional convenience)
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();   // Creates DB + tables if they don't exist
}

app.Run();
