# Week 3 Day 3 — Theory: Repository Pattern & Dependency Injection

## 1. Why Repository Pattern?

Without it (Day 2):
```
Controller → Service → AppDbContext (EF Core) → SQL Server
```
`EmployeeService` directly called `_context.Employees.Include(...).ToListAsync()`.
That means the service was tightly coupled to EF Core — hard to test, hard to swap databases.

With it (Day 3):
```
Controller → Service → Repository → AppDbContext → SQL Server
```
The service now speaks only to an interface (`IEmployeeRepository`).
You can swap EF Core for Dapper, or mock the repository in unit tests, without touching the service.

---

## 2. The Four Layers — Responsibilities

| Layer | Class | Knows About | Does Not Know About |
|-------|-------|-------------|---------------------|
| Controller | `EmployeesController` | `IEmployeeService` | Repository, DbContext |
| Service | `EmployeeService` | `IEmployeeRepository` | DbContext, EF Core |
| Repository | `EmployeeRepository` | `AppDbContext` (EF Core) | Service, Controller |
| DbContext | `AppDbContext` | SQL Server | Everything above |

Each layer only knows the layer directly below it — via an interface.

---

## 3. Interfaces Folder — Why Separate?

Day 2 had `IEmployeeService` inside the `Services/` folder.
Day 3 moves both `IEmployeeService` and `IEmployeeRepository` to `Interfaces/`.

Benefits:
- One place to find all contracts.
- Circular dependency risk eliminated (Services/ doesn't reference Repositories/).
- Clearer separation between "what" (interfaces) and "how" (implementations).

---

## 4. Dependency Injection — How It Works in ASP.NET Core

DI has three steps:

**Step 1 — Register** (in `Program.cs`):
```csharp
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
```

**Step 2 — Request** (via constructor):
```csharp
// EmployeeRepository requests AppDbContext
public EmployeeRepository(AppDbContext context) { ... }

// EmployeeService requests IEmployeeRepository
public EmployeeService(IEmployeeRepository repository) { ... }

// Controller requests IEmployeeService
public EmployeesController(IEmployeeService service, ILogger logger) { ... }
```

**Step 3 — Resolve** (ASP.NET Core does this automatically per request):
```
HTTP Request arrives
    → DI creates AppDbContext (Scoped)
    → DI creates EmployeeRepository(AppDbContext)         [IEmployeeRepository]
    → DI creates EmployeeService(EmployeeRepository)      [IEmployeeService]
    → DI creates EmployeesController(EmployeeService, Logger)
    → Controller action runs
    → All objects disposed at end of request
```

---

## 5. Service Lifetimes

| Lifetime | Created | Destroyed | Use For |
|----------|---------|-----------|---------|
| `Singleton` | App start | App stop | Config, caches |
| `Scoped` | Per HTTP request | End of request | DbContext, Repository, Service |
| `Transient` | Every injection | After use | Lightweight stateless helpers |

`Scoped` is correct for Repository and Service because they depend on `DbContext`,
which is itself Scoped. Injecting a Scoped service into a Singleton would cause a
**captive dependency** bug — ASP.NET Core will throw at startup if you do this.

---

## 6. Repository Pattern — Key EF Core Methods

| Method | SQL Generated | When |
|--------|--------------|------|
| `ToListAsync()` | `SELECT * FROM Employees` | GetAll |
| `FirstOrDefaultAsync(e => e.Id == id)` | `SELECT TOP 1 WHERE Id=@id` | GetById |
| `AddAsync(entity)` + `SaveChangesAsync()` | `INSERT INTO Employees` | Create |
| `Update(entity)` + `SaveChangesAsync()` | `UPDATE Employees SET...` | Update |
| `Remove(entity)` + `SaveChangesAsync()` | `DELETE FROM Employees` | Delete |
| `AnyAsync(e => e.Id == id)` | `SELECT CASE WHEN EXISTS(...)` | Exists check |

---

## 7. AsNoTracking — Read vs Write

```csharp
// READ — skip change tracker (faster, less memory)
.AsNoTracking().ToListAsync()

// WRITE — do NOT use AsNoTracking
// EF Core must track the entity to know what changed
_context.Employees.Update(existing);
await _context.SaveChangesAsync();
```

---

## 8. LoadDepartmentAsync — Why It's Needed

After `AddAsync` or `UpdateAsync`, the entity is tracked but its navigation property
`Department` is null (EF Core doesn't auto-load it).

```csharp
await _context.Entry(employee)
    .Reference(e => e.Department)
    .LoadAsync();
// Now employee.Department is populated for the JSON response
```

---

## 9. Full DI Chain Visualized

```
Program.cs registers:
  IEmployeeRepository  ←→  EmployeeRepository
  IEmployeeService     ←→  EmployeeService

HTTP POST /api/employees
    ↓
DI resolves: EmployeesController
    → needs IEmployeeService
    → DI resolves: EmployeeService
        → needs IEmployeeRepository
        → DI resolves: EmployeeRepository
            → needs AppDbContext
            → DI resolves: AppDbContext (Scoped, one per request)

Controller.CreateEmployee(employee)
    → _employeeService.CreateEmployeeAsync(employee)
        → _repository.AddAsync(employee)       ← EmployeeRepository
            → _context.Employees.AddAsync()    ← AppDbContext
            → _context.SaveChangesAsync()
        → _repository.LoadDepartmentAsync()
    ← returns created employee
← returns 201 Created
```
