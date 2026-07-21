using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace Logging
{
    /// <summary>
    /// Centralised Serilog configuration.
    /// Call UseSerilogConfiguration(serviceName) in every Program.cs.
    ///
    /// Output sinks:
    ///   Console  — structured JSON in Production, friendly text in Development
    ///   File     — rolling daily file under logs/{ServiceName}-.log
    ///
    /// Every log entry is enriched with:
    ///   ServiceName | MachineName | ThreadId | ProcessId | ExceptionDetail
    /// </summary>
    public static class SerilogConfiguration
    {
        public static Action<HostBuilderContext, LoggerConfiguration> Configure(string serviceName) =>
            (ctx, loggerConfig) =>
            {
                var env = ctx.HostingEnvironment;
                var cfg = ctx.Configuration;

                loggerConfig
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft",              LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("System",                 LogEventLevel.Warning)
                    .MinimumLevel.Override("RabbitMQ.Client",        LogEventLevel.Warning)
                    // ── Enrichers ─────────────────────────────────────────────
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ServiceName", serviceName)
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.WithProcessId()
                    .Enrich.WithExceptionDetails()
                    // ── Console sink ──────────────────────────────────────────
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                    // ── Rolling file sink ──────────────────────────────────────
                    .WriteTo.File(
                        path:              $"logs/{serviceName}-.log",
                        rollingInterval:   RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate:    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ServiceName}] [{RequestId}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
                        shared:            true);
            };
    }
}
