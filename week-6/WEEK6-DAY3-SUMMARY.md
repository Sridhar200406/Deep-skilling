# Week 6 Day 3 – Azure Functions & Serverless Computing

## What Was Added

### New Project: `src/AzureFunctions/`

A complete Azure Functions .NET 8 isolated worker project integrated with the existing Employee Management solution.

---

## Files Created (Day 3)

### Azure Functions Project
```
src/AzureFunctions/
├── AzureFunctions.csproj          ← .NET 8 isolated worker, all Azure packages
├── Program.cs                     ← HostBuilder with DI, Key Vault, App Insights
├── host.json                      ← Functions host config (timeout, logging, concurrency)
├── local.settings.json            ← Local dev config (never committed to Git)
├── Dockerfile                     ← Multi-stage Docker build for Functions
│
├── Functions/
│   ├── EmployeeNotificationFunction.cs   ← HTTP Trigger (POST + GET health)
│   ├── EmployeeBlobProcessingFunction.cs ← Blob Trigger (fires on file upload)
│   └── EmployeeCleanupFunction.cs        ← Timer Trigger (daily at 06:00 UTC)
│
├── Services/
│   ├── NotificationService.cs     ← Email via SendGrid (dev: logs only)
│   ├── BlobProcessingService.cs   ← Blob metadata extraction + file management
│   └── EmployeeReportService.cs   ← EF Core queries for reports/cleanup
│
├── Data/
│   └── FunctionDbContext.cs       ← Lightweight EF Core context (read-focused)
│
├── Models/
│   └── EmployeeModels.cs          ← Request/response/result models
│
└── Configuration/
    └── KeyVaultSetup.cs           ← Key Vault integration with DefaultAzureCredential
```

### CI/CD
```
.github/workflows/
└── azure-functions-deploy.yml     ← Separate pipeline: build → deploy Functions
```

### Azure Deployment
```
azure/
└── deploy-functions.ps1           ← Full PowerShell deployment script
```

### Documentation
```
docs/
└── day3-azure-functions.md        ← Concepts, architecture, testing guide, KQL queries
```

### Modified Existing Files
```
src/EmployeeService/
├── Infrastructure/Functions/
│   └── FunctionTriggerService.cs  ← NEW: Calls notification function after create/update
├── Application/Services/
│   └── EmployeeAppService.cs      ← UPDATED: Fire-and-forget function trigger on CRUD
├── Program.cs                     ← UPDATED: Registers FunctionTriggerService + HttpClient
├── appsettings.json               ← UPDATED: Added AzureFunctions section
└── appsettings.Development.json   ← UPDATED: Functions local URL

EmployeeManagement.sln             ← UPDATED: Added AzureFunctions project
docker-compose.yml                 ← UPDATED: Added azure-functions service
```

---

## The Three Functions Explained

### 1. HTTP Trigger — `EmployeeNotificationFunction`

**Route**: `POST /api/employee-notification`  
**Auth**: `Function` key required

Triggered by the Employee API after creating/updating employees.

```
Employee API creates employee
  └─► Fire-and-forget HTTP POST to Functions
              ↓
    Parse + validate request
              ↓
    Send email via SendGrid (or log in dev)
              ↓
    Return 200 with processing result
```

**Test locally:**
```bash
curl -X POST http://localhost:7071/api/employee-notification \
  -H "Content-Type: application/json" \
  -d '{
    "employeeId": 1, "firstName": "John", "lastName": "Doe",
    "email": "john@test.com", "position": "Dev",
    "departmentName": "Engineering", "eventType": "EmployeeCreated"
  }'
```

**Health check (no auth):**
```bash
curl http://localhost:7071/api/employee-notification/health
```

---

### 2. Blob Trigger — `EmployeeBlobProcessingFunction`

**Watches**: `employee-files/{name}` container  
**Fires**: Automatically when the Employee API uploads a file

```
Employee API uploads file to Azure Blob Storage
  └─► Azure detects new blob (within ~1 second)
              ↓
    Function receives blob stream + name
              ↓
    Extract metadata (size, content type, employee ID)
              ↓
    Security check (block .exe, .bat, .js, etc.)
              ↓
    Route: image → log for thumbnail, document → log for indexing
              ↓
    Log details to Application Insights
```

**Test locally** (Azurite must be running):
```bash
# Upload a file to trigger it
az storage blob upload \
  --connection-string "UseDevelopmentStorage=true" \
  --container-name "employee-files" \
  --name "employees/1/test-document.pdf" \
  --file "./test.pdf"
```

---

### 3. Timer Trigger — `EmployeeCleanupFunction`

**Schedule**: `"0 0 6 * * *"` = every day at 06:00 UTC (configurable)  
**Fires**: Automatically on schedule

Three tasks per execution:

| Task | What it does |
|------|-------------|
| Daily Report | Queries SQL → employee/department stats → emails admin |
| Inactive Scan | Finds employees with no updates > 90 days |
| Temp Cleanup | Deletes `temp/` blobs older than 24 hours |

**Test manually (trigger immediately):**
```bash
# Local
curl -X POST http://localhost:7071/admin/functions/EmployeeCleanup \
  -H "Content-Type: application/json" -d '{"input":""}'

# Azure
az rest --method post \
  --url "https://week6-employee-functions.azurewebsites.net/admin/functions/EmployeeCleanup" \
  --headers "x-functions-key=<master-key>" \
  --body '{"input":""}'
```

---

## API → Functions Integration

When the Employee API creates or updates an employee, it now triggers the notification function:

```csharp
// EmployeeAppService.cs — CreateAsync
var dto = MapToDto(employee);

// Fire-and-forget: non-blocking, never fails the API
if (_functionTrigger != null)
{
    _ = _functionTrigger.TriggerEmployeeNotificationAsync(
        employee.Id, employee.FirstName, employee.LastName,
        employee.Email, employee.Position,
        dto.DepartmentName, "EmployeeCreated");
}
return dto;
```

`FunctionTriggerService` handles:
- Reading the function URL from `AzureFunctions:NotificationUrl` config
- Adding the function key if configured
- Swallowing exceptions — function failure never breaks the API response
- Logging warnings if the trigger fails

**Local dev config** (`appsettings.Development.json`):
```json
"AzureFunctions": {
  "NotificationUrl": "http://localhost:7071/api/employee-notification",
  "FunctionKey": ""
}
```

**Production** (loaded from Key Vault):
```
AzureFunctions--NotificationUrl = https://week6-employee-functions.azurewebsites.net/api/employee-notification
AzureFunctions--FunctionKey = <function-key>
```

---

## Concepts Summary

| Concept | Explanation |
|---------|-------------|
| **Serverless** | Pay only when code runs; Azure scales automatically to zero |
| **Azure Functions** | Serverless compute for event-driven, background tasks |
| **HTTP Trigger** | Function invoked by an HTTP request (like a controller action) |
| **Blob Trigger** | Function invoked when a file is created/modified in Blob Storage |
| **Timer Trigger** | Function invoked on a cron schedule |
| **Function App** | Container that hosts one or more functions together |
| **Consumption Plan** | Serverless plan — scale to zero, 1M free calls/month |
| **Isolated Worker** | .NET 8 model — own process, full .NET 8, recommended for new projects |
| **Managed Identity** | Azure-managed credentials — no passwords stored anywhere |
| **Cold Start** | Delay when function wakes from idle (1-3s on Consumption plan) |

---

## Architecture Diagram

```
                    User
                      │
                      ▼
            Azure App Service
          (Employee Management API)
                      │
          ┌───────────┼────────────┐
          │           │            │
          ▼           ▼            ▼
   Azure SQL DB  Azure Blob    Azure Functions
                  Storage      │
                     │         ├── HTTP Trigger
                     │         │   POST /api/employee-notification
                     │         │   └─ Send email notifications
                     │         │
                     └────────►├── Blob Trigger
                               │   employee-files/{name}
                               │   └─ Process uploaded files
                               │
                               └── Timer Trigger
                                   Daily 06:00 UTC
                                   └─ Report + Cleanup
                                         │
                        ┌────────────────┤
                        │                │
                        ▼                ▼
                 Azure Key Vault   Application Insights
               (secrets loading)   (monitoring + logs)
```

---

## Running Everything Locally

```bash
# Terminal 1: Start infrastructure
docker-compose up sql-server redis rabbitmq azurite

# Terminal 2: Start Employee API
cd src/EmployeeService
dotnet run
# → http://localhost:5000 (Swagger at root)

# Terminal 3: Start Azure Functions
cd src/AzureFunctions
func start
# → http://localhost:7071

# Terminal 4: Test the integration
# Create an employee via the API — it triggers the notification function automatically
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
# Copy the token, then:
curl -X POST http://localhost:5000/api/employees \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Jane","lastName":"Smith","email":"jane@test.com","position":"Engineer","salary":75000,"departmentId":1}'
# → API creates employee, then calls Azure Function in the background
```

---

## Project Statistics (Day 3)

| Metric | Count |
|--------|-------|
| New files | 14 |
| Modified files | 6 |
| Azure Functions | 3 (HTTP, Blob, Timer) |
| Services | 3 (Notification, BlobProcessing, EmployeeReport) |
| Lines of code added | ~1,400 |
| CI/CD pipelines total | 2 (API + Functions) |
| Azure services used | 6 (App Service, SQL, Blob, Key Vault, App Insights, Functions) |

---

## Deliverables Completed

| Requirement | Status | Location |
|-------------|--------|----------|
| Azure Functions .NET 8 isolated worker project | ✅ | `src/AzureFunctions/` |
| HTTP Trigger function | ✅ | `Functions/EmployeeNotificationFunction.cs` |
| Blob Trigger function | ✅ | `Functions/EmployeeBlobProcessingFunction.cs` |
| Timer Trigger function | ✅ | `Functions/EmployeeCleanupFunction.cs` |
| Dependency Injection | ✅ | `Program.cs` |
| Azure SQL via EF Core | ✅ | `Data/FunctionDbContext.cs` + `EmployeeReportService.cs` |
| Azure Key Vault integration | ✅ | `Configuration/KeyVaultSetup.cs` |
| Application Insights | ✅ | `Program.cs` + `host.json` |
| Blob Storage integration | ✅ | `Services/BlobProcessingService.cs` |
| API → Functions trigger | ✅ | `Infrastructure/Functions/FunctionTriggerService.cs` |
| Docker support | ✅ | `Dockerfile` + `docker-compose.yml` |
| CI/CD pipeline | ✅ | `.github/workflows/azure-functions-deploy.yml` |
| Azure deployment script | ✅ | `azure/deploy-functions.ps1` |
| Documentation | ✅ | `docs/day3-azure-functions.md` |
| local.settings.json | ✅ | `local.settings.json` |
| Serverless concepts explained | ✅ | This doc + `docs/day3-azure-functions.md` |
