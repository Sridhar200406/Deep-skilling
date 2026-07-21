using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Monitoring
{
    /// <summary>
    /// Application performance metrics using OpenTelemetry Meters.
    ///
    /// Tracks per-service:
    ///   http.requests.total          — counter of all requests
    ///   http.requests.success        — counter of 2xx responses
    ///   http.requests.failed         — counter of 4xx/5xx responses
    ///   http.request.duration        — histogram of response times (ms)
    ///   cache.hits.total             — Redis cache hit counter
    ///   cache.misses.total           — Redis cache miss counter
    ///   rabbitmq.messages.published  — messages sent to RabbitMQ
    ///   rabbitmq.messages.consumed   — messages received from RabbitMQ
    ///
    /// Usage:
    ///   1. Call builder.Services.AddAppMetrics(serviceName) in Program.cs
    ///   2. Inject AppMetrics where needed
    /// </summary>
    public static class MetricsConfiguration
    {
        public static IServiceCollection AddAppMetrics(
            this IServiceCollection services,
            string serviceName,
            string serviceVersion = "1.0.0")
        {
            // Register metrics as singleton — shared across the whole process
            services.AddSingleton(new AppMetrics(serviceName));

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService(serviceName, serviceVersion))
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter(serviceName)
                        .AddConsoleExporter();
                });

            return services;
        }
    }

    /// <summary>
    /// Strongly-typed metric instruments.
    /// Inject this class where you want to record metrics.
    /// </summary>
    public sealed class AppMetrics
    {
        private readonly Meter _meter;

        // ── HTTP counters ─────────────────────────────────────────────────────
        public readonly Counter<long> RequestsTotal;
        public readonly Counter<long> RequestsSuccess;
        public readonly Counter<long> RequestsFailed;
        public readonly Histogram<double> RequestDurationMs;

        // ── Cache counters ───────────────────────────────────────────────────
        public readonly Counter<long> CacheHits;
        public readonly Counter<long> CacheMisses;

        // ── RabbitMQ counters ────────────────────────────────────────────────
        public readonly Counter<long> MessagesPublished;
        public readonly Counter<long> MessagesConsumed;

        public AppMetrics(string serviceName)
        {
            _meter = new Meter(serviceName, "1.0.0");

            RequestsTotal     = _meter.CreateCounter<long>("http.requests.total",     description: "Total HTTP requests");
            RequestsSuccess   = _meter.CreateCounter<long>("http.requests.success",   description: "Successful HTTP requests (2xx)");
            RequestsFailed    = _meter.CreateCounter<long>("http.requests.failed",    description: "Failed HTTP requests (4xx/5xx)");
            RequestDurationMs = _meter.CreateHistogram<double>("http.request.duration", unit: "ms", description: "HTTP request duration");

            CacheHits         = _meter.CreateCounter<long>("cache.hits.total",        description: "Redis cache hits");
            CacheMisses       = _meter.CreateCounter<long>("cache.misses.total",      description: "Redis cache misses");

            MessagesPublished = _meter.CreateCounter<long>("rabbitmq.messages.published", description: "RabbitMQ messages published");
            MessagesConsumed  = _meter.CreateCounter<long>("rabbitmq.messages.consumed",  description: "RabbitMQ messages consumed");
        }
    }
}
