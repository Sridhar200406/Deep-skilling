# Week 3 Day 3 — Assignment

## Objective
Refactor the Day 2 CRUD API by introducing the Repository Pattern.
Add an `Interfaces/` folder and a `Repositories/` folder.
`EmployeeService` must no longer reference `AppDbContext` directly.

---

## Required New Files

| File | Purpose |
|------|---------|
| `Interfaces/IEmployeeRepository.cs` | Repository contract |
| `Interfaces/IEmployeeService.cs` | Service contract (moved from Services/) |
| `Repositories/EmployeeRepository.cs` | EF Core data access implementation |

## Modified Files

| File | Change |
|------|--------|
| `Services/EmployeeService.cs` | Inject `IEmployeeRepository` instead of `AppDbContext` |
| `Controllers/EmployeesController.cs` | Update `using` to `Interfaces` namespace |
| `Program.cs` | Register `IEmployeeRepository` + `IEmployeeService` |

---

## Architecture Diagram

```
HTTP Request
    ↓
EmployeesController   (uses IEmployeeService)
    ↓
EmployeeService        (uses IEmployeeRepository)
    ↓
EmployeeRepository     (uses AppDbContext)
    ↓
AppDbContext            (EF Core)
    ↓
SQL Server — EmployeeManagementDB
```

---

## DI Registration (Program.cs)

```csharp
// Order matters: register dependencies before dependents
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
```

---

## Grading Criteria

| Criterion | Points |
|-----------|--------|
| `IEmployeeRepository` interface with all 7 methods | 15 |
| `EmployeeRepository` implements all methods correctly | 20 |
| `EmployeeService` uses `IEmployeeRepository` (no DbContext reference) | 20 |
| All 5 CRUD endpoints still work after refactor | 20 |
| `IEmployeeRepository` and `IEmployeeService` both in `Interfaces/` | 10 |
| DI registered correctly in `Program.cs` | 10 |
| Swagger still shows all 5 endpoints | 5 |
| **Total** | **100** |

---

## Bonus (Optional)
- Create a generic `IRepository<T>` interface and have `IEmployeeRepository` extend it.
- Add a `DepartmentsController` following the same 4-layer pattern.
- Add a unit test project and mock `IEmployeeRepository` with Moq.
