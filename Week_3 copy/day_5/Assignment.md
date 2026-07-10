# Week 3 Day 5 — Assignment

## Objective
Extend the Day 4 project with:
- Global Exception Handling via custom middleware
- Standardized API responses using `ApiResponse<T>`
- Structured logging in Controller, Service, and Repository layers

---

## Required New Files

| File | Purpose |
|------|---------|
| `Responses/ApiResponse.cs` | Generic response wrapper used by all endpoints |
| `Middleware/ExceptionMiddleware.cs` | Catches all unhandled exceptions globally |

## Modified Files

| File | Change |
|------|--------|
| `Controllers/EmployeesController.cs` | Returns `ApiResponse<T>`, no try/catch, added logging |
| `Services/EmployeeService.cs` | Added `ILogger<EmployeeService>` + log statements |
| `Repositories/EmployeeRepository.cs` | Added `ILogger<EmployeeRepository>` + log statements |
| `Program.cs` | Registers `ExceptionMiddleware` as first middleware |
| `appsettings.json` | Added namespace-level log level config |

---

## Sample Success Responses

### GET /api/employees → 200 OK
```json
{
  "Success": true,
  "StatusCode": 200,
  "Message": "Successfully retrieved 3 employee(s).",
  "Data": [
    {
      "EmployeeId": 1,
      "FirstName": "Alice",
      "LastName": "Johnson",
      "FullName": "Alice Johnson",
      "Email": "alice.johnson@company.com",
      "Phone": "555-0101",
      "Position": "Senior Developer",
      "Salary": 95000.00,
      "HireDate": "2020-03-15T00:00:00",
      "IsActive": true,
      "DepartmentId": 1,
      "DepartmentName": "Engineering"
    }
  ],
  "Errors": null
}
```

### POST /api/employees → 201 Created
```json
{
  "Success": true,
  "StatusCode": 201,
  "Message": "Resource created successfully.",
  "Data": {
    "EmployeeId": 4,
    "FirstName": "David",
    "LastName": "Brown",
    "FullName": "David Brown",
    "DepartmentName": "Marketing"
  },
  "Errors": null
}
```

### DELETE /api/employees/3 → 200 OK
```json
{
  "Success": true,
  "StatusCode": 200,
  "Message": "Employee with ID 3 was deleted successfully.",
  "Data": null,
  "Errors": null
}
```

---

## Sample Error Responses

### GET /api/employees/999 → 404 Not Found
```json
{
  "Success": false,
  "StatusCode": 404,
  "Message": "Employee with ID 999 was not found.",
  "Data": null,
  "Errors": null
}
```

### PUT with ID mismatch → 400 Bad Request
```json
{
  "Success": false,
  "StatusCode": 400,
  "Message": "Route ID (1) does not match body EmployeeId (5).",
  "Data": null,
  "Errors": null
}
```

### Unhandled Exception → 500 Internal Server Error (from ExceptionMiddleware)
```json
{
  "Success": false,
  "StatusCode": 500,
  "Message": "An unexpected error occurred. Please try again later.",
  "Data": null,
  "Errors": null
}
```

---

## Grading Criteria

| Criterion | Points |
|-----------|--------|
| `ApiResponse<T>` wraps all responses consistently | 20 |
| `ExceptionMiddleware` catches all unhandled exceptions | 20 |
| Middleware registered FIRST in `Program.cs` | 10 |
| Controller logs Information + Warning for each action | 15 |
| Service logs Information + Warning at each operation | 15 |
| Repository logs Information at each DB operation | 10 |
| All 5 CRUD endpoints still work correctly | 10 |
| **Total** | **100** |

---

## Bonus (Optional)
- Add `RequestLoggingMiddleware` that logs method, path, status code, and duration for every request.
- Throw a `KeyNotFoundException` from the service and confirm ExceptionMiddleware returns 404.
- Integrate Application Insights or Serilog for structured log sinks.
