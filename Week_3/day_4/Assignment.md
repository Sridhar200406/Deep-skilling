# Week 3 Day 4 — Assignment

## Objective
Extend the Day 3 project by introducing DTOs, AutoMapper, and improved model validation.
The Employee entity must never be exposed directly to API clients.

---

## Required New Files

| File | Purpose |
|------|---------|
| `DTOs/EmployeeCreateDto.cs` | Input DTO for POST |
| `DTOs/EmployeeUpdateDto.cs` | Input DTO for PUT |
| `DTOs/EmployeeReadDto.cs` | Output DTO for all GET responses |
| `Mapping/EmployeeProfile.cs` | AutoMapper mapping configuration |

## Modified Files

| File | Change |
|------|--------|
| `Interfaces/IEmployeeService.cs` | All signatures use DTOs |
| `Services/EmployeeService.cs` | Injects IMapper, maps DTOs ↔ entities |
| `Controllers/EmployeesController.cs` | Accepts/returns DTOs only |
| `Program.cs` | Registers AutoMapper |
| `EmployeeManagementAPI.csproj` | Added AutoMapper packages |

---

## Sample POST Request

### POST /api/employees
**Request Body (EmployeeCreateDto):**
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

**Response — 201 Created (EmployeeReadDto):**
```json
{
  "EmployeeId": 4,
  "FirstName": "David",
  "LastName": "Brown",
  "FullName": "David Brown",
  "Email": "david.brown@company.com",
  "Phone": "555-0201",
  "Position": "Marketing Specialist",
  "Salary": 65000.00,
  "HireDate": "2024-01-15T00:00:00",
  "IsActive": true,
  "DepartmentId": 4,
  "DepartmentName": "Marketing"
}
```

---

## Sample PUT Request

### PUT /api/employees/1
**Request Body (EmployeeUpdateDto):**
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

**Response — 200 OK (EmployeeReadDto):**
```json
{
  "EmployeeId": 1,
  "FirstName": "Alice",
  "LastName": "Johnson",
  "FullName": "Alice Johnson",
  "Email": "alice.johnson@company.com",
  "Phone": "555-0199",
  "Position": "Lead Developer",
  "Salary": 105000.00,
  "HireDate": "2020-03-15T00:00:00",
  "IsActive": true,
  "DepartmentId": 1,
  "DepartmentName": "Engineering"
}
```

---

## Sample GET Response

### GET /api/employees/1 → 200 OK
```json
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
```

---

## Validation Error Responses

### 400 — Missing Required Field
```json
{
  "errors": {
    "FirstName": ["First name is required."],
    "Email": ["Email is required."]
  },
  "status": 400,
  "title": "One or more validation errors occurred."
}
```

### 400 — ID Mismatch on PUT
```json
{
  "message": "Route ID (1) does not match body EmployeeId (5)."
}
```

---

## Grading Criteria

| Criterion | Points |
|-----------|--------|
| All 3 DTOs created with correct validation attributes | 20 |
| AutoMapper profile maps all 3 directions correctly | 20 |
| EmployeeService uses IMapper (no manual property copying) | 15 |
| Controller accepts DTOs on POST and PUT | 15 |
| GET responses return EmployeeReadDto with FullName + DepartmentName | 15 |
| Swagger shows DTO schemas (not Employee entity) | 10 |
| 400 returned on validation failure | 5 |
| **Total** | **100** |

---

## Bonus (Optional)
- Add a `DepartmentReadDto` and flatten the nested department in a different way.
- Add `[SwaggerRequestExample]` annotations to show example bodies in Swagger.
- Add a custom validation attribute that rejects `HireDate` in the future.
