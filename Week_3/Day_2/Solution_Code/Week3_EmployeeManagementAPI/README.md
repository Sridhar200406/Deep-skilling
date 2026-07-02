# Week 3 Day 2 — Full CRUD Employee Management API

## What Changed from Day 1

| File | Change |
|------|--------|
| `Models/Employee.cs` | Added `ErrorMessage` to validation attributes + `[Range]` on Salary and DepartmentId |
| `Services/IEmployeeService.cs` | Added `CreateEmployeeAsync`, `UpdateEmployeeAsync`, `DeleteEmployeeAsync`, `EmployeeExistsAsync` |
| `Services/EmployeeService.cs` | Implemented all 4 new methods |
| `Controllers/EmployeesController.cs` | Added POST, PUT, DELETE actions |
| `Program.cs` | Updated Swagger description to Day 2 |
| `EmployeeManagementAPI.csproj` | Added `GenerateDocumentationFile` for XML doc comments in Swagger |

---

## All Endpoints

| Method | Route | Description | Success Code |
|--------|-------|-------------|-------------|
| GET    | `/api/employees`      | All employees + department | 200 |
| GET    | `/api/employees/{id}` | Single employee by ID | 200 |
| POST   | `/api/employees`      | Create new employee | 201 + Location |
| PUT    | `/api/employees/{id}` | Full update of employee | 200 |
| DELETE | `/api/employees/{id}` | Delete employee | 204 |

---

## How to Run

Open `EmployeeManagementAPI.csproj` in Visual Studio 2022.

If running for the first time (no DB yet):
```powershell
# Package Manager Console
Add-Migration InitialCreate
Update-Database
```

If DB already exists from Day 1 — just press **F5**. No new migration needed.

Open `http://localhost:5000` — Swagger UI loads with all 5 endpoints.

---

## Testing in Swagger

### POST — Create Employee
1. Expand **POST /api/employees** → **Try it out**
2. Replace the body with:
```json
{
  "FirstName": "David",
  "LastName": "Brown",
  "Email": "david.brown@company.com",
  "Phone": "555-0201",
  "Position": "Marketing Specialist",
  "Salary": 65000.00,
  "HireDate": "2024-01-15T00:00:00",
  "IsActive": true,
  "DepartmentId": 4
}
```
3. Click **Execute** → expect **201 Created** with the new employee (EmployeeId = 4).

### PUT — Update Employee
1. Expand **PUT /api/employees/{id}** → **Try it out**
2. Set `id = 1`, body:
```json
{
  "EmployeeId": 1,
  "FirstName": "Alice",
  "LastName": "Johnson",
  "Email": "alice.johnson@company.com",
  "Phone": "555-0199",
  "Position": "Lead Developer",
  "Salary": 105000.00,
  "HireDate": "2020-03-15T00:00:00",
  "IsActive": true,
  "DepartmentId": 1
}
```
3. Click **Execute** → expect **200 OK** with updated data.

### DELETE — Remove Employee
1. Expand **DELETE /api/employees/{id}** → **Try it out**
2. Set `id = 3` → **Execute** → expect **204 No Content**.
3. Verify: GET /api/employees — employee 3 is gone.

### Test Validation (400)
- POST with `"FirstName": ""` → **400 Bad Request** with validation errors.
- PUT with route id=1 but body `"EmployeeId": 5` → **400 Bad Request**.

### Test Not Found (404)
- GET /api/employees/999 → **404 Not Found**.
- DELETE /api/employees/999 → **404 Not Found**.

---

## Key Design Decisions

**Why field-by-field update in PUT?**
Loading the entity first and applying fields prevents overwriting columns
the client didn't send. It also avoids EF Core concurrency issues.

**Why `employee.EmployeeId = 0` in CreateEmployeeAsync?**
Prevents a client from dictating the primary key. The DB `IDENTITY` column always assigns it.

**Why no `AsNoTracking()` on write operations?**
EF Core needs to track the entity to generate correct `UPDATE` and `DELETE` SQL.
`AsNoTracking()` is only for read-only queries.
