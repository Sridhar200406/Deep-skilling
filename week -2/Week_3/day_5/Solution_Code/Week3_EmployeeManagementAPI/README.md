# Week 3 Day 5 — Global Exception Handling, Logging, Custom Middleware, Standardized Responses

## What Changed from Day 4

| Change | Detail |
|--------|--------|
| New `Responses/ApiResponse.cs` | Generic wrapper — every response has Success, StatusCode, Message, Data, Errors |
| New `Middleware/ExceptionMiddleware.cs` | Catches all unhandled exceptions; maps to HTTP codes; logs with ILogger |
| `Controllers/EmployeesController.cs` | Returns `ApiResponse<T>`, no try/catch, enriched logging |
| `Services/EmployeeService.cs` | Injected `ILogger<EmployeeService>`, structured log statements |
| `Repositories/EmployeeRepository.cs` | Injected `ILogger<EmployeeRepository>`, SQL operation logging |
| `Program.cs` | `app.UseMiddleware<ExceptionMiddleware>()` added as FIRST middleware |
| `appsettings.json` | Added namespace-level log config |

---

## Full Project Structure

```
Week3_EmployeeManagementAPI/
│
├── Controllers/
│   └── EmployeesController.cs      ← ApiResponse<T> + ILogger
│
├── Middleware/                      ← NEW
│   └── ExceptionMiddleware.cs      ← Global exception handler
│
├── Responses/                       ← NEW
│   └── ApiResponse.cs              ← Standardized response wrapper
│
├── DTOs/
│   ├── EmployeeCreateDto.cs
│   ├── EmployeeUpdateDto.cs
│   └── EmployeeReadDto.cs
│
├── Mapping/
│   └── EmployeeProfile.cs
│
├── Interfaces/
│   ├── IEmployeeRepository.cs
│   └── IEmployeeService.cs
│
├── Repositories/
│   └── EmployeeRepository.cs       ← ILogger added
│
├── Services/
│   └── EmployeeService.cs          ← ILogger added
│
├── Models/
│   ├── Employee.cs
│   └── Department.cs
│
├── Data/
│   └── AppDbContext.cs
│
├── Program.cs                      ← ExceptionMiddleware registered FIRST
├── appsettings.json                ← Namespace log levels configured
├── EmployeeManagementAPI.csproj
└── README.md
```

---

## Middleware Pipeline Order

```
Request
  ↓
ExceptionMiddleware      ← wraps everything; catches any unhandled exception
  ↓
SwaggerUI / Swagger
  ↓
HttpsRedirection
  ↓
Authorization
  ↓
Controllers              ← actual endpoint logic runs here
  ↑
Response flows back up through the same chain
```

---

## Standardized Response Shape

Every endpoint returns:
```json
{
  "Success": true | false,
  "StatusCode": 200 | 201 | 400 | 404 | 500,
  "Message": "human-readable string",
  "Data": { ... } | [ ... ] | null,
  "Errors": [ "error detail" ] | null
}
```

---

## How to Run

Open `EmployeeManagementAPI.csproj` in Visual Studio 2022.  
DB already exists → press **F5**.  
If fresh: `Add-Migration InitialCreate` + `Update-Database` first.

Open `http://localhost:5000` → Swagger UI with all 5 endpoints.

---

## Verify Logging

With Visual Studio running, open **View → Output → Show output from: ASP.NET Core Web Server**.

You'll see log output like:
```
info: Week3_EmployeeManagementAPI.Controllers.EmployeesController
      GET /api/employees — fetching all employees at 2026-07-05 10:00:00
info: Week3_EmployeeManagementAPI.Services.EmployeeService
      EmployeeService: Fetching all employees from repository.
info: Week3_EmployeeManagementAPI.Repositories.EmployeeRepository
      EmployeeRepository: Executing GetAllAsync — SELECT all employees with Department JOIN.
info: Week3_EmployeeManagementAPI.Repositories.EmployeeRepository
      EmployeeRepository: GetAllAsync returned 3 records.
info: Week3_EmployeeManagementAPI.Services.EmployeeService
      EmployeeService: Retrieved 3 employees.
info: Week3_EmployeeManagementAPI.Controllers.EmployeesController
      GET /api/employees — returned 3 employees
```
