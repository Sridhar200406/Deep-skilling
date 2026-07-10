# Week 3 Day 2 — Assignment

## Objective
Extend the Day 1 ASP.NET Core Web API with full CRUD operations:
POST, PUT, and DELETE endpoints alongside the existing GET endpoints.

---

## Required Endpoints

| Method | Route | Body | Success Response |
|--------|-------|------|-----------------|
| GET    | `/api/employees`      | None | 200 + array |
| GET    | `/api/employees/{id}` | None | 200 + object |
| POST   | `/api/employees`      | Employee JSON | 201 + created object |
| PUT    | `/api/employees/{id}` | Employee JSON | 200 + updated object |
| DELETE | `/api/employees/{id}` | None | 204 No Content |

---

## Sample POST Request

### POST /api/employees

**Request Body:**
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

**Response — 201 Created:**
```json
{
  "EmployeeId": 4,
  "FirstName": "David",
  "LastName": "Brown",
  "Email": "david.brown@company.com",
  "Phone": "555-0201",
  "Position": "Marketing Specialist",
  "Salary": 65000.00,
  "HireDate": "2024-01-15T00:00:00",
  "IsActive": true,
  "DepartmentId": 4,
  "Department": {
    "DepartmentId": 4,
    "DepartmentName": "Marketing",
    "Description": "Brand and product promotion"
  }
}
```

**Response Header:**
```
Location: /api/employees/4
```

---

## Sample PUT Request

### PUT /api/employees/1

**Request Body:**
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

**Response — 200 OK:**
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
  "DepartmentId": 1,
  "Department": {
    "DepartmentId": 1,
    "DepartmentName": "Engineering",
    "Description": "Software development and architecture"
  }
}
```

---

## Sample DELETE Request

### DELETE /api/employees/3

**Response — 204 No Content** (empty body)

---

## Error Responses

### 400 Bad Request — Missing Required Field (POST)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "FirstName": ["First name is required."]
  }
}
```

### 400 Bad Request — ID Mismatch (PUT)
```json
{
  "message": "Route ID (1) does not match body EmployeeId (5)."
}
```

### 404 Not Found
```json
{
  "message": "Employee with ID 999 was not found."
}
```

---

## Expected Swagger Output

Swagger UI at `http://localhost:5000` should display 5 endpoints:

```
GET    /api/employees          Get all employees
GET    /api/employees/{id}     Get a specific employee by ID
POST   /api/employees          Create a new employee
PUT    /api/employees/{id}     Update an existing employee
DELETE /api/employees/{id}     Delete an employee by ID
```

---

## Grading Criteria

| Criterion | Points |
|-----------|--------|
| POST creates employee with 201 + Location header | 20 |
| PUT updates employee with 200 | 20 |
| DELETE removes employee with 204 | 15 |
| 404 returned for missing employee on GET/PUT/DELETE | 15 |
| 400 returned for invalid model on POST/PUT | 15 |
| All 5 endpoints visible in Swagger | 10 |
| Service layer handles all CRUD (no DbContext in controller) | 5 |
| **Total** | **100** |

---

## Bonus (Optional)
- Add `PATCH /api/employees/{id}/deactivate` that sets `IsActive = false`.
- Add input validation that rejects duplicate email addresses on POST.
- Add `GET /api/employees?isActive=true` filter query parameter.
