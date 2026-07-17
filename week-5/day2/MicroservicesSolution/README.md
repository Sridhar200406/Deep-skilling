# Week 5 – Day 2: API Gateway & Inter-Service Communication

## Project Structure

```
MicroservicesSolution/
├── ApiGateway/                        ← Ocelot API Gateway (port 5000)
│   ├── Middleware/
│   │   └── GatewayExceptionMiddleware.cs   ← Centralized error handling
│   ├── ocelot.json                    ← Route configuration
│   ├── appsettings.json
│   └── Program.cs
│
├── AuthenticationService/             ← JWT issuer (port 5001)
│   ├── Controllers/AuthController.cs  ← /api/auth/register, /api/auth/login
│   ├── Data/AuthDbContext.cs
│   ├── DTOs/
│   ├── Models/AppUser.cs
│   ├── Services/TokenService.cs       ← JWT token generation
│   └── Program.cs
│
├── EmployeeService/                   ← Employee CRUD (port 5002)
│   ├── Controllers/EmployeesController.cs
│   ├── HttpClients/
│   │   └── DepartmentServiceClient.cs ← Typed HttpClient (inter-service comm)
│   ├── Services/EmployeeBusinessService.cs  ← Enriches DepartmentName via HTTP
│   ├── Middleware/ExceptionMiddleware.cs
│   └── Program.cs
│
├── DepartmentService/                 ← Department CRUD (port 5003)
│   ├── Controllers/DepartmentsController.cs
│   ├── Services/DepartmentService.cs
│   ├── Repositories/DepartmentRepository.cs
│   └── Program.cs
│
└── README.md
```

---

## Port Assignments

| Service               | Port |
|-----------------------|------|
| API Gateway           | 5000 |
| AuthenticationService | 5001 |
| EmployeeService       | 5002 |
| DepartmentService     | 5003 |

---

## Day 2 Key Additions

### 1. API Gateway (Ocelot)
Routes all public traffic through port 5000.

| Gateway URL                          | Downstream                            |
|--------------------------------------|---------------------------------------|
| `/gateway/auth/{everything}`         | AuthenticationService :5001           |
| `/gateway/employees`                 | EmployeeService :5002 *(JWT required)*|
| `/gateway/employees/{id}`            | EmployeeService :5002 *(JWT required)*|
| `/gateway/departments`               | DepartmentService :5003 *(JWT required)*|
| `/gateway/departments/{id}`          | DepartmentService :5003 *(JWT required)*|
| `/gateway/internal/departments/{id}` | DepartmentService :5003 (service call) |

### 2. Inter-Service Communication
`EmployeeService` uses a **Typed HttpClient** (`DepartmentServiceClient`) to call
`DepartmentService` and enrich every `EmployeeReadDto` with `DepartmentName`.

```csharp
// DI registration in EmployeeService/Program.cs
builder.Services.AddHttpClient<DepartmentServiceClient>(client => {
    client.BaseAddress = new Uri("http://localhost:5003");
    client.Timeout     = TimeSpan.FromSeconds(10);
});
```

### 3. Service-to-Service Authentication
The `DepartmentServiceClient` **forwards the caller's JWT** from the incoming
request header, so DepartmentService accepts the call as authenticated:

```csharp
var token = _httpContextAccessor.HttpContext?
    .Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
_httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

### 4. Centralized Error Handling
- **Gateway level** — `GatewayExceptionMiddleware` (ApiGateway)
- **Service level** — `ExceptionMiddleware` (EmployeeService)

Both return a consistent JSON envelope:
```json
{
  "success": false,
  "statusCode": 500,
  "message": "An unexpected error occurred.",
  "detail": null
}
```

---

## Setup Instructions

### Prerequisites
- .NET 8 SDK
- SQL Server LocalDB (or update connection strings)

### 1. Restore & Build
```bash
cd MicroservicesSolution
dotnet restore
dotnet build
```

### 2. Run All Services (4 separate terminals)

**Terminal 1 — AuthenticationService**
```bash
cd AuthenticationService
dotnet run
# Swagger: http://localhost:5001/swagger
```

**Terminal 2 — DepartmentService**
```bash
cd DepartmentService
dotnet run
# Swagger: http://localhost:5003/swagger
```

**Terminal 3 — EmployeeService**
```bash
cd EmployeeService
dotnet run
# Swagger: http://localhost:5002/swagger
```

**Terminal 4 — ApiGateway**
```bash
cd ApiGateway
dotnet run
# Gateway: http://localhost:5000
```

---

## Testing Instructions

### Step 1 — Register a user
```http
POST http://localhost:5000/gateway/auth/register
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "password": "Password1"
}
```
Copy the `token` from the response.

### Step 2 — Use JWT for protected routes
```http
GET http://localhost:5000/gateway/employees
Authorization: Bearer <paste-token-here>
```

### Step 3 — Create an employee
```http
POST http://localhost:5000/gateway/employees
Authorization: Bearer <token>
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@company.com",
  "position": "Developer",
  "salary": 85000,
  "hireDate": "2024-01-15T00:00:00",
  "departmentId": 1,
  "isActive": true
}
```

### Step 4 — Get employee (enriched with DepartmentName)
```http
GET http://localhost:5000/gateway/employees/1
Authorization: Bearer <token>
```
Response includes `"departmentName": "Engineering"` — fetched live from DepartmentService.

### Step 5 — List departments
```http
GET http://localhost:5000/gateway/departments
Authorization: Bearer <token>
```

### Step 6 — Health checks (no token needed)
```
GET http://localhost:5001/api/auth/health
GET http://localhost:5002/api/employees/health
GET http://localhost:5003/api/departments/health
```

---

## Connection Strings

All three services default to LocalDB. Change in each `appsettings.json` if needed:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=<ServiceDb>;Trusted_Connection=True;"
}
```

Databases created automatically on first run via `EnsureCreatedAsync()`.

---

## JWT Configuration

The **same secret** must be set identically in:
- `AuthenticationService/appsettings.json` — issues tokens
- `EmployeeService/appsettings.json` — validates tokens
- `DepartmentService/appsettings.json` — validates tokens
- `ApiGateway/appsettings.json` — validates at gateway level

```json
"JwtSettings": {
  "Secret": "SuperSecretKey@2024_MicroservicesWeek5_MinLength32Chars!",
  "Issuer": "AuthenticationService",
  "Audience": "MicroservicesClients",
  "ExpirationMinutes": 60
}
```
