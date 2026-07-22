using Azure.Identity;
using EmployeeService.Application.Services;
using EmployeeService.Infrastructure.Auth;
using EmployeeService.Infrastructure.Azure;
using EmployeeService.Infrastructure.Data;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json;

// ─────────────────────────────────────────────
// STEP 1: Bootstrap Serilog (before host builds)
// ─────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("Starting EmployeeService...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─────────────────────────────────────────────
    // STEP 2: Add Azure Key Vault to configuration
    //  (loads secrets BEFORE services are registered)
    // ─────────────────────────────────────────────
    builder.Configuration.AddAzureKeyVaultIfConfigured(builder.Configuration);

    // ─────────────────────────────────────────────
    // STEP 3: Configure Serilog with App Insights sink
    // ─────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, loggerConfig) =>
    {
        loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/employee-service-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");

        // Add Application Insights sink if connection string is configured
        var aiConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(aiConnectionString))
        {
            loggerConfig.WriteTo.ApplicationInsights(
                services.GetRequiredService<TelemetryConfiguration>(),
                TelemetryConverter.Traces);
        }
    });

    // ─────────────────────────────────────────────
    // STEP 4: Application Insights
    // ─────────────────────────────────────────────
    var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrEmpty(aiConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = aiConnectionString;
        });
        Log.Information("Application Insights telemetry enabled.");
    }
    else
    {
        Log.Warning("Application Insights connection string is not configured. Telemetry will not be sent.");
    }

    // ─────────────────────────────────────────────
    // STEP 5: Database (Azure SQL / LocalDB)
    // ─────────────────────────────────────────────
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' is not set.");

    builder.Services.AddDbContext<EmployeeDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
        });
    });

    // ─────────────────────────────────────────────
    // STEP 6: JWT Authentication
    // ─────────────────────────────────────────────
    var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
        ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Return 401 as JSON
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new { success = false, message = "Unauthorized. Please provide a valid JWT token." });
                    return context.Response.WriteAsync(result);
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new { success = false, message = "Forbidden. You do not have permission to perform this action." });
                    return context.Response.WriteAsync(result);
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
    });

    // ─────────────────────────────────────────────
    // STEP 7: Azure Blob Storage
    // ─────────────────────────────────────────────
    builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

    // ─────────────────────────────────────────────
    // STEP 8: Redis Cache
    // ─────────────────────────────────────────────
    var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "EmployeeService:";
        });
        Log.Information("Redis cache configured.");
    }
    else
    {
        // Fallback to in-memory cache for development
        builder.Services.AddDistributedMemoryCache();
        Log.Warning("Redis is not configured. Using in-memory distributed cache.");
    }

    // ─────────────────────────────────────────────
    // STEP 9: Application Services (DI)
    // ─────────────────────────────────────────────
    builder.Services.AddScoped<IEmployeeAppService, EmployeeAppService>();
    builder.Services.AddScoped<IAuthAppService, AuthAppService>();
    builder.Services.AddScoped<IDocumentAppService, DocumentAppService>();
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

    // ─────────────────────────────────────────────
    // STEP 10: Health Checks
    // ─────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString,
            name: "sql-server",
            tags: new[] { "db", "sql", "azure-sql" })
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"),
            tags: new[] { "api" });

    // Add Blob Storage health check only if configured
    var blobConnectionString = builder.Configuration["AzureBlobStorage:ConnectionString"];
    if (!string.IsNullOrEmpty(blobConnectionString) && blobConnectionString != "UseDevelopmentStorage=true")
    {
        builder.Services.AddHealthChecks()
            .AddAzureBlobStorage(blobConnectionString, name: "azure-blob-storage",
                tags: new[] { "storage", "azure" });
    }

    // ─────────────────────────────────────────────
    // STEP 11: OpenTelemetry
    // ─────────────────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService("EmployeeService",
                serviceVersion: "1.0.0",
                serviceInstanceId: Environment.MachineName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options => options.SetDbStatementForText = true)
            .AddConsoleExporter());

    // ─────────────────────────────────────────────
    // STEP 12: Controllers + Swagger
    // ─────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Employee Management API",
            Version = "v1",
            Description = "ASP.NET Core 8 Employee Management API with Azure Cloud Integration (Week 6)",
            Contact = new OpenApiContact { Name = "Employee Management Team" }
        });

        // JWT Bearer security scheme for Swagger UI
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token in the field below.\n\nExample: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments if available
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    });

    // CORS — tighten in production
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        options.AddPolicy("ProductionCors", policy =>
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });

    // ─────────────────────────────────────────────
    // BUILD
    // ─────────────────────────────────────────────
    var app = builder.Build();

    // ─────────────────────────────────────────────
    // STEP 13: Run EF Migrations on startup (dev only)
    //   In production, run migrations via CI/CD pipeline
    // ─────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        try
        {
            db.Database.Migrate();
            Log.Information("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not apply database migrations. Ensure the database server is running.");
        }

        // Initialize Blob Storage container
        var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>() as BlobStorageService;
        if (blobService != null)
        {
            try
            {
                await blobService.InitializeAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not initialize Azure Blob Storage container. Ensure the storage emulator or Azure Storage is running.");
            }
        }
    }

    // ─────────────────────────────────────────────
    // STEP 14: Middleware Pipeline
    // ─────────────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
            options.RoutePrefix = string.Empty; // Swagger at root
            options.DisplayRequestDuration();
        });
    }
    else
    {
        // Swagger available in production too for testing convenience
        // Remove in fully locked-down environments
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();

    app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "ProductionCors");

    app.UseAuthentication();
    app.UseAuthorization();

    // Health Check endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("api"),
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("storage"),
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapControllers();

    Log.Information("EmployeeService started. Environment: {Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "EmployeeService terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
