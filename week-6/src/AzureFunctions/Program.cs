using EmployeeManagement.AzureFunctions.Configuration;
using EmployeeManagement.AzureFunctions.Data;
using EmployeeManagement.AzureFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ═══════════════════════════════════════════════════════════════════════════
// Azure Functions .NET 8 Isolated Worker — Program.cs
//
// Isolated Worker vs In-Process:
//   - Isolated (this file): Functions run in a separate process from the host.
//     Full .NET 8 support, latest ASP.NET Core middleware, custom DI, etc.
//   - In-Process: Functions run in the same process as the Functions host.
//     Limited to .NET 6, being retired.
//
// The isolated model is now the recommended approach for all new Functions.
// ═══════════════════════════════════════════════════════════════════════════

var host = new HostBuilder()
    // ── Step 1: Load Azure Key Vault into configuration ──────────────────────
    // Must happen before ConfigureFunctionsWorkerDefaults so services
    // can read secrets (DB connection string, JWT, Blob connection, etc.)
    .AddAzureKeyVault()

    // ── Step 2: Configure Functions worker ───────────────────────────────────
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        // Application Insights integration for Functions
        worker.AddApplicationInsights()
              .AddApplicationInsightsLogger();
    })

    // ── Step 3: Register all services (Dependency Injection) ─────────────────
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // ── Application Insights ──────────────────────────────────────────
        var aiConnectionString = config["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(aiConnectionString))
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = aiConnectionString;
            });
        }

        // ── Azure SQL Database (EF Core) ──────────────────────────────────
        // Connection string comes from Key Vault in production,
        // local.settings.json in development — never hardcoded.
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDbContext<FunctionDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });
            });
        }
        else
        {
            // Fallback to InMemory for local dev without SQL Server
            services.AddDbContext<FunctionDbContext>(options =>
                options.UseInMemoryDatabase("FunctionDb_Dev"));
        }

        // ── Business Services ─────────────────────────────────────────────
        // All services registered as Scoped — a new instance per function invocation
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBlobProcessingService, BlobProcessingService>();
        services.AddScoped<IEmployeeReportService, EmployeeReportService>();

        // ── Logging ───────────────────────────────────────────────────────
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddConsole();
        });

        // ── HTTP Client (for calling Employee API if needed) ──────────────
        services.AddHttpClient("EmployeeApi", client =>
        {
            var baseUrl = config["EmployeeApi:BaseUrl"] ?? "http://localhost:5000";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var apiKey = config["EmployeeApi:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            client.Timeout = TimeSpan.FromSeconds(30);
        });
    })

    // ── Step 4: Configure logging ─────────────────────────────────────────────
    .ConfigureLogging((context, logging) =>
    {
        logging.SetMinimumLevel(LogLevel.Information);

        // Reduce noise from Azure internals
        logging.AddFilter("Azure", LogLevel.Warning);
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);

        // Detailed logging for our own functions
        logging.AddFilter("EmployeeManagement.AzureFunctions", LogLevel.Debug);
    })

    .Build();

await host.RunAsync();
