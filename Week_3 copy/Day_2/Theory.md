# Week 3 Day 2 — Theory: Full CRUD REST API with ASP.NET Core

## 1. CRUD ↔ HTTP Verb Mapping

| Operation | HTTP Verb | Route | Success Code |
|-----------|-----------|-------|-------------|
| Read All  | GET    | `/api/employees`     | 200 OK |
| Read One  | GET    | `/api/employees/{id}`| 200 OK |
| Create    | POST   | `/api/employees`     | 201 Created |
| Update    | PUT    | `/api/employees/{id}`| 200 OK |
| Delete    | DELETE | `/api/employees/{id}`| 204 No Content |

---

## 2. HTTP Status Codes — When to Use Each

| Code | Name | When |
|------|------|------|
| 200 | OK | Successful GET or PUT |
| 201 | Created | Successful POST — resource was created |
| 204 | No Content | Successful DELETE — nothing to return |
| 400 | Bad Request | Invalid input, validation failure, mismatched IDs |
| 404 | Not Found | Resource does not exist |
| 500 | Internal Server Error | Unhandled exception |

---

## 3. IActionResult Helper Methods

```csharp
return Ok(data);                        // 200 + body
return CreatedAtAction("GetById",       // 201 + Location header + body
    new { id = obj.Id }, obj);
return NoContent();                     // 204 — no body
return BadRequest(new { message });     // 400 + body
return NotFound(new { message });       // 404 + body
return StatusCode(500, new { message });// 500 + body
```

---

## 4. POST — Creating a Resource

- Client sends JSON body **without** the primary key (DB auto-generates it).
- Server responds with **201 Created** + a `Location` header pointing to the new resource.
- `CreatedAtAction` generates the Location header automatically.

```csharp
return CreatedAtAction(nameof(GetEmployeeById), new { id = created.EmployeeId }, created);
// Response header: Location: /api/employees/4
```

---

## 5. PUT — Full Replacement Update

- Client sends the **complete** resource in the body, including the ID.
- Route ID must match body ID — mismatch → 400 Bad Request.
- Server loads the existing record, applies all fields, saves.
- Never allow the client to change the primary key.

```csharp
if (id != employee.EmployeeId)
    return BadRequest(...);
```

---

## 6. DELETE — Remove a Resource

- No request body needed.
- If found: delete and return **204 No Content** (empty body — nothing to return).
- If not found: return **404 Not Found**.

---

## 7. Model Validation with [ApiController]

`[ApiController]` automatically validates the incoming model against data annotations
and returns a **400 Bad Request** with validation details if invalid.
No manual `ModelState.IsValid` check needed.

```csharp
// These annotations on Employee drive both DB schema and API validation:
[Required(ErrorMessage = "First name is required.")]
[MaxLength(100)]
public string FirstName { get; set; }

[Range(0, 9999999.99)]
public decimal Salary { get; set; }
```

---

## 8. Service Layer — CRUD Pattern

```
POST /api/employees
    Controller.CreateEmployee(employee)
        → EmployeeService.CreateEmployeeAsync(employee)
            → employee.EmployeeId = 0      // let DB assign
            → _context.Employees.Add(employee)
            → _context.SaveChangesAsync()
            → reload Department via Entry().Reference().LoadAsync()
            → return created employee
        ← Controller returns 201 Created

PUT /api/employees/{id}
    Controller.UpdateEmployee(id, employee)
        → EmployeeService.UpdateEmployeeAsync(id, employee)
            → load existing from DB
            → apply field-by-field updates
            → _context.SaveChangesAsync()
            → return updated employee (or null if not found)
        ← Controller returns 200 OK or 404

DELETE /api/employees/{id}
    Controller.DeleteEmployee(id)
        → EmployeeService.DeleteEmployeeAsync(id)
            → load existing from DB
            → _context.Employees.Remove(existing)
            → _context.SaveChangesAsync()
            → return true (or false if not found)
        ← Controller returns 204 or 404
```

---

## 9. Why AsNoTracking() is NOT used for Write Operations

- `AsNoTracking()` skips EF Core's change tracker — fine for reads.
- For `Update` and `Delete`, we need EF Core to **track** the entity so it knows
  what changed and can generate the correct SQL `UPDATE` / `DELETE`.
- Rule: use `AsNoTracking()` only on read-only queries.

---

## 10. Swagger — All 5 Endpoints

After adding CRUD, Swagger UI shows:

```
GET    /api/employees          → Try it → Execute → 200 array
GET    /api/employees/{id}     → Try it → id=1    → 200 object
POST   /api/employees          → Try it → paste JSON body → 201
PUT    /api/employees/{id}     → Try it → id=1 + JSON body → 200
DELETE /api/employees/{id}     → Try it → id=1    → 204
```
