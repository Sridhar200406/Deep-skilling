# Week 5 – Day 3: Messaging and Event-Driven Architecture

## What's New in Day 3

| Component | Day 2 | Day 3 Added |
|---|---|---|
| EmployeeService | HTTP CRUD | Publishes 3 RabbitMQ events on every write |
| DepartmentService | HTTP CRUD | Consumes all 3 events, updates EmployeeCount |
| Shared | — | Event contracts: EmployeeCreated/Updated/Deleted |
| Messaging/Producer | — | RabbitMQProducer with retry logic |
| Messaging/Consumer | — | RabbitMQConsumerService (BackgroundService) + DLQ |
| Infrastructure | — | docker-compose.yml (RabbitMQ + SQL Server) |

---

## Project Structure

```
MicroservicesSolution/
├── ApiGateway/                         ← Ocelot (port 5000)
├── AuthenticationService/              ← JWT issuer (port 5001)
├── EmployeeService/                    ← CRUD + EVENT PUBLISHER (port 5002)
│   └── Messaging/EmployeeEventPublisher.cs
├── DepartmentService/                  ← CRUD + EVENT CONSUMER (port 5003)
│   └── EventHandlers/
│       ├── EmployeeCreatedEventHandler.cs
│       ├── EmployeeUpdatedEventHandler.cs
│       └── EmployeeDeletedEventHandler.cs
├── Shared/
│   └── Events/
│       ├── BaseEvent.cs
│       ├── EmployeeCreatedEvent.cs
│       ├── EmployeeUpdatedEvent.cs
│       └── EmployeeDeletedEvent.cs
├── Messaging/
│   ├── Producer/RabbitMQProducer.cs    ← Publishes to exchange with retry
│   └── Consumer/RabbitMQConsumerService.cs  ← BackgroundService + DLQ
├── docker-compose.yml                  ← RabbitMQ + SQL Server
└── README.md
```

---

## Event-Driven Architecture Explained

```
EmployeeService                    RabbitMQ                   DepartmentService
     │                                │                              │
     │──POST /api/employees──────────►│                              │
     │                                │                              │
     │  EmployeeCreatedEvent ─────────► exchange: employee.events    │
     │  routing key: employee.created  │                              │
     │                                 │──queue: department.employee.q►│
     │                                 │                              │
     │                                 │         EmployeeCreatedEventHandler
     │                                 │         dept.EmployeeCount++
```

### Message Flow
1. Client calls `POST /gateway/employees` through the API Gateway
2. EmployeeService creates the employee in its DB
3. EmployeeService publishes `EmployeeCreatedEvent` to RabbitMQ exchange `employee.events` with routing key `employee.created`
4. RabbitMQ routes the message to queue `department.employee.q` (bound to `employee.*`)
5. DepartmentService `RabbitMQConsumerService` (BackgroundService) picks up the message
6. Dispatches to `EmployeeCreatedEventHandler` which increments `Department.EmployeeCount`
7. Message is ACKed — removed from queue

### Dead Letter Queue (DLQ)
If a handler throws an exception, the message is NACKed (requeue=false) and automatically routed to `employee.events.dlq` for manual inspection or replay.

---

## RabbitMQ Topology

| Resource | Type | Purpose |
|---|---|---|
| `employee.events` | Topic Exchange | Main exchange — all employee events |
| `department.employee.q` | Durable Queue | DepartmentService consumes from here |
| `employee.events.dlx` | Fanout Exchange | Dead letter exchange |
| `employee.events.dlq` | Durable Queue | Failed messages land here |

| Routing Key | Event |
|---|---|
| `employee.created` | EmployeeCreatedEvent |
| `employee.updated` | EmployeeUpdatedEvent |
| `employee.deleted` | EmployeeDeletedEvent |

---

## Setup Instructions

### Step 1 — Start RabbitMQ and SQL Server via Docker
```bash
cd MicroservicesSolution
docker-compose up -d
```
- RabbitMQ Management UI: http://localhost:15672 (guest / guest)
- SQL Server: localhost:1433

### Step 2 — Run all 4 services (separate terminals)

**Terminal 1**
```bash
cd AuthenticationService && dotnet run
```

**Terminal 2**
```bash
cd DepartmentService && dotnet run
```

**Terminal 3**
```bash
cd EmployeeService && dotnet run
```

**Terminal 4**
```bash
cd ApiGateway && dotnet run
```

---

## Testing Instructions

### 1. Register and get a token
```http
POST http://localhost:5000/gateway/auth/register
Content-Type: application/json

{ "firstName":"John","lastName":"Doe","email":"john@test.com","password":"Password1" }
```
Copy the `token` from the response.

### 2. Create an employee (triggers EmployeeCreatedEvent)
```http
POST http://localhost:5000/gateway/employees
Authorization: Bearer <token>
Content-Type: application/json

{
  "firstName":"Jane","lastName":"Smith","email":"jane@test.com",
  "position":"Developer","salary":85000,"hireDate":"2024-01-15T00:00:00",
  "departmentId":1,"isActive":true
}
```

### 3. Verify DepartmentService received the event
```http
GET http://localhost:5000/gateway/departments/1
Authorization: Bearer <token>
```
The `employeeCount` field should be incremented by 1.

### 4. Update employee to a different department (triggers EmployeeUpdatedEvent)
```http
PUT http://localhost:5000/gateway/employees/1
Authorization: Bearer <token>
Content-Type: application/json

{
  "employeeId":1,"firstName":"Jane","lastName":"Smith","email":"jane@test.com",
  "position":"Senior Developer","salary":95000,"hireDate":"2024-01-15T00:00:00",
  "departmentId":2,"isActive":true
}
```
- Department 1 `employeeCount` decrements
- Department 2 `employeeCount` increments

### 5. Delete employee (triggers EmployeeDeletedEvent)
```http
DELETE http://localhost:5000/gateway/employees/1
Authorization: Bearer <token>
```
Department `employeeCount` decrements.

### 6. View RabbitMQ queues in the Management UI
Open http://localhost:15672 → Queues tab
- `department.employee.q` — shows message rates
- `employee.events.dlq` — shows any failed messages

---

## Retry Mechanism

The `RabbitMQProducer` retries publishing up to **3 times** with a 2-second delay:
```csharp
while (attempt < maxRetries)
{
    try { _channel.BasicPublish(...); return; }
    catch (Exception ex) when (attempt < maxRetries)
    {
        Thread.Sleep(TimeSpan.FromSeconds(2));
    }
}
```

The consumer uses **manual ACK** (`autoAck: false`):
- Success → `BasicAck`
- Failure → `BasicNack(requeue: false)` → goes to DLQ

---

## Connection Strings

Update if not using Docker:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=<Db>;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;"
}
```
