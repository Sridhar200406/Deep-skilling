using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. REGISTER SERVICES (Dependency Injection Container)
// ============================================================

// Register controllers — [ApiController] handles automatic 400 on invalid model
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Keep PascalCase property names in JSON output
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        // Prevent infinite loops from circular navigation properties
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Register AppDbContext with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register EmployeeService — Scoped: one instance per HTTP request
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Register Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "Employee Management API",
        Version     = "v1",
        Description = "Week 3 Day 2 — Full CRUD REST API for Employee Management System",
        Contact     = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name  = "Deep Skilling Week 3",
            Email = "admin@company.com"
        }
    });

    // Include XML doc comments in Swagger (/// summaries on actions)
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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
    c.RoutePrefix = string.Empty;   // Swagger UI at root "/"
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
