# Week 5 – Day 5: Observability, Centralized Logging & Monitoring

## What's New in Day 5

| Feature | Library | Files |
|---|---|---|
| Structured Logging | Serilog | `Logging/SerilogConfiguration.cs` |
| Request/Response Logging | Serilog | `Logging/LoggingMiddleware.cs` |
| Distributed Tracing | OpenTelemetry | `Monitoring/OpenTelemetryConfiguration.cs` |
| Performance Metrics | OpenTelemetry Meters | `Monitoring/MetricsConfiguration.cs` |
| Metrics HTTP endpoint | — | `EmployeeService/Controllers/MetricsController.cs` |
| Docker (all services) | Docker Compose | `docker-compose.yml` + `Docker/` Dockerfiles |
| Updated solution | — | `MicroservicesSolution.sln` (12 projects) |

---

## Project Structure

```
MicroservicesSolution/
├── ApiGateway/               ← Ocelot (port 5000) + Serilog + OTel
├── AuthenticationService/    ← JWT (port 5001) + Serilog + OTel
├── EmployeeService/          ← (port 5002) + Serilog + OTel + Metrics endpoint
├── DepartmentService/        ← (port 5003) + Serilog + OTel
├── Shared/                   ← Domain event contracts
├── Messaging/                ← RabbitMQ Producer + Consumer
├── Caching/                  ← Redis cache service
├── HealthChecks/             ← SQL + Redis + RabbitMQ health checks
├── Resilience/               ← Polly policies
├── Logging/                  ← NEW: SerilogConfiguration, LoggingMiddleware
├── Monitoring/               ← NEW: OpenTelemetryConfiguration, MetricsConfiguration
├── Docker/                   ← NEW: Dockerfiles for all 4 services
├── docker-compose.yml        ← Updated: all 7 services + infrastructure
└── README.md
```

---

## Structured Logging with Serilog

Every log line contains:
```
[HH:mm:ss INF] [ServiceName] {Message} {Properties}
```

### Log file location
```
logs/EmployeeService-20240720.log
logs/DepartmentService-20240720.log
logs/AuthenticationService-20240720.log
logs/ApiGateway-20240720.log
```
Rolling daily, 7 files retained.

### Sample log output
```
[10:23:45 INF] [EmployeeService] → GET /api/employees CorrelationId=a3f2b1 RequestId=...
[10:23:45 INF] [EmployeeService] Cache MISS employees list key=p1s10q...
[10:23:45 INF] [EmployeeService] ← 200 GET /api/employees 45ms CorrelationId=a3f2b1
```

---

## Distributed Tracing with OpenTelemetry

Traces are exported to the **Console** (visible in each service terminal).

### What's instrumented
| Layer | Instrumentation |
|---|---|
| Incoming HTTP | ASP.NET Core — every request creates a span |
| Outgoing HTTP | HttpClient — Polly calls to DepartmentService |
| Database | SqlClient — every EF Core query |

### Sample console trace output
```
Activity.TraceId:    abc123def456...
Activity.SpanId:     1234abcd
Activity.DisplayName: GET /api/employees
Activity.Duration:   00:00:00.0450000
Activity.Tags:
    http.method: GET
    http.url: http://localhost:5002/api/employees
    http.status_code: 200
    service.name: EmployeeService
```

### Correlation IDs
Every request receives a `X-Correlation-Id` header (generated if not present).
This ID flows through:
1. Client → ApiGateway
2. ApiGateway → EmployeeService (forwarded in header)
3. EmployeeService → DepartmentService (forwarded in header)
4. All log lines include the same CorrelationId for end-to-end tracing

---

## Performance Metrics

Metrics are exported via OTel Console exporter. Available meters:

| Metric | Type | Description |
|---|---|---|
| `http.requests.total` | Counter | All HTTP requests |
| `http.requests.success` | Counter | 2xx responses |
| `http.requests.failed` | Counter | 4xx/5xx responses |
| `http.request.duration` | Histogram | Response time in ms |
| `cache.hits.total` | Counter | Redis cache hits |
| `cache.misses.total` | Counter | Redis cache misses |
| `rabbitmq.messages.published` | Counter | Messages sent |
| `rabbitmq.messages.consumed` | Counter | Messages received |

### Metrics endpoint
```
GET http://localhost:5002/api/metrics
```

---

## Setup Instructions

### Option A — Local development

**Step 1: Start infrastructure**
```bash
docker-compose up -d rabbitmq redis sqlserver
```

**Step 2: Run all 4 services (separate terminals)**
```bash
cd AuthenticationService  && dotnet run
cd DepartmentService      && dotnet run
cd EmployeeService        && dotnet run
cd ApiGateway             && dotnet run
```

### Option B — Full Docker (all services)

```bash
docker-compose up --build
```
All 7 containers start together: RabbitMQ, Redis, SQL Server, Auth, Department, Employee, Gateway.

---

## Testing

### Test Serilog structured logging
```bash
# Make a request and watch the console output
GET http://localhost:5000/gateway/employees
Authorization: Bearer <token>

# Check the log files
type logs\EmployeeService-*.log
```

### Test Distributed Tracing
```bash
# Make a request — OTel trace appears in console output
GET http://localhost:5002/api/employees/1
Authorization: Bearer <token>
# Look for "Activity.TraceId" in terminal output
```

### Test Health Checks
```bash
GET http://localhost:5002/health          # full report
GET http://localhost:5002/health/live     # liveness
GET http://localhost:5002/health/ready    # readiness
GET http://localhost:5003/health          # department service
```

### Test Metrics endpoint
```bash
GET http://localhost:5002/api/metrics
```

### Verify CorrelationId flows end-to-end
```bash
curl -H "X-Correlation-Id: test-trace-123" http://localhost:5000/gateway/employees -H "Authorization: Bearer <token>"
# Look for "test-trace-123" appearing in logs of BOTH EmployeeService and DepartmentService
```

---

## Concepts Explained

### Structured Logging
Unlike plain text logs, structured logging stores log entries as key-value pairs.
Serilog serialises log context as JSON so tools can filter/search on any field.

### Centralized Logging
All services use the same `SerilogConfiguration` so every log line has the same shape:
`Timestamp | ServiceName | Level | RequestId | CorrelationId | Message`

### Distributed Tracing
A single user request may touch 3 services. OpenTelemetry creates a parent `TraceId`
and a child `SpanId` per service hop — you can reconstruct the full call chain.

### Correlation IDs
A UUID generated at the entry point (Gateway or service) and forwarded in
`X-Correlation-Id` header. Every service includes it in every log line,
so you can `grep` for one ID and see the entire request story across all services.

### Performance Metrics
Counters and histograms that track request throughput, cache efficiency, and messaging
volume. In production, export to Prometheus + Grafana for dashboards and alerting.
