using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Monitoring
{
    /// <summary>
    /// OpenTelemetry distributed tracing configuration.
    ///
    /// Instruments:
    ///   - Incoming HTTP requests  (ASP.NET Core)
    ///   - Outgoing HTTP calls     (HttpClient / Polly)
    ///   - SQL Server queries      (SqlClient)
    ///
    /// Exporter: Console (visible in all terminals for dev / learning purposes).
    ///
    /// In production, swap AddConsoleExporter() for:
    ///   .AddOtlpExporter()  → sends to Jaeger / Zipkin / any OTEL collector
    /// </summary>
    public static class OpenTelemetryConfiguration
    {
        /// <summary>
        /// Register OpenTelemetry tracing in DI.
        /// Call builder.Services.AddAppTracing(serviceName) from each Program.cs.
        /// </summary>
        public static IServiceCollection AddAppTracing(
            this IServiceCollection services,
            string serviceName,
            string serviceVersion = "1.0.0")
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource =>
                    resource.AddService(
                        serviceName:    serviceName,
                        serviceVersion: serviceVersion))
                .WithTracing(tracing =>
                {
                    tracing
                        // ── Instrumentation ───────────────────────────────────
                        .AddAspNetCoreInstrumentation(opt =>
                        {
                            opt.RecordException = true;
                            // Exclude health check endpoints from traces
                            opt.Filter = ctx =>
                                !ctx.Request.Path.StartsWithSegments("/health");
                        })
                        .AddHttpClientInstrumentation(opt =>
                        {
                            opt.RecordException = true;
                        })
                        .AddSqlClientInstrumentation(opt =>
                        {
                            opt.SetDbStatementForText       = true;
                            opt.RecordException              = true;
                            opt.EnableConnectionLevelAttributes = true;
                        })
                        // ── Exporter ──────────────────────────────────────────
                        // Console exporter — shows traces in terminal.
                        // Replace with .AddOtlpExporter() for Jaeger/Zipkin.
                        .AddConsoleExporter();
                });

            return services;
        }
    }
}
