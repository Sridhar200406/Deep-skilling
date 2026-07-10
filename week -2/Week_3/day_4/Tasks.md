# Week 3 Day 4 — Tasks

## Task 1 — Install AutoMapper Packages
- [ ] Open Package Manager Console in Visual Studio 2022.
- [ ] Run: `Install-Package AutoMapper -Version 13.0.1`
- [ ] Run: `Install-Package AutoMapper.Extensions.Microsoft.DependencyInjection -Version 12.0.1`
- [ ] Verify both appear in `EmployeeManagementAPI.csproj`.

## Task 2 — Create DTOs Folder
- [ ] Create `DTOs/` folder in the project root.
- [ ] Create `DTOs/EmployeeCreateDto.cs`.
- [ ] Create `DTOs/EmployeeUpdateDto.cs`.
- [ ] Create `DTOs/EmployeeReadDto.cs`.

## Task 3 — Define EmployeeCreateDto
- [ ] Include all fields except `EmployeeId` and `Department` navigation.
- [ ] Add `[Required]`, `[StringLength]`, `[EmailAddress]`, `[Range]` annotations.
- [ ] Confirm `EmployeeId` is NOT present.

## Task 4 — Define EmployeeUpdateDto
- [ ] Include `EmployeeId` (needed for route vs body validation).
- [ ] Same validation attributes as `EmployeeCreateDto`.

## Task 5 — Define EmployeeReadDto
- [ ] Include `EmployeeId`, all employee fields.
- [ ] Add `FullName` string property (computed by AutoMapper).
- [ ] Add `DepartmentName` string property (flattened from navigation).
- [ ] No validation attributes (read-only DTO).

## Task 6 — Create Mapping Folder
- [ ] Create `Mapping/` folder.
- [ ] Create `Mapping/EmployeeProfile.cs` extending `AutoMapper.Profile`.
- [ ] Map `Employee → EmployeeReadDto` with custom `FullName` and `DepartmentName`.
- [ ] Map `EmployeeCreateDto → Employee`.
- [ ] Map `EmployeeUpdateDto → Employee`.

## Task 7 — Register AutoMapper in Program.cs
- [ ] Add `using Week3_EmployeeManagementAPI.Mapping;`.
- [ ] Add `builder.Services.AddAutoMapper(typeof(EmployeeProfile).Assembly);`.
- [ ] Update Swagger description to "Day 4".

## Task 8 — Update IEmployeeService
- [ ] Change all method signatures to use DTOs:
  - `GetAllEmployeesAsync()` → `Task<List<EmployeeReadDto>>`
  - `GetEmployeeByIdAsync(int id)` → `Task<EmployeeReadDto?>`
  - `CreateEmployeeAsync(EmployeeCreateDto dto)` → `Task<EmployeeReadDto>`
  - `UpdateEmployeeAsync(int id, EmployeeUpdateDto dto)` → `Task<EmployeeReadDto?>`

## Task 9 — Refactor EmployeeService
- [ ] Inject `IMapper` via constructor alongside `IEmployeeRepository`.
- [ ] Use `_mapper.Map<Employee>(createDto)` in `CreateEmployeeAsync`.
- [ ] Use `_mapper.Map(updateDto, existing)` in `UpdateEmployeeAsync`.
- [ ] Use `_mapper.Map<EmployeeReadDto>(entity)` before returning.

## Task 10 — Update EmployeesController
- [ ] Change `[FromBody] Employee employee` to `[FromBody] EmployeeCreateDto createDto` on POST.
- [ ] Change `[FromBody] Employee employee` to `[FromBody] EmployeeUpdateDto updateDto` on PUT.
- [ ] Update route ID check: `id != updateDto.EmployeeId`.
- [ ] Add `using Week3_EmployeeManagementAPI.DTOs`.
- [ ] Remove `using Week3_EmployeeManagementAPI.Models`.

## Task 11 — Test in Swagger
- [ ] Run the project → confirm Swagger shows DTO schemas (not Employee entity).
- [ ] POST — use `EmployeeCreateDto` body (no EmployeeId field).
- [ ] PUT — use `EmployeeUpdateDto` body (with EmployeeId field).
- [ ] GET — confirm response has `FullName` and `DepartmentName` fields.
- [ ] POST with empty `FirstName` → confirm 400 with validation errors.
