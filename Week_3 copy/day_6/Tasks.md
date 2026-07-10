# Week 3 Day 6 — Tasks

## Task 1 — Install NuGet Packages
- [ ] Open Package Manager Console.
- [ ] Run: `Install-Package Microsoft.AspNetCore.Authentication.JwtBearer -Version 8.0.0`
- [ ] Run: `Install-Package System.IdentityModel.Tokens.Jwt -Version 7.3.1`
- [ ] Verify both packages appear in `EmployeeManagementAPI.csproj`.

## Task 2 — Add JWT Settings to appsettings.json
- [ ] Add `"JwtSettings"` section with `SecretKey`, `Issuer`, `Audience`, `ExpiryMinutes`.
- [ ] Confirm SecretKey is at least 32 characters long.

## Task 3 — Create Authentication Folder
- [ ] Create `Authentication/LoginRequest.cs` with `Username` and `Password`.
- [ ] Add `[Required]` and `[StringLength]` validation to both fields.
- [ ] Create `Authentication/LoginResponse.cs` with `Token`, `TokenType`, `ExpiresAt`, `Username`, `Role`.

## Task 4 — Create JwtService
- [ ] Create `Services/JwtService.cs`.
- [ ] Inject `IConfiguration` and `ILogger<JwtService>`.
- [ ] Read `JwtSettings` from configuration.
- [ ] Build `JwtSecurityToken` with claims: `sub`, `name`, `role`, `jti`, `iat`.
- [ ] Sign with `HmacSha256` and `SymmetricSecurityKey`.
- [ ] Return the token string.
- [ ] Add `GetExpiryTime()` helper method.
- [ ] Register `JwtService` as Singleton in `Program.cs`.

## Task 5 — Create AuthController
- [ ] Create `Controllers/AuthController.cs`.
- [ ] Add `[AllowAnonymous]` to the controller.
- [ ] Implement `POST /api/auth/login`.
- [ ] Validate credentials against the demo user dictionary.
- [ ] Return `401` on invalid credentials wrapped in `ApiResponse`.
- [ ] Return `200` with `LoginResponse` wrapped in `ApiResponse` on success.

## Task 6 — Configure JWT in Program.cs
- [ ] Add `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`.
- [ ] Configure `TokenValidationParameters` (Issuer, Audience, SigningKey, Lifetime).
- [ ] Set `ClockSkew = TimeSpan.Zero`.
- [ ] Add `builder.Services.AddAuthorization()`.
- [ ] Add `app.UseAuthentication()` BEFORE `app.UseAuthorization()`.

## Task 7 — Protect EmployeesController
- [ ] Add `[Authorize]` attribute to `EmployeesController` class.
- [ ] Confirm `AuthController` has `[AllowAnonymous]`.

## Task 8 — Configure Swagger for JWT
- [ ] Add `c.AddSecurityDefinition("Bearer", ...)` in `AddSwaggerGen`.
- [ ] Add `c.AddSecurityRequirement(...)` to apply Bearer globally.
- [ ] Verify 🔓 Authorize button appears in Swagger UI.

## Task 9 — Test Authentication Flow in Swagger
- [ ] POST /api/auth/login with `admin` / `Admin@123` → get token.
- [ ] Click 🔓 Authorize → paste `Bearer <token>` → Authorize.
- [ ] GET /api/employees → 200 OK.
- [ ] DELETE /api/employees/3 → 200 OK.

## Task 10 — Test 401 Scenarios
- [ ] GET /api/employees without token → 401 Unauthorized.
- [ ] GET /api/employees with wrong token → 401 Unauthorized.
- [ ] POST /api/auth/login with wrong password → 401 with error message.
