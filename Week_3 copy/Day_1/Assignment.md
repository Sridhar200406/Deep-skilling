# Week 3 Day 1 — Assignment

## Objective
Convert the Week 2 Employee Management System (EF Core console app) into a
fully working ASP.NET Core 8 Web API with Swagger documentation.

---

## Deliverables

### 1. Working Project
Submit the complete `Week3_EmployeeManagementAPI` project that:
- Compiles without errors in Visual Studio 2022 (.NET 8)
- Connects to SQL Server / LocalDB
- Seeds departments and employees on first run
- Exposes REST endpoints at `/api/employees`

### 2. Required Endpoints

| Method | Route | Expected Response |
|--------|-------|-------------------|
| GET | `/api/employees` | 200 OK — array of all employees |
| GET | `/api/employees/{id}` | 200 OK — single employee, or 404 Not Found |

### 3. Required Files

| File | Purpose |
|------|---------|
| `Models/Employee.cs` | Employee entity with all fields |
| `Models/Department.cs` | Department entity |
| `Data/AppDbContext.cs` | DbContext with seed data |
| `Services/IEmployeeService.cs` | Interface |
| `Services/EmployeeService.cs` | Implementation |
| `Controllers/EmployeesController.cs` | REST controller |
| `Program.cs` | DI + middleware setup |
| `appsettings.json` | Connection string |

---

## Grading Criteria

| Criterion | Points |
|-----------|--------|
| Project compiles and runs | 20 |
| GET /api/employees returns correct JSON | 20 |
| GET /api/employees/{id} returns 200 or 404 | 20 |
| Swagger UI accessible and functional | 15 |
| Service layer correctly abstracts DbContext | 15 |
| REST best practices followed | 10 |
| **Total** | **100** |

---

## Submission Instructions

1. Ensure `Update-Database` has been run and data is seeded.
2. Run the project and take a screenshot of Swagger UI showing both endpoints.
3. Test both endpoints using Swagger and screenshot the responses.
4. Submit the project folder and screenshots.

---

## Bonus (Optional)
- Add XML doc comments (`///`) to all controller actions and generate them in Swagger.
- Add a `GET /api/departments` endpoint following the same pattern.
- Add request logging middleware that logs method, path, and response status.
