# Week 3 Day 6 — Assignment

## Objective
Secure the Employee Management API using JWT Bearer Authentication.
All employee endpoints must require a valid token.
Only `/api/auth/login` is publicly accessible.

---

## Required New Files

| File | Purpose |
|------|---------|
| `Authentication/LoginRequest.cs` | Login input DTO |
| `Authentication/LoginResponse.cs` | Login output — contains JWT token |
| `Services/JwtService.cs` | Generates and signs JWT tokens |
| `Controllers/AuthController.cs` | POST /api/auth/login endpoint |

## Modified Files

| File | Change |
|------|--------|
| `Controllers/EmployeesController.cs` | Added `[Authorize]` |
| `Program.cs` | JWT auth configured, Swagger Bearer support added |
| `appsettings.json` | Added `JwtSettings` section |
| `EmployeeManagementAPI.csproj` | Added JWT NuGet packages |

---

## Sample Login Request

### POST /api/auth/login
**Request Body:**
```json
{
  "Username": "admin",
  "Password": "Admin@123"
}
```

**Response — 200 OK:**
```json
{
  "Success": true,
  "StatusCode": 200,
  "Message": "Login successful.",
  "Data": {
    "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "TokenType": "Bearer",
    "ExpiresAt": "2026-07-05T11:00:00Z",
    "Username": "admin",
    "Role": "Admin"
  },
  "Errors": null
}
```

**Invalid credentials — 401 Unauthorized:**
```json
{
  "Success": false,
  "StatusCode": 401,
  "Message": "Invalid username or password.",
  "Data": null,
  "Errors": null
}
```

---

## How to Use the Token in Swagger

1. POST /api/auth/login → copy the `Token` value from `Data`.
2. Click the 🔓 **Authorize** button at the top of Swagger UI.
3. In the value field enter: `Bearer eyJhbGci...` (paste your token after "Bearer ").
4. Click **Authorize** → **Close**.
5. All subsequent requests will include the `Authorization: Bearer <token>` header.

---

## Demo Credentials

| Username | Password   | Role   |
|----------|------------|--------|
| admin    | Admin@123  | Admin  |
| viewer   | Viewer@123 | Viewer |

---

## Test Scenarios

| Scenario | Expected Result |
|----------|----------------|
| Login with admin/Admin@123 | 200 + JWT token |
| Login with wrong password | 401 Unauthorized |
| GET /api/employees with valid token | 200 + employee list |
| GET /api/employees without token | 401 Unauthorized |
| GET /api/employees with expired token | 401 Unauthorized |
| GET /api/employees with tampered token | 401 Unauthorized |

---

## Grading Criteria

| Criterion | Points |
|-----------|--------|
| POST /api/auth/login returns JWT token | 20 |
| JWT token contains correct claims (sub, role, exp) | 15 |
| EmployeesController protected with [Authorize] | 15 |
| AuthController has [AllowAnonymous] | 10 |
| Invalid credentials return 401 | 15 |
| Missing/invalid token returns 401 on employee endpoints | 15 |
| Swagger shows 🔓 Authorize button and accepts Bearer token | 10 |
| **Total** | **100** |

---

## Bonus (Optional)
- Add `[Authorize(Roles = "Admin")]` to DELETE endpoint (Viewer cannot delete).
- Add token refresh endpoint: `POST /api/auth/refresh`.
- Store users in the database with hashed passwords using BCrypt.
