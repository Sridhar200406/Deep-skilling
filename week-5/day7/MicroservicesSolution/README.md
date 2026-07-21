# Week 5 – Day 7: Final Microservices Integration & Capstone

## Architecture Overview

```
                        ┌─────────────────────────────┐
                        │           CLIENT             │
                        │  (Browser / Postman / App)   │
                        └──────────────┬──────────────┘
                                       │ HTTP
                                       ▼
                        ┌─────────────────────────────┐
                        │         API GATEWAY          │
                        │   Ocelot  •  JWT Validation  │
                        │   Port 5000  •  Rate Limit   │
                        └──────┬──────────┬────────────┘
                               │          │          │
                    ┌──────────▼──┐  ┌────▼──────┐  ┌▼─────────────┐
                    │  Employee   │  │   Auth    │  │  Department  │
                    │  Service   │  │  Service  │  │   Service    │
                    │  Port 5002  │  │  Port5001 │  │  Port 5003   │
                    └──────┬──────┘  └─────┬─────┘  └──────┬───────┘
                           │               │                │
                    ┌──────▼──┐     ┌──────▼──┐     ┌──────▼──┐
                    │SQL Serv.│     │SQL Serv.│     │SQL Serv.│
                    │EmployDb │     │ AuthDb  │     │ DeptDb  │
                    └──────┬──┘     └─────────┘     └─────────┘
                           │
                    ┌──────▼──────────────────────┐
                    │         RabbitMQ             │
                    │  employee.events exchange    │
                    │  employee.created/.updated/  │
                    │  .deleted  routing keys      │
                    └──────┬──────────────────────┘
                           │ consumes
                    ┌──────▼──┐
                    │  Dept.  │
                    │ Service │  (updates EmployeeCount)
                    └─────────┘

         Employee Service  ──────► Redis Cache (port 6379)
         Department Service ─────► Redis Cache

         All Services ─────────► Serilog (Console + Rolling Files)
         All Services ─────────► OpenTelemetry (Console Exporter)
         All Services ─────────► /health  /health/live  /health/ready
```

---

## Technologies Used

| Layer | Technology |
|---|---|
| API Gateway | Ocelot 23.3.3 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Database | SQL Server 2022 + Entity Framework Core 8 |
| Messaging | RabbitMQ 3.13 + RabbitMQ.Client 6.8.1 |
| Caching | Redis 7 + StackExchange.Redis |
| Resilience | Polly 7.2.4 (Retry + CircuitBreaker + Timeout + Fallback) |
| Logging | Serilog 3.1.1 (Console + File sinks) |
| Tracing | OpenTelemetry 1.8.1 |
| Mapping | AutoMapper 13 |
| Containers | Docker + Docker Compose |
| Orchestration | Kubernetes (Minikube / Docker Desktop) |
| Framework | .NET 8 / ASP.NET Core 8 |

---

## Project Structure

```
MicroservicesSolution/
├── ApiGateway/                   Port 5000  — Ocelot routing + JWT validation
│   ├── Middleware/GatewayExceptionMiddleware.cs
│   ├── ocelot.json               Route table (container names in Docker)
│   ├── appsettings.json
│   └── Program.cs
│
├── AuthenticationService/        Port 5001  — Registration + Login + JWT issuance
│   ├── Controllers/AuthController.cs
│   ├── Data/AuthDbContext.cs
│   ├── DTOs/  RegisterDto  LoginDto  AuthResponseDto
│   ├── Models/AppUser.cs
│   ├── Services/TokenService.cs
│   └── Program.cs
│
├── EmployeeService/              Port 5002  — Employee CRUD + Events + Cache
│   ├── Controllers/
│   │   ├── EmployeesController.cs
│   │   └── MetricsController.cs
│   ├── Data/EmployeeDbContext.cs
│   ├── DTOs/
│   ├── HttpClients/DepartmentServiceClient.cs  (Typed + Polly)
│   ├── Interfaces/
│   ├── Mapping/EmployeeProfile.cs
│   ├── Messaging/EmployeeEventPublisher.cs
│   ├── Middleware/ExceptionMiddleware.cs
│   ├── Models/Employee.cs
│   ├── Repositories/EmployeeRepository.cs
│   ├── Responses/ApiResponse.cs
│   ├── Services/EmployeeBusinessService.cs     (Redis cache + event publish)
│   └── Program.cs
│
├── DepartmentService/            Port 5003  — Department CRUD + Event Consumer + Cache
│   ├── Controllers/DepartmentsController.cs
│   ├── Data/DepartmentDbContext.cs
│   ├── DTOs/
│   ├── EventHandlers/
│   │   ├── EmployeeCreatedEventHandler.cs
│   │   ├── EmployeeUpdatedEventHandler.cs
│   │   └── EmployeeDeletedEventHandler.cs
│   ├── Interfaces/IDepartmentRepository.cs
│   ├── Mapping/DepartmentProfile.cs
│   ├── Models/Department.cs
│   ├── Repositories/DepartmentRepository.cs
│   ├── Services/DepartmentService.cs
│   └── Program.cs
│
├── Shared/Events/                Domain events (shared between Producer/Consumer)
│   ├── BaseEvent.cs
│   ├── EmployeeCreatedEvent.cs
│   ├── EmployeeUpdatedEvent.cs
│   └── EmployeeDeletedEvent.cs
│
├── Messaging/
│   ├── Producer/RabbitMQProducer.cs    Topic exchange + 3-retry publish
│   └── Consumer/RabbitMQConsumerService.cs  BackgroundService + DLQ
│
├── Caching/                      Redis cache service (generic get/set/remove)
│   ├── IRedisCacheService.cs
│   ├── RedisCacheService.cs
│   └── CacheKeys.cs
│
├── HealthChecks/                 SQL + Redis + RabbitMQ checks + 3 endpoints
│   └── HealthCheckConfiguration.cs
│
├── Resilience/                   Polly: Retry + CB + Timeout + Fallback
│   └── PollyConfiguration.cs
│
├── Logging/                      Serilog: Console + rolling file + enrichers
│   ├── SerilogConfiguration.cs
│   └── LoggingMiddleware.cs
│
├── Monitoring/                   OpenTelemetry tracing + metrics
│   ├── OpenTelemetryConfiguration.cs
│   └── MetricsConfiguration.cs
│
├── Docker/
│   ├── Dockerfile.ApiGateway
│   ├── Dockerfile.AuthenticationService
│   ├── Dockerfile.DepartmentService
│   └── Dockerfile.EmployeeService
│
├── k8s/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── secrets.yaml
│   ├── sqlserver-deployment.yaml + service.yaml
│   ├── rabbitmq-deployment.yaml  + service.yaml
│   ├── redis-deployment.yaml     + service.yaml
│   ├── authentication-deployment.yaml + service.yaml
│   ├── department-deployment.yaml     + service.yaml
│   ├── employee-deployment.yaml       + service.yaml
│   └── api-gateway-deployment.yaml    + service.yaml
│
├── docker-compose.yml            All 7 services + networking
└── README.md                     This file
```

---

## Setup Instructions

### Prerequisites
- .NET 8 SDK
- Docker Desktop (with Kubernetes enabled for K8s)
- Git

### Option A — Local development (no Docker)

```bash
# 1. Start infrastructure only
docker-compose up -d sqlserver rabbitmq redis

# 2. Run each service in a separate terminal
cd AuthenticationService  && dotnet run   # http://localhost:5001
cd DepartmentService      && dotnet run   # http://localhost:5003
cd EmployeeService        && dotnet run   # http://localhost:5002
cd ApiGateway             && dotnet run   # http://localhost:5000
```

### Option B — Full Docker Compose

```bash
# Build and start all 7 containers
docker-compose up --build

# Background mode
docker-compose up --build -d

# Stop
docker-compose down

# Stop + wipe volumes
docker-compose down -v
```

### Option C — Kubernetes

```bash
# Build images
docker build -t employee-mgmt/authentication-service:latest -f Docker/Dockerfile.AuthenticationService .
docker build -t employee-mgmt/department-service:latest     -f Docker/Dockerfile.DepartmentService .
docker build -t employee-mgmt/employee-service:latest       -f Docker/Dockerfile.EmployeeService .
docker build -t employee-mgmt/api-gateway:latest            -f Docker/Dockerfile.ApiGateway .

# Deploy
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/
kubectl get pods -n employee-mgmt
```

---

## API Documentation

### Base URLs
| Mode | URL |
|---|---|
| Local | `http://localhost:5000` |
| Docker | `http://localhost:5000` |
| Kubernetes | `http://localhost:30000` |

### Authentication Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/gateway/auth/register` | None | Register new user |
| POST | `/gateway/auth/login` | None | Login, receive JWT |
| GET | `/gateway/auth/health` | None | Auth service health |

#### Register
```json
POST /gateway/auth/register
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "password": "Password1"
}
```

#### Login
```json
POST /gateway/auth/login
{
  "email": "john@example.com",
  "password": "Password1"
}
// Response:
{
  "token": "eyJhbGci...",
  "email": "john@example.com",
  "fullName": "John Doe",
  "expires": "2024-07-21T11:00:00Z"
}
```

### Employee Endpoints (JWT required)

| Method | Route | Description |
|---|---|---|
| GET | `/gateway/employees` | List (paginated, filterable) |
| GET | `/gateway/employees/{id}` | Get by ID (with DepartmentName) |
| POST | `/gateway/employees` | Create + publish RabbitMQ event |
| PUT | `/gateway/employees/{id}` | Update + publish RabbitMQ event |
| DELETE | `/gateway/employees/{id}` | Delete + publish RabbitMQ event |
| GET | `/api/employees/health` | Health check |

#### GET query parameters
```
?pageNumber=1&pageSize=10&searchTerm=alice&departmentId=1&sortBy=salary&isAscending=false
```

#### Create Employee
```json
POST /gateway/employees
Authorization: Bearer <token>
{
  "firstName": "Alice",
  "lastName": "Smith",
  "email": "alice@company.com",
  "phone": "555-1234",
  "position": "Developer",
  "salary": 85000,
  "hireDate": "2024-01-15T00:00:00",
  "departmentId": 1,
  "isActive": true
}
```

### Department Endpoints (JWT required)

| Method | Route | Description |
|---|---|---|
| GET | `/gateway/departments` | List all departments |
| GET | `/gateway/departments/{id}` | Get by ID |
| POST | `/gateway/departments` | Create department |
| PUT | `/gateway/departments/{id}` | Update department |
| DELETE | `/gateway/departments/{id}` | Delete department |

---

## RabbitMQ Events

### Exchange topology
```
Exchange:    employee.events  (topic, durable)
Queue:       department.employee.q  (durable, bound to employee.*)
DLQ:         employee.events.dlq   (dead letter queue for failures)
```

### Events published
| Event | Routing Key | Trigger |
|---|---|---|
| EmployeeCreatedEvent | `employee.created` | POST /employees |
| EmployeeUpdatedEvent | `employee.updated` | PUT /employees/{id} |
| EmployeeDeletedEvent | `employee.deleted` | DELETE /employees/{id} |

### What DepartmentService does on consume
| Event | Action |
|---|---|
| EmployeeCreated | `department.EmployeeCount++` |
| EmployeeUpdated | If dept changed: old dept `--`, new dept `++` |
| EmployeeDeleted | `department.EmployeeCount--` |

### Management UI
```
http://localhost:15672   (guest / guest)
Queues tab → department.employee.q  → verify message rates
```

---

## Redis Caching

### Cache keys
```
EmployeeService:employee:{id}           — single employee (10 min TTL)
EmployeeService:employees:list:{hash}   — paginated list   (10 min TTL)
DepartmentService:department:{id}       — single dept      (30 min TTL)
DepartmentService:departments:list:all  — all depts        (30 min TTL)
```

### Verify via Redis CLI
```bash
docker exec -it redis redis-cli
KEYS *
GET "EmployeeService:employee:1"
TTL "EmployeeService:employee:1"
```

### Cache invalidation
- POST/PUT/DELETE → removes relevant keys immediately after DB write

---

## Health Check Endpoints

```
GET /health         — full report (SQL + Redis + RabbitMQ)
GET /health/live    — liveness (always 200 if process is up)
GET /health/ready   — readiness (200 only if all deps healthy)
```

### Sample response
```json
{
  "status": "Healthy",
  "timestamp": "2024-07-21T10:00:00Z",
  "duration": "45ms",
  "checks": [
    { "name": "sqlserver", "status": "Healthy", "duration": "12ms" },
    { "name": "redis",     "status": "Healthy", "duration": "5ms"  },
    { "name": "rabbitmq",  "status": "Healthy", "duration": "8ms"  }
  ]
}
```

---

## Polly Resilience

Applied to all `DepartmentServiceClient` outbound calls from EmployeeService:

```
Fallback (503 JSON)        — fires when circuit open or all retries fail
  → CircuitBreaker         — opens after 50% failures in 30s window (min 5 req); breaks 15s
    → Retry (3 attempts)   — exponential backoff: 2s, 4s, 8s
      → Timeout (10s)      — per-attempt timeout
```

### Test resilience
```bash
# Stop DepartmentService, then call EmployeeService
docker-compose stop department-service

# Call an employee endpoint — watch logs for retry/CB/fallback messages
GET http://localhost:5000/gateway/employees/1
Authorization: Bearer <token>

# Restart
docker-compose start department-service
```

---

## Logging

### Log format (console)
```
[10:23:45 INF] [EmployeeService] → GET /api/employees CorrelationId=a3f2b1
[10:23:45 INF] [EmployeeService] Cache MISS employees list key=p1s10q...
[10:23:45 INF] [EmployeeService] ← 200 GET /api/employees 45ms
```

### Log files (rolling daily)
```
logs/EmployeeService-20240721.log
logs/DepartmentService-20240721.log
logs/AuthenticationService-20240721.log
logs/ApiGateway-20240721.log
```

### View Docker logs
```bash
docker-compose logs -f employee-service
docker-compose logs -f api-gateway
```

---

## Distributed Tracing

OpenTelemetry exports traces to console. Every request shows:
```
Activity.TraceId:     abc123def456...
Activity.SpanId:      1234abcd
Activity.DisplayName: GET /api/employees
Activity.Duration:    00:00:00.0450000
Tags: http.method=GET, http.status_code=200, service.name=EmployeeService
```

CorrelationId flows end-to-end via `X-Correlation-Id` header:
```bash
curl -H "X-Correlation-Id: my-trace-id" \
     -H "Authorization: Bearer <token>" \
     http://localhost:5000/gateway/employees
# "my-trace-id" appears in ALL service logs for this request
```

---

## Failure Scenarios & Graceful Handling

| Failure | Behaviour |
|---|---|
| SQL Server down | Service returns 503 via health check; EF Core throws on DB call |
| RabbitMQ down | EmployeeService: event publish retries 3×, then throws; CRUD still works |
| Redis down | `RedisCacheService` catches exception, logs warning, falls back to DB |
| DepartmentService down | Polly retries 3×, CB opens, Fallback returns null DepartmentName |
| EmployeeService down | Gateway returns 503 (Ocelot downstream error) |

---

## Docker Commands Reference

```bash
# Start all
docker-compose up --build -d

# Stop all
docker-compose down

# View logs
docker-compose logs -f
docker-compose logs -f employee-service

# Exec into a container
docker exec -it employee-service bash
docker exec -it redis redis-cli

# Scale a service
docker-compose up -d --scale employee-service=2

# Rebuild one service
docker-compose up --build employee-service
```

---

## Kubernetes Commands Reference

```bash
kubectl get pods       -n employee-mgmt
kubectl get services   -n employee-mgmt
kubectl get deployments -n employee-mgmt

kubectl logs -f deployment/employee-service  -n employee-mgmt
kubectl describe pod <name>                  -n employee-mgmt

# Port-forward for local access
kubectl port-forward svc/api-gateway 5000:5000 -n employee-mgmt

# Scale
kubectl scale deployment employee-service --replicas=3 -n employee-mgmt

# Delete all
kubectl delete namespace employee-mgmt
```

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `Connection refused` on startup | Infrastructure containers not ready — wait 30s and retry |
| JWT `401 Unauthorized` | Token expired (60 min TTL) — re-login |
| RabbitMQ `connection reset` | RabbitMQ not ready — services have `AutomaticRecoveryEnabled=true` |
| Redis `ECONNREFUSED` | Redis container not healthy — `docker-compose ps` to check |
| SQL Server timeout | First boot is slow — `docker-compose ps` until healthcheck passes |
| Ocelot `503` | Downstream service not started — check `docker-compose logs <service>` |
| `Certificate error` in Docker | Add `TrustServerCertificate=True` to connection string |

---

## Week 5 Final Checklist

### Day 1 — Foundation
- [x] EmployeeService CRUD with Repository Pattern
- [x] Entity Framework Core + SQL Server
- [x] DTOs + AutoMapper
- [x] Swagger documentation
- [x] Dependency Injection

### Day 2 — API Gateway & Inter-Service Communication
- [x] Ocelot API Gateway with JWT validation
- [x] Route configuration for all 3 services
- [x] Typed HttpClient (DepartmentServiceClient)
- [x] Service-to-service JWT forwarding
- [x] Centralized exception middleware

### Day 3 — Messaging & Event-Driven Architecture
- [x] RabbitMQ topic exchange `employee.events`
- [x] EmployeeCreatedEvent / EmployeeUpdatedEvent / EmployeeDeletedEvent
- [x] RabbitMQProducer with 3-retry publish logic
- [x] RabbitMQConsumerService (BackgroundService)
- [x] Dead Letter Queue (DLQ) for failed messages
- [x] DepartmentService EmployeeCount updated via events
- [x] Docker Compose with RabbitMQ + SQL Server

### Day 4 — Distributed Caching, Health Checks & Resilience
- [x] Redis distributed cache (RedisCacheService)
- [x] Cache on Employee reads + invalidation on writes
- [x] Cache on Department reads + invalidation on writes
- [x] Health checks: SQL Server + Redis + RabbitMQ
- [x] /health / /health/live / /health/ready endpoints
- [x] Polly: Retry + CircuitBreaker + Timeout + Fallback
- [x] IHttpClientFactory with Polly policies

### Day 5 — Observability, Logging & Monitoring
- [x] Serilog structured logging (Console + rolling file)
- [x] CorrelationId enrichment on every log line
- [x] LoggingMiddleware for request/response logging
- [x] OpenTelemetry tracing (ASP.NET Core + HttpClient + SqlClient)
- [x] OTel Metrics (requests, cache hits/misses, RabbitMQ messages)
- [x] Metrics HTTP endpoint GET /api/metrics
- [x] Serilog on all 4 services

### Day 6 — Docker & Kubernetes
- [x] Multi-stage Dockerfiles (build → runtime, non-root user)
- [x] docker-compose.yml with microservices-net network
- [x] Container names used in all service URLs (not localhost)
- [x] Health checks + depends_on in docker-compose
- [x] Kubernetes Namespace, ConfigMap, Secrets
- [x] K8s Deployments with liveness + readiness probes
- [x] K8s NodePort Services (30000-30003)
- [x] K8s PersistentVolumeClaims for SQL Server + RabbitMQ + Redis

### Day 7 — Final Integration & Capstone
- [x] Architecture diagram
- [x] Complete README with all documentation
- [x] Postman collection (EmployeeManagement.postman_collection.json)
- [x] Final checklist verified
- [x] Failure scenario handling documented
- [x] All troubleshooting tips documented
