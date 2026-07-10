# Week 3 Day 3 — Tasks

## Task 1 — Create Interfaces Folder
- [ ] Create `Interfaces/` folder in the project root.
- [ ] Create `Interfaces/IEmployeeRepository.cs`.
- [ ] Create `Interfaces/IEmployeeService.cs` (moved from Services/).

## Task 2 — Define IEmployeeRepository
- [ ] Add `Task<List<Employee>> GetAllAsync()`.
- [ ] Add `Task<Employee?> GetByIdAsync(int id)`.
- [ ] Add `Task AddAsync(Employee employee)`.
- [ ] Add `Task UpdateAsync(Employee employee)`.
- [ ] Add `Task DeleteAsync(Employee employee)`.
- [ ] Add `Task<bool> ExistsAsync(int id)`.
- [ ] Add `Task LoadDepartmentAsync(Employee employee)`.

## Task 3 — Create Repositories Folder
- [ ] Create `Repositories/` folder in the project root.
- [ ] Create `Repositories/EmployeeRepository.cs`.

## Task 4 — Implement EmployeeRepository
- [ ] Inject `AppDbContext` via constructor.
- [ ] Implement `GetAllAsync` using `Include().AsNoTracking().ToListAsync()`.
- [ ] Implement `GetByIdAsync` using `Include().AsNoTracking().FirstOrDefaultAsync()`.
- [ ] Implement `AddAsync` using `AddAsync()` + `SaveChangesAsync()`.
- [ ] Implement `UpdateAsync` using `Update()` + `SaveChangesAsync()`.
- [ ] Implement `DeleteAsync` using `Remove()` + `SaveChangesAsync()`.
- [ ] Implement `ExistsAsync` using `AnyAsync()`.
- [ ] Implement `LoadDepartmentAsync` using `Entry().Reference().LoadAsync()`.

## Task 5 — Refactor EmployeeService
- [ ] Change constructor to inject `IEmployeeRepository` instead of `AppDbContext`.
- [ ] Remove all EF Core using statements from EmployeeService.
- [ ] Replace all `_context.*` calls with `_repository.*` calls.
- [ ] Verify all 5 CRUD methods still work correctly.

## Task 6 — Update IEmployeeService
- [ ] Move `IEmployeeService` to `Interfaces/` folder.
- [ ] Change return type of `GetAllEmployeesAsync` from `IEnumerable<Employee>` to `List<Employee>`.
- [ ] Ensure all method signatures match the updated EmployeeService implementation.

## Task 7 — Update EmployeesController
- [ ] Change `using Week3_EmployeeManagementAPI.Services` to `using Week3_EmployeeManagementAPI.Interfaces`.
- [ ] Verify constructor still injects `IEmployeeService` — no other changes needed.

## Task 8 — Register in Program.cs
- [ ] Add `using Week3_EmployeeManagementAPI.Interfaces`.
- [ ] Add `using Week3_EmployeeManagementAPI.Repositories`.
- [ ] Register: `builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>()`.
- [ ] Keep: `builder.Services.AddScoped<IEmployeeService, EmployeeService>()`.
- [ ] Update Swagger description to "Day 3".

## Task 9 — Build and Verify
- [ ] Build the solution — 0 errors.
- [ ] Run the project — Swagger UI loads at `http://localhost:5000`.
- [ ] Confirm all 5 endpoints are visible in Swagger.

## Task 10 — Test All Endpoints
- [ ] GET /api/employees → 200 + array.
- [ ] GET /api/employees/1 → 200 + object.
- [ ] POST /api/employees → 201 Created.
- [ ] PUT /api/employees/1 → 200 Updated.
- [ ] DELETE /api/employees/3 → 204 No Content.
- [ ] GET /api/employees/999 → 404 Not Found.
