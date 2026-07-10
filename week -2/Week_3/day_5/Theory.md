# Week 3 Day 5 — Theory: Global Exception Handling, Logging, Custom Middleware, Standardized Responses

## 1. Why Standardized API Responses?

Without a standard shape, different endpoints return different structures:
```json
// GET success
{ "EmployeeId": 1, "FirstName": "Alice" ... }

// 404 error
{ "message": "Employee not found." }

// 500 error
{ "title": "An error occurred" }
```

The client must handle three different formats.

With `ApiResponse<T>`, every response has the same shape:
```json
{
  "Success": true,
  "StatusCode": 200,
  "Message": "Successfully retrieved 3 employee(s).",
  "Data": [ ... ],
  "Errors": null
}
```

Client code becomes simple and predictable:
```js
if (response.Success) { use(response.Data); }
else { show(response.Message); }
```

---

## 2. ApiResponse<T> Design

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    // Factory helpers avoid repeated object initializers in every controller action
    public static ApiResponse<T> SuccessResponse(T data, string message) => ...
    public static ApiResponse<T> NotFoundResponse(string message) => ...
    public static ApiResponse<T> BadRequestResponse(string message) => ...
    public static ApiResponse<T> ServerErrorResponse(string message) => ...
}
```

---

## 3. Custom Middleware — How It Works

ASP.NET Core middleware is a chain of components. Each component:
1. Receives the `HttpContext`
2. Optionally processes it
3. Calls `_next(context)` to pass to the next component
4. Optionally processes the response on the way back

```
Request →
  ExceptionMiddleware.InvokeAsync()
    → try { await _next(context); }   ← passes to Swagger, then Controller
    → catch (Exception ex) { ... }    ← catches anything that bubbled up
← Response
```

Registration order in `Program.cs` determines execution order:
```csharp
app.UseMiddleware<ExceptionMiddleware>();  // ← FIRST — wraps everything
app.UseSwagger();
app.UseRouting();
app.MapControllers();                     // ← LAST
```

---

## 4. Global Exception Handling Middleware

```csharp
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);   // run the rest of the pipeline
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", ...);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Map exception type → HTTP status code
        var statusCode = ex switch
        {
            ArgumentException        => 400,
            KeyNotFoundException     => 404,
            UnauthorizedAccessException => 401,
            _                        => 500
        };

        // Write JSON response
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorBody));
    }
}
```

Benefits:
- Controllers don't need try/catch for unexpected errors.
- Stack traces never leak to clients in Production.
- One place to add APM/monitoring hooks.

---

## 5. ILogger<T> — Structured Logging

ASP.NET Core has built-in structured logging. Inject `ILogger<T>` where T is the class name
(used as the log category).

```csharp
public class EmployeeService
{
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(ILogger<EmployeeService> logger) { ... }

    public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync()
    {
        _logger.LogInformation("Fetching all employees.");        // level: Info
        _logger.LogWarning("Employee {Id} not found.", id);       // level: Warn
        _logger.LogError(ex, "Database error for ID {Id}.", id);  // level: Error
    }
}
```

Log levels (lowest → highest):
```
Trace → Debug → Information → Warning → Error → Critical
```

Configured per namespace in `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Week3_EmployeeManagementAPI": "Information",
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

Structured logging with named parameters:
```csharp
// BAD — string interpolation loses structure
_logger.LogInformation($"Employee {id} not found");

// GOOD — named parameter preserved in log sinks (Application Insights, Seq, etc.)
_logger.LogWarning("Employee {EmployeeId} not found", id);
```

---

## 6. Logging in Each Layer

| Layer | Logs |
|-------|------|
| Controller | Request received (method + path), result (count / ID / status) |
| Service | Business operation start/end, not-found warnings |
| Repository | SQL operation start/end, record counts, insert IDs |
| ExceptionMiddleware | Full exception with stack trace for unhandled errors |

---

## 7. Exception Types → HTTP Status Codes

| Exception | Status Code |
|-----------|-------------|
| `ArgumentNullException` | 400 Bad Request |
| `ArgumentException` | 400 Bad Request |
| `KeyNotFoundException` | 404 Not Found |
| `InvalidOperationException` | 400 Bad Request |
| `UnauthorizedAccessException` | 401 Unauthorized |
| Any other | 500 Internal Server Error |

---

## 8. Full Request Flow — Day 5

```
HTTP Request
    ↓
ExceptionMiddleware (wraps entire pipeline)
    ↓
EmployeesController.GetAllEmployees()
    → _logger.LogInformation("GET /api/employees")
    → _employeeService.GetAllEmployeesAsync()
        → _logger.LogInformation("Fetching all employees")
        → _repository.GetAllAsync()
            → _logger.LogInformation("SELECT all employees")
            → EF Core → SQL Server
            → _logger.LogInformation("Returned {Count} records")
        → _mapper.Map<List<EmployeeReadDto>>(employees)
    → _logger.LogInformation("Returned {Count} employees")
    → return Ok(ApiResponse<List<EmployeeReadDto>>.SuccessResponse(employees))
    ↓
JSON Response:
{
  "Success": true,
  "StatusCode": 200,
  "Message": "Successfully retrieved 3 employee(s).",
  "Data": [ ... ],
  "Errors": null
}
```
