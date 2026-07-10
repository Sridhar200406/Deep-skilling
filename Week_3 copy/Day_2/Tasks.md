# Week 3 Day 2 — Tasks

## Task 1 — Extend IEmployeeService
- [ ] Add `CreateEmployeeAsync(Employee employee)` returning `Task<Employee>`.
- [ ] Add `UpdateEmployeeAsync(int id, Employee employee)` returning `Task<Employee?>`.
- [ ] Add `DeleteEmployeeAsync(int id)` returning `Task<bool>`.
- [ ] Add `EmployeeExistsAsync(int id)` returning `Task<bool>`.

## Task 2 — Implement EmployeeService
- [ ] Implement `CreateEmployeeAsync`:
  - Set `EmployeeId = 0` before adding.
  - Call `_context.Employees.Add(employee)`.
  - Call `SaveChangesAsync()`.
  - Reload the Department navigation property.
- [ ] Implement `UpdateEmployeeAsync`:
  - Load existing employee (no `AsNoTracking`).
  - Apply field-by-field updates.
  - Call `SaveChangesAsync()`.
  - Reload Department and return.
- [ ] Implement `DeleteEmployeeAsync`:
  - Load employee, call `Remove()`, call `SaveChangesAsync()`.
  - Return `true` if deleted, `false` if not found.
- [ ] Implement `EmployeeExistsAsync` using `AnyAsync`.

## Task 3 — Extend EmployeesController
- [ ] Add `POST /api/employees`:
  - Accept `[FromBody] Employee`.
  - Call `CreateEmployeeAsync`.
  - Return `CreatedAtAction(nameof(GetEmployeeById), new { id }, created)`.
- [ ] Add `PUT /api/employees/{id}`:
  - Validate `id == employee.EmployeeId` → 400 if mismatch.
  - Call `UpdateEmployeeAsync`.
  - Return `Ok(updated)` or `NotFound`.
- [ ] Add `DELETE /api/employees/{id}`:
  - Call `DeleteEmployeeAsync`.
  - Return `NoContent()` or `NotFound`.

## Task 4 — Update Employee.cs Validation
- [ ] Add `ErrorMessage` to `[Required]` attributes.
- [ ] Add `[Range(0, 9999999.99)]` to `Salary`.
- [ ] Add `[Range(1, int.MaxValue)]` to `DepartmentId`.

## Task 5 — Update .csproj
- [ ] Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.
- [ ] Add `<NoWarn>$(NoWarn);1591</NoWarn>` to suppress missing XML warnings.

## Task 6 — Test POST in Swagger
- [ ] Open Swagger UI at `http://localhost:5000`.
- [ ] Expand POST /api/employees → Try it out.
- [ ] Paste the sample JSON body (see Assignment.md).
- [ ] Click Execute → expect 201 Created response.

## Task 7 — Test PUT in Swagger
- [ ] Expand PUT /api/employees/{id} → Try it out.
- [ ] Enter id = 1, paste updated JSON body.
- [ ] Click Execute → expect 200 OK with updated data.

## Task 8 — Test DELETE in Swagger
- [ ] Expand DELETE /api/employees/{id} → Try it out.
- [ ] Enter id = 3.
- [ ] Click Execute → expect 204 No Content.
- [ ] Verify by calling GET /api/employees — employee 3 should be gone.

## Task 9 — Test 404 Handling
- [ ] GET /api/employees/999 → expect 404 with `{"message":"..."}`.
- [ ] PUT /api/employees/999 → expect 404.
- [ ] DELETE /api/employees/999 → expect 404.

## Task 10 — Test 400 Handling
- [ ] POST with empty `FirstName` → expect 400 with validation errors.
- [ ] PUT with mismatched route ID and body EmployeeId → expect 400.
