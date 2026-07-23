using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DepartmentService.Data;
using DepartmentService.Interfaces;
using DepartmentService.Repositories;
using DepartmentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DepartmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDepartmentService, DepartmentBusinessService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Department Microservice API",
        Version = "v1",
        Description = "Week 5 Day 1 — Department Microservice with CRUD operations."
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Department Service v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DepartmentDbContext>();
    try { db.Database.EnsureCreated(); }
    catch (Exception ex) { scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogError(ex, "DB init error."); }
}

app.Run();
