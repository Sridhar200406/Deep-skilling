# Week 3 Day 5 — Tasks

## Task 1 — Create Responses Folder
- [ ] Create `Responses/` folder.
- [ ] Create `Responses/ApiResponse.cs`.
- [ ] Define `ApiResponse<T>` with `Success`, `StatusCode`, `Message`, `Data`, `Errors`.
- [ ] Add static factory methods: `SuccessResponse`, `CreatedResponse`, `NotFoundResponse`, `BadRequestResponse`, `ServerErrorResponse`.
- [ ] Add non-generic `ApiResponse : ApiResponse<object>` with `NoContentResponse`.

## Task 2 — Create Middleware Folder
- [ ] Create `Middleware/` folder.
- [ ] Create `Middleware/ExceptionMiddleware.cs`.
- [ ] Implement `InvokeAsync(HttpContext context)` with try/catch.
- [ ] Map exception types to HTTP status codes using switch expression.
- [ ] Write JSON response using `JsonSerializer`.
- [ ] Log the exception using `ILogger<ExceptionMiddleware>`.
- [ ] Hide stack trace in Production (`IHostEnvironment.IsDevelopment()`).

## Task 3 — Register Middleware in Program.cs
- [ ] Add `using Week3_EmployeeManagementAPI.Middleware`.
- [ ] Add `app.UseMiddleware<ExceptionMiddleware>()` as the FIRST middleware.
- [ ] Update Swagger description to "Day 5".

## Task 4 — Update EmployeesController
- [ ] Add `using Week3_EmployeeManagementAPI.Responses`.
- [ ] Remove all try/catch blocks (ExceptionMiddleware handles unexpected errors).
- [ ] Wrap all `Ok(...)` returns with `ApiResponse<T>.SuccessResponse(...)`.
- [ ] Wrap `CreatedAtAction` with `ApiResponse<T>.CreatedResponse(...)`.
- [ ] Wrap `NotFound(...)` with `ApiResponse<T>.NotFoundResponse(...)`.
- [ ] Wrap `BadRequest(...)` with `ApiResponse<T>.BadRequestResponse(...)`.
- [ ] Update `[ProducesResponseType]` to use `ApiResponse<T>` types.
- [ ] Add `<remarks>` XML comments to each action.

## Task 5 — Add Logging to EmployeeService
- [ ] Inject `ILogger<EmployeeService>` via constructor.
- [ ] Log `LogInformation` at start and end of each method.
- [ ] Log `LogWarning` when an employee is not found.

## Task 6 — Add Logging to EmployeeRepository
- [ ] Inject `ILogger<EmployeeRepository>` via constructor.
- [ ] Log SQL operation names at `LogInformation` level.
- [ ] Log record counts after SELECT operations.
- [ ] Log generated IDs after INSERT.
- [ ] Log `LogWarning` when no record found by ID.

## Task 7 — Update appsettings.json
- [ ] Add `"Week3_EmployeeManagementAPI": "Information"` to `LogLevel`.
- [ ] Add `"Week3_EmployeeManagementAPI": "Debug"` in `appsettings.Development.json`.

## Task 8 — Test Success Responses in Swagger
- [ ] GET /api/employees → confirm `"Success": true`, `"Data": [...]`.
- [ ] GET /api/employees/1 → confirm `"Data"` has `FullName` and `DepartmentName`.
- [ ] POST → confirm `"StatusCode": 201`, `"Data"` has new EmployeeId.
- [ ] PUT /api/employees/1 → confirm `"Data"` has updated fields.

## Task 9 — Test Error Responses in Swagger
- [ ] GET /api/employees/999 → confirm `"Success": false`, `"StatusCode": 404`.
- [ ] PUT with mismatched ID → confirm `"StatusCode": 400`.
- [ ] POST with empty FirstName → confirm `400` with validation errors.

## Task 10 — Verify Logs in Output Window
- [ ] Run in Visual Studio → open Output → ASP.NET Core Web Server.
- [ ] Confirm `EmployeeRepository: GetAllAsync returned X records.` appears.
- [ ] Confirm `EmployeeService: Retrieved X employees.` appears.
- [ ] Confirm controller log `GET /api/employees — returned X employees` appears.
