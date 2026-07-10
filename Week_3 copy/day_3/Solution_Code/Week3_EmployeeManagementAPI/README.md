# Week 3 Day 3 — Repository Pattern + Dependency Injection

## What Changed from Day 2

| Change | Detail |
|--------|--------|
| New folder `Interfaces/` | Contains `IEmployeeRepository` and `IEmployeeService` |
| New folder `Repositories/` | Contains `EmployeeRepository` (only class touching DbContext) |
| `EmployeeService` refactored | Now injects `IEmployeeRepository` — zero EF Core imports |
| `EmployeesController` | Updated `using` to `Interfaces` namespace — logic unchanged |
| `Program.cs` | Registers `IEmployeeRepository → EmployeeRepository` |

---

## Full Project Structure

```
Week3_EmployeeManagementAPI/
│
├── Controllers/
│   └── EmployeesController.cs      ← Talks to IEmployeeService only
│
├── Interfaces/
│   ├── IEmployeeRepository.cs      ← NEW: data access contract
│   └── IEmployeeService.cs         ← Moved from Services/
│
├── Repositories/
│   └── EmployeeRepository.cs       ← NEW: only class using AppDbContext
│
├── Services/
│   └── EmployeeService.cs          ← Refactored: uses IEmployeeRepository
│
├── Models/
│   ├── Employee.cs
│   └── Department.cs
│
├── Data/
│   └── AppDbContext.cs             ← Unchanged
│
├── Properties/
│   └── launchSettings.json
│
├── Program.cs                      ← Registers IEmployeeRepository + IEmployeeService
├── appsettings.json
├── EmployeeManagementAPI.csproj
└── README.md
```

---

## Dependency Injection Chain

```
Program.cs registers:
  AppDbContext              (Scoped)
  IEmployeeRepository  →  EmployeeRepository  (Scoped)
  IEmployeeService     →  EmployeeService     (Scoped)

Per HTTP request, DI resolves:
  EmployeesController
      └─ EmployeeService         [IEmployeeService]
              └─ EmployeeRepository   [IEmployeeRepository]
                      └─ AppDbContext     [DbContext]
```

---

## All Endpoints (unchanged from Day 2)

| Method | Route | Response |
|--------|-------|----------|
| GET    | `/api/employees`      | 200 + array |
| GET    | `/api/employees/{id}` | 200 / 404 |
| POST   | `/api/employees`      | 201 Created |
| PUT    | `/api/employees/{id}` | 200 / 404 |
| DELETE | `/api/employees/{id}` | 204 / 404 |

---

## How to Run

Open `EmployeeManagementAPI.csproj` in Visual Studio 2022.

DB already exists from Day 1/2 → just press **F5**. No new migration needed.

If fresh start:
```powershell
Add-Migration InitialCreate
Update-Database
```

Open `http://localhost:5000` → Swagger UI with all 5 endpoints.

---

## Why This Architecture Matters

**Testability** — Mock `IEmployeeRepository` in unit tests. `EmployeeService` logic
is tested without ever touching the database.

**Swappability** — Replace `EmployeeRepository` with a Dapper or in-memory
implementation without touching `EmployeeService` or `EmployeesController`.

**Single Responsibility** — Each class has exactly one job:
- Controller: HTTP input/output
- Service: business rules
- Repository: database queries
- DbContext: EF Core configuration
