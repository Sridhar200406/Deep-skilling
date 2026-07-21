using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;

namespace HealthChecks
{
    /// <summary>
    /// Extension methods to register and map health check endpoints.
    ///
    /// Endpoints exposed:
    ///   /health        — full report (all checks)
    ///   /health/live   — liveness  (is the process alive?)
    ///   /health/ready  — readiness (are all dependencies ready?)
    /// </summary>
    public static class HealthCheckConfiguration
    {
        /// <summary>
        /// Add SQL Server, Redis, and RabbitMQ health checks.
        /// Call from Program.cs: builder.Services.AddAppHealthChecks(builder.Configuration)
        /// </summary>
        public static IHealthChecksBuilder AddAppHealthChecks(
            this IServiceCollection services,
            string sqlConnectionString,
            string redisConnectionString,
            string rabbitMqHost,
            int    rabbitMqPort = 5672,
            string rabbitMqUser = "guest",
            string rabbitMqPass = "guest")
        {
            return services
                .AddHealthChecks()
                // ── SQL Server ────────────────────────────────────────────────
                .AddSqlServer(
                    connectionString: sqlConnectionString,
                    name:             "sqlserver",
                    tags:             new[] { "db", "ready" },
                    failureStatus:    HealthStatus.Unhealthy)

                // ── Redis ─────────────────────────────────────────────────────
                .AddRedis(
                    redisConnectionString: redisConnectionString,
                    name:                  "redis",
                    tags:                  new[] { "cache", "ready" },
                    failureStatus:         HealthStatus.Degraded)   // degraded, not down — app still works

                // ── RabbitMQ ──────────────────────────────────────────────────
                .AddRabbitMQ(
                    rabbitMQConnectionString: $"amqp://{rabbitMqUser}:{rabbitMqPass}@{rabbitMqHost}:{rabbitMqPort}/",
                    name:                     "rabbitmq",
                    tags:                     new[] { "messaging", "ready" },
                    failureStatus:            HealthStatus.Degraded);
        }

        /// <summary>
        /// Map all three health check endpoints.
        /// Call from Program.cs: app.MapAppHealthChecks()
        /// </summary>
        public static IApplicationBuilder MapAppHealthChecks(this WebApplication app)
        {
            // /health — all checks, full JSON report
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteJsonResponse,
                AllowCachingResponses = false
            });

            // /health/live — liveness: is the process alive? (no dependency checks)
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate             = _ => false,   // skip all checks — just returns 200
                ResponseWriter        = WriteJsonResponse,
                AllowCachingResponses = false
            });

            // /health/ready — readiness: are dependencies ready?
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate             = hc => hc.Tags.Contains("ready"),
                ResponseWriter        = WriteJsonResponse,
                AllowCachingResponses = false
            });

            return app;
        }

        // ── JSON response writer ──────────────────────────────────────────────
        private static async Task WriteJsonResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status    = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                duration  = report.TotalDuration.TotalMilliseconds + "ms",
                checks    = report.Entries.Select(e => new
                {
                    name        = e.Key,
                    status      = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration    = e.Value.Duration.TotalMilliseconds + "ms",
                    tags        = e.Value.Tags,
                    error       = e.Value.Exception?.Message
                })
            };

            var statusCode = report.Status == HealthStatus.Healthy ? 200
                           : report.Status == HealthStatus.Degraded ? 200
                           : 503;

            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented        = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }
    }
}
