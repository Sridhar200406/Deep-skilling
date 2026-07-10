# Week 3 Day 1 — Tasks

## Task 1 — Create the Project
- [ ] Create a new ASP.NET Core Web API project in Visual Studio 2022 targeting .NET 8.
- [ ] Name it `Week3_EmployeeManagementAPI`.
- [ ] Confirm Swagger/OpenAPI is enabled in the project wizard.

## Task 2 — Add NuGet Packages
- [ ] Install `Microsoft.EntityFrameworkCore.SqlServer` (v8.0.0).
- [ ] Install `Microsoft.EntityFrameworkCore.Tools` (v8.0.0).
- [ ] Install `Swashbuckle.AspNetCore` (v6.5.0).

## Task 3 — Create Models
- [ ] Create `Models/Employee.cs` — copy from Week 2 (or use solution code).
- [ ] Create `Models/Department.cs` — copy from Week 2 (or use solution code).
- [ ] Verify `[JsonIgnore]` is on `Department.Employees` to prevent circular JSON.

## Task 4 — Create AppDbContext
- [ ] Create `Data/AppDbContext.cs`.
- [ ] Add `DbSet<Employee>` and `DbSet<Department>`.
- [ ] Configure the one-to-many relationship in `OnModelCreating`.
- [ ] Add seed data for at least 2 departments and 3 employees.

## Task 5 — Configure Connection String
- [ ] Open `appsettings.json`.
- [ ] Add `"DefaultConnection"` pointing to your LocalDB or SQL Server.
- [ ] Register `AddDbContext<AppDbContext>` in `Program.cs`.

## Task 6 — Create the Service Layer
- [ ] Create `Services/IEmployeeService.cs` with `GetAllEmployeesAsync` and `GetEmployeeByIdAsync`.
- [ ] Create `Services/EmployeeService.cs` implementing the interface.
- [ ] Register `AddScoped<IEmployeeService, EmployeeService>` in `Program.cs`.

## Task 7 — Create EmployeesController
- [ ] Create `Controllers/EmployeesController.cs`.
- [ ] Add `GET /api/employees` endpoint returning all employees.
- [ ] Add `GET /api/employees/{id}` endpoint returning one employee or 404.
- [ ] Use `IActionResult` for all return types.
- [ ] Inject `IEmployeeService` via constructor.

## Task 8 — Run Migrations
- [ ] Open Package Manager Console.
- [ ] Run `Add-Migration InitialCreate`.
- [ ] Run `Update-Database`.
- [ ] Confirm tables exist in SQL Server Object Explorer.

## Task 9 — Test with Swagger
- [ ] Press F5 to run.
- [ ] Confirm Swagger UI loads at `http://localhost:5000`.
- [ ] Test `GET /api/employees` — should return 200 with employee array.
- [ ] Test `GET /api/employees/1` — should return 200 with one employee.
- [ ] Test `GET /api/employees/999` — should return 404 with error message.

## Task 10 — Verify REST Best Practices
- [ ] Routes use plural nouns (`/api/employees`).
- [ ] HTTP status codes are correct (200, 404, 500).
- [ ] JSON responses include nested `Department` object.
- [ ] No DbContext used directly in controller.
