# Week 3 Day 1 — Theory: ASP.NET Core Web API Fundamentals

## 1. What is ASP.NET Core Web API?

ASP.NET Core Web API is a framework for building HTTP services (REST APIs) on top of .NET.
It runs cross-platform, is lightweight, and integrates tightly with the .NET DI container.

---

## 2. REST API Principles

| Principle | Meaning |
|-----------|---------|
| **Uniform Interface** | Use standard HTTP verbs (GET, POST, PUT, DELETE) |
| **Stateless** | Each request contains all info the server needs |
| **Resource-based URLs** | `/api/employees`, `/api/employees/1` |
| **HTTP Status Codes** | 200 OK, 201 Created, 404 Not Found, 500 Server Error |
| **JSON** | Standard data exchange format |

---

## 3. ASP.NET Core Web API Project Structure

```
Program.cs          ← Entry point. Configure DI and middleware pipeline.
Controllers/        ← Handle HTTP requests. Thin — delegate to services.
Models/             ← Data entities / domain objects.
Data/               ← DbContext. EF Core lives here.
Services/           ← Business logic. Controllers call services.
appsettings.json    ← Config (connection strings, logging).
```

---

## 4. Program.cs — Two-Phase Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// PHASE 1: Register services into DI container
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddSwaggerGen();

var app = builder.Build();  // ← Freeze registrations, build the app

// PHASE 2: Configure middleware pipeline
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 5. Dependency Injection (DI)

DI is built into ASP.NET Core. You register services once; the framework injects them.

**Three lifetimes:**

| Lifetime | Meaning | Use For |
|----------|---------|---------|
| `Singleton` | One instance for entire app life | Config, caches |
| `Scoped` | One per HTTP request | DbContext, services using DbContext |
| `Transient` | New instance every time | Lightweight, stateless services |

```csharp
// Scoped is correct for services that use DbContext
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
```

---

## 6. [ApiController] and ControllerBase

- `[ApiController]` — enables automatic model validation, binding source inference.
- `ControllerBase` — base class for API controllers (no View support — that's `Controller`).
- `[Route("api/[controller]")]` — sets the route to `api/employees` (from class name `EmployeesController`).

---

## 7. IActionResult Return Types

```csharp
return Ok(data);                    // 200 OK + body
return NotFound(new { message }); // 404 Not Found + body
return BadRequest(modelState);     // 400 Bad Request
return StatusCode(500, error);     // 500 Internal Server Error
return CreatedAtAction(...);        // 201 Created (for POST)
return NoContent();                 // 204 No Content (for DELETE/PUT)
```

---

## 8. Swagger / OpenAPI

Swashbuckle reads your controller routes and generates an OpenAPI spec (JSON).
Swagger UI renders that spec as an interactive web page.

```csharp
// Register
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

// Use
app.UseSwagger();       // serves /swagger/v1/swagger.json
app.UseSwaggerUI();     // serves interactive HTML UI
```

---

## 9. EF Core in Web API — Key Patterns

```csharp
// Eager loading — load related entity in same query (JOIN)
_context.Employees.Include(e => e.Department)

// AsNoTracking — skip change tracking for read-only queries (faster)
.AsNoTracking()

// Async — never block threads with I/O
await _context.Employees.ToListAsync();
await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
```

---

## 10. Service Layer Pattern

```
HTTP Request
    ↓
Controller  (routes, HTTP concerns, IActionResult)
    ↓
IEmployeeService  (interface — contract)
    ↓
EmployeeService  (implementation — queries DbContext)
    ↓
AppDbContext  (EF Core — SQL Server)
    ↓
SQL Server Database
```

The controller never touches DbContext directly. This makes the code testable and maintainable.
