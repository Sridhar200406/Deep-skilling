# Week 6 Day 3 – Azure Functions & Serverless Computing

## Core Concepts

---

### What is Serverless Computing?

Traditional hosting: you pay for a server running 24/7, whether it gets 0 or 10,000 requests.

Serverless: you pay only when code runs. Azure manages:
- Provisioning servers
- Scaling (0 to thousands of instances in seconds)
- OS patches and maintenance
- High availability

**You write the function. Azure handles everything else.**

Key characteristics:
- **Event-driven** — runs in response to triggers (HTTP request, file upload, timer, queue message)
- **Stateless** — each invocation is independent; no memory between calls
- **Auto-scaling** — Azure adds instances as load increases, drops to zero when idle
- **Pay-per-execution** — billed per invocation and execution time (first 1M calls/month free)

---

### What is Azure Functions?

Azure Functions is Microsoft's serverless compute platform. You write small, focused functions that:
- Do one thing well
- Run in response to events
- Complete quickly (max 10 min on Consumption plan)
- Connect to other services via bindings

**How it differs from ASP.NET Core Web API:**

| Aspect | ASP.NET Core Web API | Azure Functions |
|--------|---------------------|-----------------|
| Runs | Always-on server | Only when triggered |
| Billing | Per hour (server) | Per invocation |
| Scaling | Manual / App Service plan | Automatic, to zero |
| Cold start | None | Yes (~1-3s first call) |
| Timeout | Unlimited | 10 min (Consumption) |
| Best for | User-facing APIs, real-time | Background jobs, events |
| State | Stateful (DI scope) | Stateless per invocation |
| Deploy unit | Entire API | Individual functions |

**When to use Functions instead of the API:**
- Processing a file after upload (don't block the API response)
- Scheduled tasks (cleanup, reports, data sync)
- Responding to queue messages
- Sending emails or notifications asynchronously
- Tasks that may take > 30 seconds

---

### Azure Functions Isolated Worker Model

This project uses **.NET 8 isolated worker** — the modern approach.

```
HTTP Request
     ↓
Azure Functions Host (manages triggers, scaling, bindings)
     ↓           ← JSON over gRPC
Your Process (dotnet-isolated)
     ↓
Your Function Code (full .NET 8, ASP.NET Core middleware)
```

Benefits over in-process:
- Full .NET 8 support (not limited to .NET 6)
- Your own `Program.cs` with `HostBuilder` — familiar setup
- Full Dependency Injection like ASP.NET Core
- Your process doesn't share memory with the Functions host
- Easier to unit test

---

### The Three Trigger Types Implemented

#### 1. HTTP Trigger — `EmployeeNotificationFunction`

Fires when an HTTP request hits the function endpoint.

```
POST https://<function-app>.azurewebsites.net/api/employee-notification
Headers:
  Content-Type: application/json
  x-functions-key: <your-function-key>

Body:
{
  "employeeId": 1,
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@company.com",
  "position": "Senior Developer",
  "departmentName": "Engineering",
  "eventType": "EmployeeCreated",
  "occurredAt": "2024-01-15T10:30:00Z"
}

Response 200:
{
  "success": true,
  "message": "Notification sent for EmployeeCreated event — Employee 1",
  "data": {
    "employeeId": 1,
    "eventType": "EmployeeCreated",
    "email": "john.doe@company.com",
    "processedAt": "2024-01-15T10:30:01Z"
  },
  "processedAt": "2024-01-15T10:30:01Z",
  "functionName": "EmployeeNotification"
}
```

**Authorization levels:**
- `Anonymous` — anyone can call it (health check endpoint)
- `Function` — requires `x-functions-key` header or `?code=` query param
- `Admin` — requires master key

#### 2. Blob Trigger — `EmployeeBlobProcessingFunction`

Fires automatically when a blob is created/updated in `employee-files` container.

```
Trigger configuration:
  Container: employee-files/{name}
  Connection: AzureBlobStorage:ConnectionString

What it receives:
  - blobStream: the file content as a Stream
  - name: full blob path, e.g. "employees/1/abc123_resume.pdf"

What it does:
  1. Extracts metadata (size, type, employee ID from path)
  2. Validates file type (blocks .exe, .bat, .js, etc.)
  3. Logs structured details to Application Insights
  4. Routes based on type (image → thumbnail, document → index)
  5. Logs upload confirmation
```

**No polling** — Azure Event Grid notifies the function within ~1 second of upload.

#### 3. Timer Trigger — `EmployeeCleanupFunction`

Fires on a schedule defined by a cron expression.

```
Schedule: "0 0 6 * * *"  →  Every day at 06:00 UTC
Format:   {second} {minute} {hour} {day} {month} {dayOfWeek}

Common schedules:
  "0 0 6 * * *"       → Daily at 6 AM UTC
  "0 */30 * * * *"    → Every 30 minutes
  "0 0 0 * * 1"       → Every Monday at midnight
  "0 0 0 1 * *"       → First day of every month

Configurable: stored in "Cleanup:Schedule" app setting
  → Change schedule without redeployment
```

**Three tasks per run:**
1. **Daily Report** — query Azure SQL, generate HTML summary, email admin via SendGrid
2. **Inactive Employee Scan** — find employees with no updates > 90 days
3. **Temp Blob Cleanup** — delete `temp/` blobs older than 24 hours

---

### Function App

A **Function App** is the deployment unit — it hosts one or more functions together.

Think of it like: Function App = App Service Plan + all your function code

**Components:**
- **Function App** — the container (like an App Service)
- **Storage Account** — required, stores function code and state
- **App Service Plan** — defines compute resources
- **Application Insights** — monitoring

**Consumption Plan (Serverless):**
- Scale to zero when idle
- Auto-scale up to 200 instances
- Billed per-execution (first 1M/month free, then ~$0.20 per million)
- Cold start: 1-3 seconds for first request after idle period
- Max execution time: 10 minutes

**Premium Plan:**
- Pre-warmed instances (no cold start)
- Runs on dedicated VMs
- No execution time limit
- More expensive but predictable performance

---

### Managed Identity for Functions

Same pattern as the Employee API (Day 1):

```bash
# Enable Managed Identity on the Function App
az functionapp identity assign \
  --name week6-employee-functions \
  --resource-group Week6-EmployeeManagement-RG

# Get the Principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name week6-employee-functions \
  --resource-group Week6-EmployeeManagement-RG \
  --query principalId -o tsv)

# Grant Key Vault read access
az keyvault set-policy \
  --name week6-emp-kv \
  --object-id "$PRINCIPAL_ID" \
  --secret-permissions get list
```

Then in `local.settings.json` (local dev):
```json
{
  "Values": {
    "AzureKeyVault:VaultUri": ""   ← empty = skip Key Vault locally
  }
}
```

In Azure Portal → Function App → Configuration:
```
AzureKeyVault__VaultUri = https://week6-emp-kv.vault.azure.net/
```

The `DefaultAzureCredential` in `KeyVaultSetup.cs` automatically uses the Managed Identity when running on Azure.

---

## Architecture Integration

```
User
  ↓
Azure App Service (Employee API)
  │
  ├── POST /api/employees → Create employee
  │     └── Fire-and-forget → POST /api/employee-notification (HTTP Trigger)
  │                                    ↓
  │                           Azure Function processes
  │                                    ↓
  │                           Send email via SendGrid
  │
  ├── POST /api/employees/{id}/documents/upload
  │     └── Uploads to Azure Blob Storage
  │                    ↓
  │           Blob Trigger fires automatically
  │                    ↓
  │           EmployeeBlobProcessingFunction runs
  │                    ↓
  │           Log, validate, process file
  │
  └── Every day 06:00 UTC → Timer fires automatically
                    ↓
           EmployeeCleanupFunction runs
                    ↓
           ┌─ Generate daily report → email admin
           ├─ Scan inactive employees → log/notify
           └─ Delete temp blobs → free storage
```

---

## Local Development

### Prerequisites
```bash
# Install Azure Functions Core Tools v4
npm install -g azure-functions-core-tools@4

# Verify
func --version  # Should show 4.x.x

# Start Azurite (blob emulator) — included in docker-compose
docker-compose up azurite
```

### Run Functions locally
```bash
cd src/AzureFunctions

# Start the Functions runtime
func start

# Output:
# Functions:
#   EmployeeNotification: [POST] http://localhost:7071/api/employee-notification
#   EmployeeNotificationHealth: [GET] http://localhost:7071/api/employee-notification/health
#   EmployeeBlobProcessing: blobTrigger
#   EmployeeCleanup: timerTrigger
```

### Test HTTP Trigger
```bash
# Health check (no auth)
curl http://localhost:7071/api/employee-notification/health

# Send notification (EmployeeCreated)
curl -X POST http://localhost:7071/api/employee-notification \
  -H "Content-Type: application/json" \
  -d '{
    "employeeId": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@test.com",
    "position": "Developer",
    "departmentName": "Engineering",
    "eventType": "EmployeeCreated"
  }'

# Expected response:
# { "success": true, "message": "Notification sent for EmployeeCreated event — Employee 1", ... }
```

### Test Blob Trigger
```bash
# Upload a file to Azurite (the blob trigger watches "employee-files")
az storage blob upload \
  --connection-string "UseDevelopmentStorage=true" \
  --container-name "employee-files" \
  --name "employees/1/test-resume.pdf" \
  --file "/path/to/test.pdf"

# The EmployeeBlobProcessing function triggers automatically
# Check the func start terminal for log output
```

### Test Timer Trigger manually
```bash
# Invoke the timer trigger immediately (bypasses the schedule)
curl -X POST http://localhost:7071/admin/functions/EmployeeCleanup \
  -H "Content-Type: application/json" \
  -d '{"input": ""}'
```

### Run via Docker Compose
```bash
# Start full stack including Azure Functions
docker-compose up --build

# Functions available at http://localhost:7071
# API available at http://localhost:5000
```

---

## Deploying to Azure

### Option A: PowerShell script (recommended)
```powershell
# Edit variables at top of script, then run:
.\azure\deploy-functions.ps1
```

### Option B: Manual Azure CLI steps
```bash
# 1. Create Function App on Consumption Plan
az functionapp create \
  --name "week6-employee-functions" \
  --resource-group "Week6-EmployeeManagement-RG" \
  --storage-account "week6employeestorage" \
  --consumption-plan-location "eastus" \
  --runtime "dotnet-isolated" \
  --runtime-version "8" \
  --functions-version "4" \
  --app-insights "Week6-EmployeeManagement-Insights" \
  --os-type Linux

# 2. Enable Managed Identity
az functionapp identity assign \
  --name "week6-employee-functions" \
  --resource-group "Week6-EmployeeManagement-RG"

# 3. Grant Key Vault access
PRINCIPAL_ID=$(az functionapp identity show \
  --name "week6-employee-functions" \
  --resource-group "Week6-EmployeeManagement-RG" \
  --query "principalId" -o tsv)

az keyvault set-policy \
  --name "week6-emp-kv" \
  --object-id "$PRINCIPAL_ID" \
  --secret-permissions get list

# 4. Configure app settings
az functionapp config appsettings set \
  --name "week6-employee-functions" \
  --resource-group "Week6-EmployeeManagement-RG" \
  --settings \
    "AzureKeyVault__VaultUri=https://week6-emp-kv.vault.azure.net/" \
    "AzureBlobStorage__ContainerName=employee-files" \
    "Cleanup__Schedule=0 0 6 * * *" \
    "Cleanup__InactiveDaysThreshold=90" \
    "Report__AdminEmail=admin@company.com"

# 5. Publish
dotnet publish src/AzureFunctions -c Release -o ./fn-publish
cd fn-publish && zip -r ../fn-deploy.zip . && cd ..
az functionapp deployment source config-zip \
  --resource-group "Week6-EmployeeManagement-RG" \
  --name "week6-employee-functions" \
  --src "fn-deploy.zip"
```

### Option C: GitHub Actions (automated)
Push to `week-6-azure-deployment` branch → `.github/workflows/azure-functions-deploy.yml` runs automatically.

---

## Monitoring in Application Insights

### Function-specific queries (KQL)

**All function executions:**
```kql
requests
| where cloud_RoleName == "EmployeeNotification"
   or operation_Name contains "EmployeeCleanup"
   or operation_Name contains "EmployeeBlobProcessing"
| where timestamp > ago(24h)
| project timestamp, operation_Name, success, duration, resultCode
| order by timestamp desc
```

**Failed function executions:**
```kql
exceptions
| where timestamp > ago(24h)
| where cloud_RoleName startswith "Employee"
| project timestamp, type, outerMessage, operation_Name
| order by timestamp desc
```

**Timer function execution history:**
```kql
traces
| where message contains "EmployeeCleanup"
| where timestamp > ago(7d)
| project timestamp, message, severityLevel
| order by timestamp desc
```

**Average HTTP trigger duration:**
```kql
requests
| where operation_Name == "EmployeeNotification"
| where timestamp > ago(1h)
| summarize avg(duration), count() by bin(timestamp, 5m)
| render timechart
```

---

## Troubleshooting

### "Blob trigger not firing"
- Ensure `AzureWebJobsStorage` is set to a valid storage connection string
- For Azurite locally: `UseDevelopmentStorage=true`
- The trigger watches the container name in the `[BlobTrigger(...)]` attribute exactly

### "Timer function ran at wrong time"
- All cron times are UTC
- Check `Cleanup:Schedule` setting in App Configuration
- Test immediately: `func run EmployeeCleanup` (local) or admin endpoint (Azure)

### "HTTP Trigger returns 401"
- Add `x-functions-key: <key>` header for `AuthorizationLevel.Function`
- Get the key: Azure Portal → Function App → Functions → EmployeeNotification → Function Keys

### "Key Vault access denied in Function App"
```bash
# Verify Managed Identity is enabled
az functionapp identity show --name week6-employee-functions --resource-group Week6-EmployeeManagement-RG

# Re-grant access
az keyvault set-policy --name week6-emp-kv --object-id <principalId> --secret-permissions get list
```

### "Cold start latency"
- First request after idle period takes 1-3s to start the process
- Use **Premium Plan** if cold starts are unacceptable for production
- Or add a "warm-up" ping via Azure Monitor
