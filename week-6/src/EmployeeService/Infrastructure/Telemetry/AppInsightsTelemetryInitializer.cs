using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace EmployeeService.Infrastructure.Telemetry;

/// <summary>
/// Enriches every Application Insights telemetry item with:
///   - Service name
///   - Environment name
///   - API version
///
/// This makes it easy to filter in the Azure Portal by service or environment.
/// </summary>
public class AppInsightsTelemetryInitializer : ITelemetryInitializer
{
    private readonly string _environment;
    private readonly string _serviceName;

    public AppInsightsTelemetryInitializer(IWebHostEnvironment env)
    {
        _environment = env.EnvironmentName;
        _serviceName = "EmployeeService";
    }

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = _serviceName;
        telemetry.Context.GlobalProperties["ServiceName"] = _serviceName;
        telemetry.Context.GlobalProperties["Environment"] = _environment;
        telemetry.Context.GlobalProperties["ApiVersion"] = "v1";
        telemetry.Context.GlobalProperties["MachineName"] = Environment.MachineName;
    }
}
