# Week 3 Day 6 — Theory: JWT Authentication and Authorization

## 1. Authentication vs Authorization

| Concept | Question | Example |
|---------|----------|---------|
| **Authentication** | Who are you? | Login with username/password → get a token |
| **Authorization** | What can you do? | Token has role "Admin" → allowed to DELETE |

Authentication must happen BEFORE authorization.
In `Program.cs`: `app.UseAuthentication()` → `app.UseAuthorization()`

---

## 2. What is JWT?

JWT (JSON Web Token) is a compact, self-contained token for securely transmitting claims between parties.
It is digitally signed — the server can verify it without a database lookup.

### Token Structure

A JWT has three parts separated by dots:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
.
eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJBZG1pbiIsImV4cCI6MTcyMDAwMDAwMH0
.
HMACSHA256(base64Header + "." + base64Payload, secretKey)
```

| Part | Content | Example |
|------|---------|---------|
| **Header** | Algorithm + type | `{ "alg": "HS256", "typ": "JWT" }` |
| **Payload** | Claims (user data) | `{ "sub": "admin", "role": "Admin", "exp": 1720000000 }` |
| **Signature** | HMAC of header+payload | Prevents tampering |

### Standard Claims in Payload

| Claim | Name | Purpose |
|-------|------|---------|
| `sub` | Subject | Username / user ID |
| `name` | Name | Display name |
| `role` | Role | Authorization role |
| `jti` | JWT ID | Unique token ID (prevents replay) |
| `iat` | Issued At | When token was created |
| `exp` | Expiration | When token expires |
| `iss` | Issuer | Who issued the token |
| `aud` | Audience | Who the token is for |

---

## 3. JWT Flow

```
1. Client  →  POST /api/auth/login  { username, password }
2. Server  →  validates credentials
3. Server  →  generates JWT token (signed with SecretKey)
4. Server  →  returns { Token, ExpiresAt, Role }
5. Client  →  stores token (localStorage / memory)
6. Client  →  GET /api/employees
              Authorization: Bearer eyJhbGci...
7. Server  →  JwtBearer middleware validates token
              - signature valid? ✓
              - not expired? ✓
              - issuer/audience match? ✓
8. Server  →  runs the controller action
9. Server  →  returns data
```

---

## 4. Configuration in appsettings.json

```json
"JwtSettings": {
  "SecretKey": "EmployeeManagementAPI_SuperSecretKey_Week3Day6_2026!@#",
  "Issuer":    "EmployeeManagementAPI",
  "Audience":  "EmployeeManagementAPIClients",
  "ExpiryMinutes": "60"
}
```

- **SecretKey** — must be at least 32 characters for HS256. Keep this secret!
- **Issuer** — identifies who issued the token (your API).
- **Audience** — identifies who the token is for (your clients).
- **ExpiryMinutes** — how long the token is valid.

---

## 5. Program.cs — JWT Registration

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,   // check iss claim
            ValidateAudience         = true,   // check aud claim
            ValidateLifetime         = true,   // reject expired tokens
            ValidateIssuerSigningKey = true,   // verify signature
            ValidIssuer              = "EmployeeManagementAPI",
            ValidAudience            = "EmployeeManagementAPIClients",
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew                = TimeSpan.Zero  // no grace period on expiry
        };
    });

// Order matters!
app.UseAuthentication();  // validate the token
app.UseAuthorization();   // enforce [Authorize]
```

---

## 6. Protecting Endpoints

```csharp
// Require valid token for all actions in this controller
[Authorize]
public class EmployeesController : ControllerBase { ... }

// Public endpoint — no token required
[AllowAnonymous]
public class AuthController : ControllerBase { ... }

// Role-based — only Admins can delete
[Authorize(Roles = "Admin")]
public IActionResult DeleteEmployee(int id) { ... }
```

---

## 7. Swagger JWT Support

Without configuration, Swagger doesn't send the Authorization header.
After adding `AddSecurityDefinition` and `AddSecurityRequirement`:

1. A 🔓 **Authorize** button appears in Swagger UI.
2. Click it → enter `Bearer eyJhbGci...` (your token).
3. Swagger sends `Authorization: Bearer <token>` on every request.

---

## 8. Demo Users (hardcoded for learning)

| Username | Password   | Role   |
|----------|------------|--------|
| admin    | Admin@123  | Admin  |
| viewer   | Viewer@123 | Viewer |

In production, credentials come from a database with hashed passwords (BCrypt/Argon2).

---

## 9. What Happens Without a Token

```
GET /api/employees  (no Authorization header)
→ 401 Unauthorized  (JwtBearer middleware rejects before controller runs)

GET /api/employees  (expired token)
→ 401 Unauthorized

GET /api/employees  (tampered token)
→ 401 Unauthorized  (signature validation fails)

GET /api/employees  (valid token)
→ 200 OK  (controller runs normally)
```
