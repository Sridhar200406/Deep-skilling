# Week 3 Day 1 — Employee Management API

## Overview
Converts the Week 2 Entity Framework Core console/desktop Employee Management System
into a fully functional **ASP.NET Core 8 Web API** with Swagger/OpenAPI documentation.

---

## Project Structure

```
Week3_EmployeeManagementAPI/
│
├── Controllers/
│   └── EmployeesController.cs      ← REST endpoints for /api/employees
│
├── Models/
│   ├── Employee.cs                 ← Employee entity (reused from Week 2)
│   └── Department.cs              ← Department entity (reused from Week 2)
│
├── Data/
│   └── AppDbContext.cs             ← EF Core DbContext (reused from Week 2)
│
├── Services/
│   ├── IEmployeeService.cs         ← Interface (contract)
│   └── EmployeeService.cs         ← Implementation using AppDbContext
│
├── Properties/
│   └── launchSettings.json         ← Dev server URLs and environment
│
├── Program.cs                      ← App entry point, DI registration, middleware
├── appsettings.json                ← Connection string and logging config
├── appsettings.Development.json    ← Dev-only overrides
├── EmployeeManagementAPI.csproj    ← Project file with NuGet packages
└── README.md                       ← This file
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| Visual Studio 2022 | 17.x |
| .NET SDK | 8.0 |
| SQL Server / LocalDB | Any |

---

## How to Run

### Option A — Visual Studio 2022

1. Open `EmployeeManagementAPI.csproj` in Visual Studio 2022.
2. Verify the connection string in `appsettings.json` (default uses LocalDB).
3. Open **Package Manager Console** and run:
   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```
4. Press **F5** (or Ctrl+F5) to run.
5. Browser opens at `http://localhost:5000` → Swagger UI loads automatically.

### Option B — .NET CLI

```bash
cd Week3_EmployeeManagementAPI

# Restore packages
dotnet restore

# Apply migrations (creates DB and seeds data)
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the API
dotnet run
```

Open `http://localhost:5000` in your browser.

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/employees` | Returns all employees with department |
| GET | `/api/employees/{id}` | Returns employee by ID |

---

## Testing with Swagger UI

1. Navigate to `http://localhost:5000` (Swagger UI is the default page).
2. Expand **GET /api/employees** → click **Try it out** → **Execute**.
3. Expand **GET /api/employees/{id}** → enter `1` → **Execute**.

---

## Sample JSON Responses

### GET /api/employees  →  200 OK

```json
[
  {
    "EmployeeId": 1,
    "FirstName": "Alice",
    "LastName": "Johnson",
    "Email": "alice.johnson@company.com",
    "Phone": "555-0101",
    "Position": "Senior Developer",
    "Salary": 95000.00,
    "HireDate": "2020-03-15T00:00:00",
    "IsActive": true,
    "DepartmentId": 1,
    "Department": {
      "DepartmentId": 1,
      "DepartmentName": "Engineering",
      "Description": "Software development and architecture"
    }
  },
  {
    "EmployeeId": 2,
    "FirstName": "Bob",
    "LastName": "Smith",
    "Email": "bob.smith@company.com",
    "Phone": "555-0102",
    "Position": "HR Manager",
    "Salary": 75000.00,
    "HireDate": "2019-06-01T00:00:00",
    "IsActive": true,
    "DepartmentId": 2,
    "Department": {
      "DepartmentId": 2,
      "DepartmentName": "Human Resources",
      "Description": "Recruitment and employee welfare"
    }
  }
]
```

### GET /api/employees/1  →  200 OK

```json
{
  "EmployeeId": 1,
  "FirstName": "Alice",
  "LastName": "Johnson",
  "Email": "alice.johnson@company.com",
  "Phone": "555-0101",
  "Position": "Senior Developer",
  "Salary": 95000.00,
  "HireDate": "2020-03-15T00:00:00",
  "IsActive": true,
  "DepartmentId": 1,
  "Department": {
    "DepartmentId": 1,
    "DepartmentName": "Engineering",
    "Description": "Software development and architecture"
  }
}
```

### GET /api/employees/999  →  404 Not Found

```json
{
  "message": "Employee with ID 999 was not found."
}
```

---

## Key Concepts Demonstrated

- **Dependency Injection** — `AppDbContext` and `EmployeeService` registered in `Program.cs`, injected via constructors.
- **Repository / Service pattern** — `IEmployeeService` interface keeps the controller decoupled from EF Core.
- **IActionResult** — `Ok()`, `NotFound()`, `StatusCode()` used for correct HTTP status codes.
- **Swagger/OpenAPI** — Auto-generated interactive docs via Swashbuckle.
- **EF Core** — `Include()` for eager loading, `AsNoTracking()` for read performance.
- **Seed Data** — Departments and Employees seeded in `OnModelCreating`.
- **REST conventions** — Plural noun route (`/api/employees`), proper 200/404/500 responses.
