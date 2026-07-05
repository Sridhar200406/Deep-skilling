# Week 3 Day 6 — JWT Authentication and Authorization

## What Changed from Day 5

| Change | Detail |
|--------|--------|
| New `Authentication/LoginRequest.cs` | Login input with validation |
| New `Authentication/LoginResponse.cs` | Token + expiry + role returned to client |
| New `Services/JwtService.cs` | Generates signed HS256 JWT tokens |
| New `Controllers/AuthController.cs` | POST /api/auth/login (public) |
| `Controllers/EmployeesController.cs` | Added `[Authorize]` — all endpoints require token |
| `Program.cs` | JWT auth + Swagger Bearer configured |
| `appsettings.json` | Added `JwtSettings` section |
| `EmployeeManagementAPI.csproj` | Added JWT NuGet packages |

---

## Full Project Structure

```
Week3_EmployeeManagementAPI/
│
├── Authentication/                  ← NEW
│   ├── LoginRequest.cs             ← POST body: username + password
│   └── LoginResponse.cs            ← Response: token + expiry + role
│
├── Controllers/
│   ├── AuthController.cs           ← NEW: POST /api/auth/login [AllowAnonymous]
│   └── EmployeesController.cs      ← Updated: [Authorize] added
│
├── Services/
│   ├── EmployeeService.cs          ← Unchanged
│   └── JwtService.cs               ← NEW: token generation
│
├── Middleware/
│   └── ExceptionMiddleware.cs      ← Unchanged
│
├── Responses/
│   └── ApiResponse.cs              ← Unchanged
│
├── DTOs/  Mapping/  Interfaces/  Repositories/  Models/  Data/
│   └── (all unchanged from Day 5)
│
├── Program.cs                      ← JWT auth + Swagger Bearer added
├── appsettings.json                ← JwtSettings added
└── EmployeeManagementAPI.csproj    ← JWT packages added
```

---

## How to Run

1. Open `EmployeeManagementAPI.csproj` in Visual Studio 2022.
2. Right-click Solution → **Restore NuGet Packages** (JWT packages are new).
3. DB exists from previous days → press **F5**.
4. Browser opens at `http://localhost:5000`.

---

## Step-by-Step: Using JWT in Swagger

### Step 1 — Login
Expand **POST /api/auth/login** → **Try it out** → paste:
```json
{ "Username": "admin", "Password": "Admin@123" }
```
Execute → copy the `Token` value from the response.

### Step 2 — Authorize
Click the 🔓 **Authorize** button (top right of Swagger UI).
In the field enter:
```
Bearer eyJhbGciOiJIUzI1NiIs...
```
Click **Authorize** → **Close**.

### Step 3 — Call Protected Endpoints
Now GET /api/employees, POST, PUT, DELETE all work.
Without this step they all return **401 Unauthorized**.

---

## Demo Credentials

| Username | Password   | Role   |
|----------|------------|--------|
| admin    | Admin@123  | Admin  |
| viewer   | Viewer@123 | Viewer |

---

## JWT Token Decoded (example payload)

```json
{
  "sub":  "admin",
  "name": "admin",
  "role": "Admin",
  "jti":  "a1b2c3d4-...",
  "iat":  1720000000,
  "nbf":  1720000000,
  "exp":  1720003600,
  "iss":  "EmployeeManagementAPI",
  "aud":  "EmployeeManagementAPIClients"
}
```
Paste any JWT at [jwt.io](https://jwt.io) to decode and inspect it.
