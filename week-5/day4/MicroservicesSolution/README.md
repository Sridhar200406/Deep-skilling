# Week 5 – Day 4: Distributed Caching, Health Checks & Resilience

## What's New in Day 4

| Feature | Implementation |
|---|---|
| Redis Distributed Cache | `Caching/RedisCacheService.cs` — generic get/set/remove with logging |
| Cache Keys | `Caching/CacheKeys.cs` — centralised key factory |
| Health Checks | `HealthChecks/HealthCheckConfiguration.cs` — SQL + Redis + RabbitMQ |
| Polly Resilience | `Resilience/PollyConfiguration.cs` — Retry + CircuitBreaker + Timeout + Fallback |
| Request Logging | `EmployeeService/Logging/RequestLoggingMiddleware.cs` |
| IHttpClientFactory | DepartmentServiceClient now backed by Polly via AddHttpClient |
| Docker | Updated `docker-compose.yml` adds Redis with healthcheck |

---

## Project Structure

```
MicroservicesSolution/
├── ApiGateway/                         ← Ocelot gateway (port 5000)
├── AuthenticationService/              ← JWT issuer (port 5001)
├── EmployeeService/                    ← CRUD + Events + Cache + Polly (port 5002)
│   ├── Logging/RequestLoggingMiddleware.cs
│   └── Services/EmployeeBusinessService.cs  ← Redis cache + invalidation
├── DepartmentService/                  ← CRUD + Events + Cache (port 5003)
│   └── Controllers/DepartmentsController.cs ← Redis cache + invalidation
├── Shared/Events/                      ← Domain event contracts
├── Messaging/Producer|Consumer/        ← RabbitMQ
├── Caching/                            ← NEW: IRedisCacheService, RedisCacheService, CacheKeys
├── HealthChecks/                       ← NEW: HealthCheckConfiguration (SQL+Redis+RabbitMQ)
├── Resilience/                         ← NEW: PollyConfiguration (Retry+CB+Timeout+Fallback)
├── docker-compose.yml                  ← Updated: adds Redis service
└── README.md
```

---

## Redis Caching Strategy

| Operation | Cache Behaviour |
|---|---|
| GET employee by ID | Check Redis → hit: return; miss: load DB, populate Redis (10 min TTL) |
| GET employee list | Check Redis → hit: return; miss: load DB + enrich, populate Redis |
| POST create | Write DB → publish event → **invalidate** list cache |
| PUT update | Write DB → publish event → **invalidate** item + list cache |
| DELETE | Write DB → publish event → **invalidate** item + list cache |
| GET department by ID | Check Redis → hit: return; miss: load DB, populate Redis (30 min TTL) |
| GET department list | Check Redis → hit: return; miss: load DB, populate Redis |

### Cache key format
```
employee:{id}                    — single employee
employees:list:{query-hash}      — paginated/filtered lists
department:{id}                  — single department
departments:list:all             — all departments
```

---

## Polly Resilience Policies

Applied to all `DepartmentServiceClient` outbound calls:

```
Outer  → Fallback       returns 503 JSON when circuit is open or all retries fail
       → CircuitBreaker opens after 50% failure rate over 30s (5 req min); breaks 15s
       → Retry          3 attempts with exponential back-off: 2s, 4s, 8s
Inner  → Timeout        10 seconds per individual attempt
```

### Logging events
- Each retry attempt → `LogWarning` with attempt number, delay, reason
- Circuit opens  → `LogError`
- Circuit resets → `LogInformation`
- Fallback fires  → `LogError`

---

## Health Check Endpoints

Each service exposes three endpoints:

| Endpoint | Purpose | HTTP Status |
|---|---|---|
| `/health`       | Full report — all checks with details | 200/503 |
| `/health/live`  | Liveness — is the process alive? | 200 always |
| `/health/ready` | Readiness — are dependencies up? | 200/503 |

### Sample `/health` response
```json
{
  "status": "Healthy",
  "timestamp": "2024-07-20T10:00:00Z",
  "duration": "45ms",
  "checks": [
    { "name": "sqlserver", "status": "Healthy", "duration": "12ms", "tags": ["db","ready"] },
    { "name": "redis",     "status": "Healthy", "duration": "5ms",  "tags": ["cache","ready"] },
    { "name": "rabbitmq",  "status": "Healthy", "duration": "8ms",  "tags": ["messaging","ready"] }
  ]
}
```

---

## Setup Instructions

### Step 1 — Start infrastructure via Docker
```bash
cd MicroservicesSolution
docker-compose up -d
```
Starts: RabbitMQ (:5672 + :15672), Redis (:6379), SQL Server (:1433)

### Step 2 — Run all services (4 terminals)
```bash
# Terminal 1
cd AuthenticationService && dotnet run

# Terminal 2
cd DepartmentService && dotnet run

# Terminal 3
cd EmployeeService && dotnet run

# Terminal 4
cd ApiGateway && dotnet run
```

---

## Testing

### Test Redis Caching
```bash
# First call — MISS (loads from DB)
curl http://localhost:5002/api/departments/health

# GET employee (MISS first time, HIT second time)
GET http://localhost:5000/gateway/employees/1
Authorization: Bearer <token>

# Check logs for "Cache HIT" / "Cache MISS"
```

### Test Health Checks
```bash
# Full health report
GET http://localhost:5002/health

# Liveness (always 200)
GET http://localhost:5002/health/live

# Readiness (200 if SQL+Redis+RabbitMQ all up)
GET http://localhost:5002/health/ready

# Same endpoints on DepartmentService
GET http://localhost:5003/health
GET http://localhost:5003/health/live
GET http://localhost:5003/health/ready
```

### Test Polly Resilience
```bash
# Stop DepartmentService to trigger retries and circuit breaker
# Then call EmployeeService endpoint that needs DepartmentName:
GET http://localhost:5000/gateway/employees
Authorization: Bearer <token>

# Watch logs for:
# "Polly Retry: attempt 1 after 2s..."
# "Polly Retry: attempt 2 after 4s..."
# "Polly CircuitBreaker: OPEN for 15s..."
# "Polly Fallback: returning 503..."
```

### Test Redis CLI
```bash
# Connect to Redis container
docker exec -it redis redis-cli

# List all keys
KEYS *

# Get a specific employee
GET "EmployeeService:employee:1"

# Check TTL
TTL "EmployeeService:employee:1"
```

---

## Connection Strings for Docker

Update `appsettings.json` to use Docker SQL Server:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=<Db>;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;",
  "Redis": "localhost:6379"
}
```
