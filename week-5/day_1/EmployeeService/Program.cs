using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using EmployeeService.Data;
using EmployeeService.Interfaces;
using EmployeeService.Mapping;
using EmployeeService.Middleware;
using EmployeeService.Repositories;
using EmployeeService.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. DATABASE
// ============================================================
builder.Services.AddDbContext<EmployeeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// 2. DEPENDENCY INJECTION
// ============================================================
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeBusinessService>();
builder.Services.AddAutoMapper(typeof(EmployeeProfile).Assembly);
builder.Services.AddMemoryCache();

// ============================================================
// 3. CONTROLLERS
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// ============================================================
// 4. SWAGGER
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Microservice API",
        Version = "v1",
        Description = "Week 5 Day 1 — Employee Microservice with CRUD operations.",
        Contact = new OpenApiContact { Name = "Deep Skilling Week 5" }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ============================================================
// 5. BUILD & CONFIGURE PIPELINE
// ============================================================
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Microservice v1");
    c.RoutePrefix = string.Empty;
    c.DisplayRequestDuration();
});

app.UseHttpsRedirection();
app.MapControllers();

// ============================================================
// 6. AUTO-CREATE DATABASE ON STARTUP
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
    }
}

app.Run();
