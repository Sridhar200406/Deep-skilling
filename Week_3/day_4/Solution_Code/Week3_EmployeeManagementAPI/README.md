# Week 3 Day 4 — DTOs, AutoMapper, and Model Validation

## What Changed from Day 3

| Change | Detail |
|--------|--------|
| New folder `DTOs/` | 3 DTO classes — Create, Update, Read |
| New folder `Mapping/` | `EmployeeProfile` — AutoMapper configuration |
| `EmployeeManagementAPI.csproj` | Added `AutoMapper` + `AutoMapper.Extensions.Microsoft.DependencyInjection` |
| `Interfaces/IEmployeeService.cs` | All methods now use DTOs instead of `Employee` entity |
| `Services/EmployeeService.cs` | Injects `IMapper`, maps DTOs ↔ entities |
| `Controllers/EmployeesController.cs` | POST accepts `EmployeeCreateDto`, PUT accepts `EmployeeUpdateDto`, GET returns `EmployeeReadDto` |
| `Program.cs` | `builder.Services.AddAutoMapper(...)` added |

---

## Full Project Structure

```
Week3_EmployeeManagementAPI/
│
├── Controllers/
│   └── EmployeesController.cs      ← DTOs in, DTOs out
│
├── DTOs/                           ← NEW
│   ├── EmployeeCreateDto.cs        ← POST input
│   ├── EmployeeUpdateDto.cs        ← PUT input
│   └── EmployeeReadDto.cs          ← GET output (with FullName + DepartmentName)
│
├── Mapping/                        ← NEW
│   └── EmployeeProfile.cs          ← AutoMapper mapping rules
│
├── Interfaces/
│   ├── IEmployeeRepository.cs      ← Unchanged (entity-level)
│   └── IEmployeeService.cs         ← Updated: all methods use DTOs
│
├── Repositories/
│   └── EmployeeRepository.cs       ← Unchanged
│
├── Services/
│   └── EmployeeService.cs          ← Uses IMapper to map DTOs ↔ entities
│
├── Models/
│   ├── Employee.cs                 ← EF Core entity (never leaves service layer)
│   └── Department.cs
│
├── Data/
│   └── AppDbContext.cs             ← Unchanged
│
├── Program.cs                      ← AddAutoMapper added
├── appsettings.json
├── EmployeeManagementAPI.csproj    ← AutoMapper packages added
└── README.md
```

---

## AutoMapper Mappings

| Source | Destination | Where Used |
|--------|-------------|------------|
| `Employee` | `EmployeeReadDto` | GET responses |
| `EmployeeCreateDto` | `Employee` | POST → create |
| `EmployeeUpdateDto` | `Employee` | PUT → update |

Custom rules in `EmployeeProfile`:
- `FullName` ← `$"{FirstName} {LastName}"`
- `DepartmentName` ← `Department.DepartmentName` (null-safe)

---

## Swagger — What You'll See Now

Swagger will show DTO schemas instead of the Employee entity:

**POST /api/employees** — Request schema: `EmployeeCreateDto` (no `EmployeeId` field)
**PUT /api/employees/{id}** — Request schema: `EmployeeUpdateDto` (has `EmployeeId`)
**GET /api/employees** — Response schema: `EmployeeReadDto` (has `FullName`, `DepartmentName`)

---

## How to Run

Open `EmployeeManagementAPI.csproj` in Visual Studio 2022.

**First run after adding AutoMapper packages:**
```powershell
# Restore NuGet packages
dotnet restore
# Or: right-click solution → Restore NuGet Packages
```

DB already exists from Days 1-3 → press **F5**. No new migration needed.

If fresh start:
```powershell
Add-Migration InitialCreate
Update-Database
```

Open `http://localhost:5000` → Swagger UI.

---

## Testing Endpoints

### POST — note: no EmployeeId in body
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

### PUT — EmployeeId must be in body AND must match route
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

### Test validation: POST with missing FirstName
```json
{
  "FirstName": "",
  "LastName": "Test",
  "Email": "test@test.com",
  "Position": "Tester",
  "Salary": 50000,
  "HireDate": "2024-01-01T00:00:00",
  "DepartmentId": 1
}
```
Expected: **400 Bad Request** with `"FirstName": ["First name must be between 1 and 100 characters."]`
