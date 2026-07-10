# Week 4 — Employee Management API (Final)

> **ASP.NET Core 8 Web API** with Identity, JWT, Role-Based Auth, Caching, File Upload, Email, Docker, and Unit Tests.

---

## 🏗️ Architecture

```
Week4_EmployeeManagementAPI/
├── Authentication/          # Custom auth handlers (EmailDomainRequirement)
├── Controllers/
│   ├── AuthController.cs    # Register / Login (Identity + JWT)
│   ├── EmployeesController.cs  # Full CRUD + pagination + caching
│   └── FilesController.cs   # Upload / Download / List / Delete files
├── Data/
│   ├── AppDbContext.cs       # IdentityDbContext<ApplicationUser>
│   └── DbInitializer.cs     # Seed roles & users on startup
├── DTOs/                    # Request/Response DTOs
├── Interfaces/              # IEmployeeRepository, IEmployeeService, IEmailService
├── Mapping/                 # AutoMapper profiles
├── Middleware/               # Global exception handler
├── Models/                  # Employee, Department, ApplicationUser
├── Repositories/            # EF Core repository with query params
├── Responses/               # ApiResponse<T> wrapper
├── Services/
│   ├── EmployeeService.cs   # Business logic + In-Memory caching
│   ├── EmailService.cs      # SMTP email service
│   ├── JwtService.cs        # JWT token generation
│   └── FileLoggerProvider.cs # Custom daily-rolling file logger
├── Dockerfile               # Multi-stage Docker build
├── docker-compose.yml       # API + SQL Server orchestration
├── appsettings.json         # Development config
└── appsettings.Production.json # Production config
```

---

## 📅 Week 4 Daily Breakdown

| Day | Topic | Deliverables |
|-----|-------|-------------|
| **1** | ASP.NET Core Identity | Register/Login API, ApplicationUser, password hashing |
| **2** | Advanced Authorization | Role-based + policy-based auth, role seeding |
| **3** | Unit Testing | 35 xUnit tests (Repository, Service, Controller layers) |
| **4** | Caching & Performance | In-Memory cache, response caching, pagination/filtering/sorting |
| **5** | File Upload & Email | FilesController (upload/download/list/delete), SMTP email notifications |
| **6** | Docker | Multi-stage Dockerfile, docker-compose with SQL Server |
| **7** | Deployment | Production settings, file logging, comprehensive documentation |

---

## 🔐 Authentication & Authorization

### Default Users (seeded on startup)

| Username | Password | Role | Email |
|----------|----------|------|-------|
| admin | Admin@123 | Admin | admin@company.com |
| manager | Manager@123 | Manager | manager@company.com |
| employee | Employee@123 | Employee | employee@company.com |

### Authorization Policies

| Policy | Description |
|--------|-------------|
| `AdminOnly` | Admin role required |
| `AdminOrManager` | Admin or Manager role required |
| `AllRoles` | Any authenticated role |
| `CompanyEmailOnly` | Custom policy: email must end with @company.com |

---

## 📁 API Endpoints

### Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/register` | Register new user |
| POST | `/api/v1/auth/login` | Login and get JWT token |

### Employees
| Method | Endpoint | Role(s) | Description |
|--------|----------|---------|-------------|
| GET | `/api/v1/employees` | All | List with pagination, filtering, sorting |
| GET | `/api/v1/employees/{id}` | All | Get by ID |
| POST | `/api/v1/employees` | Admin, Manager | Create employee |
| PUT | `/api/v1/employees/{id}` | Admin, Manager | Update employee |
| DELETE | `/api/v1/employees/{id}` | Admin | Delete employee |

### Files
| Method | Endpoint | Role(s) | Description |
|--------|----------|---------|-------------|
| POST | `/api/v1/files/upload` | Admin, Manager | Upload a file |
| GET | `/api/v1/files/download/{name}` | All | Download a file |
| GET | `/api/v1/files` | Admin, Manager | List uploaded files |
| DELETE | `/api/v1/files/{name}` | Admin | Delete a file |

---

## 🐳 Docker

```bash
# Build and start API + SQL Server
docker-compose up -d --build

# API available at http://localhost:5000
# SQL Server at localhost:1433

# View logs
docker-compose logs -f api

# Stop
docker-compose down
```

---

## 🧪 Unit Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

**35 tests** covering:
- **Repository** (12 tests): CRUD, search, filter, pagination using EF Core InMemory
- **Service** (13 tests): Business logic, caching, cache invalidation using Moq
- **Controller** (10 tests): HTTP status codes, response shapes, edge cases using Moq

---

## ⚙️ Configuration

### appsettings.json (Development)
- LocalDB connection string
- JWT settings with 60-min expiry
- SMTP on localhost (port 25)
- File upload: 5 MB max, common extensions

### appsettings.Production.json
- SQL Server container connection
- JWT with 30-min expiry
- SMTP with SSL enabled
- File logging with daily rotation (30-day retention)

---

## 🚀 Running Locally

```bash
# Restore and run
dotnet restore
dotnet run

# Swagger UI at https://localhost:{port}/
```
