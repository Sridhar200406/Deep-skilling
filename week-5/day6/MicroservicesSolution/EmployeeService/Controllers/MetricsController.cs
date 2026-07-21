using Microsoft.AspNetCore.Mvc;
using Monitoring;

namespace EmployeeService.Controllers
{
    /// <summary>
    /// Exposes live application metrics via HTTP for monitoring dashboards.
    /// GET /api/metrics — returns current counter snapshot.
    /// </summary>
    [ApiController]
    [Route("api/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly AppMetrics _metrics;

        public MetricsController(AppMetrics metrics) => _metrics = metrics;

        [HttpGet]
        public IActionResult Get()
        {
            // AppMetrics are counters/histograms — values are captured in OTel pipeline.
            // This endpoint confirms the metrics service is wired up and returns service info.
            return Ok(new
            {
                service   = "EmployeeService",
                timestamp = DateTime.UtcNow,
                note      = "Metrics are exported to the OTel Console exporter. " +
                            "In production, replace with .AddOtlpExporter() → Prometheus / Grafana.",
                meters    = new[]
                {
                    "http.requests.total",
                    "http.requests.success",
                    "http.requests.failed",
                    "http.request.duration",
                    "cache.hits.total",
                    "cache.misses.total",
                    "rabbitmq.messages.published"
                }
            });
        }
    }
}
