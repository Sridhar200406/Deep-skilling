# Azure Integration Complete Guide

## Overview

This guide covers all Azure services integrated in Week 6 Day 1-2 and how they work together.

---

## 1. Azure SQL Database Integration

### Connection String Storage

**Development** (appsettings.Development.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EmployeeManagementDb;Trusted_Connection=True"
  }
}
```

**Production** (loaded from Key Vault):
```
Key Vault Secret Name: ConnectionStrings--DefaultConnection
Value: Server=tcp:week6-sql-server.database.windows.net,1433;Database=EmployeeManagementDb;User ID=sqladmin;Password=...;Encrypt=True
```

### Running Migrations

**Local development**:
```bash
cd src/EmployeeService
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

**Azure SQL (production)**:
```bash
# Get the connection string from Key Vault
az keyvault secret show --vault-name week6-emp-kv \
  --name ConnectionStrings--DefaultConnection --query value -o tsv

# Run migrations
dotnet ef database update --project src/EmployeeService \
  --connection "<connection-string>"
```

**In CI/CD** (automatic):
```yaml
- name: Run EF Core migrations
  run: |
    dotnet ef database update \
      --project ${{ env.PROJECT_PATH }} \
      --connection "${{ secrets.SQL_CONNECTION_STRING }}"
```

### Connection Resiliency

The app uses EF Core retry-on-failure for transient Azure SQL errors:

```csharp
options.UseSqlServer(connectionString, sqlOptions =>
{
    sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null);
    sqlOptions.CommandTimeout(60);
});
```

**What this handles**:
- Transient network failures
- Azure SQL scaling operations
- Connection pool exhaustion
- Deadlocks (SQL error 1205)

---

## 2. Azure Blob Storage Integration

### Service Implementation

`BlobStorageService` provides three operations:

**Upload**:
```csharp
POST /api/employees/{employeeId}/documents/upload
Content-Type: multipart/form-data

// Returns:
{
  "success": true,
  "data": {
    "id": 1,
    "fileName": "employees/1/abc123_resume.pdf",
    "blobUrl": "https://week6storage.blob.core.windows.net/employee-files/employees/1/abc123_resume.pdf",
    "fileSize": 245678
  }
}
```

**Download**:
```csharp
GET /api/employees/{employeeId}/documents/{documentId}/download

// Returns binary stream with proper Content-Type header
```

**Delete**:
```csharp
DELETE /api/employees/{employeeId}/documents/{documentId}

// Soft-deletes the DB record and hard-deletes the blob
```

### Security Features

1. **Content type validation** — only allows:
   - Images: `image/jpeg`, `image/png`, `image/gif`, `image/webp`
   - Documents: `application/pdf`, Word, Excel, `text/plain`

2. **File size limit** — 10 MB max (configurable via `RequestSizeLimit` attribute)

3. **Path traversal prevention** — sanitizes filenames:
```csharp
var safeName = Path.GetFileNameWithoutExtension(fileName)
    .Replace("..", "")
    .Replace("/", "")
    .Replace("\\", "");
```

4. **Unique blob names** — prevents overwrites:
```csharp
var blobName = $"employees/{employeeId}/{Guid.NewGuid():N}_{safeName}{extension}";
```

5. **Private container** — no public access, files are only accessible via authenticated API calls

### Connection String Storage

**Development** (Azurite emulator):
```json
{
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "employee-files"
  }
}
```

**Production** (from Key Vault):
```
Secret Name: AzureBlobStorage--ConnectionString
Value: DefaultEndpointsProtocol=https;AccountName=week6storage;AccountKey=...;EndpointSuffix=core.windows.net
```

### Testing with Azurite

Local blob storage emulator included in docker-compose.yml:

```bash
docker-compose up azurite

# Access via Azure Storage Explorer:
# Account Name: devstoreaccount1
# Account Key: Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==
# Blob Endpoint: http://localhost:10000/devstoreaccount1
```

---

## 3. Azure Key Vault Integration

### How It Works

```
App Startup
    ↓
Program.cs calls AddAzureKeyVaultIfConfigured()
    ↓
Checks if AzureKeyVault:VaultUri is set
    ↓
Yes → DefaultAzureCredential gets token:
    1. Try Managed Identity (on Azure App Service)
    2. Try Environment Variables (AZURE_CLIENT_ID, etc.)
    3. Try Azure CLI (az login)
    4. Try Visual Studio credential
    ↓
Connects to Key Vault with that credential
    ↓
Downloads all secrets and adds to IConfiguration
    ↓
Secrets override appsettings.json values
    ↓
App continues startup with secrets loaded
```

### Secret Naming Convention

ASP.NET Core config uses colons `:` for hierarchy. Key Vault doesn't allow colons in secret names, so use double-dash `--`:

| Config Key (app code) | Key Vault Secret Name |
|-----------------------|----------------------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings--DefaultConnection` |
| `JwtSettings:SecretKey` | `JwtSettings--SecretKey` |
| `AzureBlobStorage:ConnectionString` | `AzureBlobStorage--ConnectionString` |

### Creating Secrets

```bash
# Store the SQL connection string
az keyvault secret set \
  --vault-name week6-emp-kv \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Server=tcp:week6-sql-server.database.windows.net,1433;..."

# Store JWT secret
az keyvault secret set \
  --vault-name week6-emp-kv \
  --name "JwtSettings--SecretKey" \
  --value "Production-Super-Secret-Key-Min-32-Chars!!"

# Store Blob Storage connection
az keyvault secret set \
  --vault-name week6-emp-kv \
  --name "AzureBlobStorage--ConnectionString" \
  --value "DefaultEndpointsProtocol=https;AccountName=..."

# Store Application Insights connection
az keyvault secret set \
  --vault-name week6-emp-kv \
  --name "ApplicationInsights--ConnectionString" \
  --value "InstrumentationKey=...;IngestionEndpoint=..."
```

### Reading Secrets in Code

No code changes needed — secrets are automatically in `IConfiguration`:

```csharp
// These values come from Key Vault in production, appsettings in dev
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
var blobConnection = builder.Configuration["AzureBlobStorage:ConnectionString"];
```

### Access Control

Grant the App Service's Managed Identity read access:

```bash
# Get the App Service's identity
PRINCIPAL_ID=$(az webapp identity show \
  --name week6-employee-api \
  --resource-group Week6-EmployeeManagement-RG \
  --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name week6-emp-kv \
  --object-id "$PRINCIPAL_ID" \
  --secret-permissions get list
```

---

## 4. Application Insights Integration

### What Gets Tracked Automatically

Once configured, Application Insights tracks:
- **HTTP requests**: URL, status code, duration
- **Dependencies**: SQL queries, HTTP calls, Redis operations
- **Exceptions**: full stack traces
- **Performance counters**: CPU, memory
- **Custom events**: via `TelemetryClient.TrackEvent()`

### Configuration

**appsettings.json** (empty in source control):
```json
{
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

**Azure App Service Configuration** (set via portal or CLI):
```bash
az webapp config appsettings set \
  --name week6-employee-api \
  --resource-group Week6-EmployeeManagement-RG \
  --settings "ApplicationInsights__ConnectionString=InstrumentationKey=...;"
```

Or **Key Vault** (recommended):
```bash
az keyvault secret set \
  --vault-name week6-emp-kv \
  --name "ApplicationInsights--ConnectionString" \
  --value "InstrumentationKey=...;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;..."
```

### Custom Enrichment

The app adds custom properties to every request:

**AppInsightsTelemetryInitializer.cs**:
```csharp
telemetry.Context.GlobalProperties["ServiceName"] = "EmployeeService";
telemetry.Context.GlobalProperties["Environment"] = env.EnvironmentName;
telemetry.Context.GlobalProperties["ApiVersion"] = "v1";
telemetry.Context.GlobalProperties["MachineName"] = Environment.MachineName;
```

**RequestTelemetryMiddleware.cs** (per-request):
```csharp
requestTelemetry.Context.User.AuthenticatedUserId = username;
requestTelemetry.Properties["AuthenticatedUser"] = username;
requestTelemetry.Properties["UserRole"] = role;
requestTelemetry.Properties["CorrelationId"] = correlationId;
requestTelemetry.Properties["SlowRequest"] = "true";  // if > 1s
```

### Querying Logs

**Azure Portal → Application Insights → Logs (KQL)**

Find failed requests:
```kql
requests
| where success == false
| where timestamp > ago(24h)
| project timestamp, name, url, resultCode, duration
| order by timestamp desc
```

Find exceptions by authenticated user:
```kql
exceptions
| where timestamp > ago(24h)
| extend user = tostring(customDimensions["AuthenticatedUser"])
| where isnotempty(user)
| project timestamp, user, type, outerMessage
| order by timestamp desc
```

Find slow requests:
```kql
requests
| where customDimensions["SlowRequest"] == "true"
| where timestamp > ago(1h)
| project timestamp, name, duration, customDimensions["AuthenticatedUser"]
| order by duration desc
```

### Serilog Integration

All `ILogger` calls are also sent to Application Insights:

```csharp
_logger.LogInformation("Created employee {EmployeeId}", employee.Id);
_logger.LogWarning("Slow request: {Duration}ms", elapsed);
_logger.LogError(ex, "Failed to upload file {FileName}", fileName);
```

These show up in Application Insights → Logs → `traces` table.

---

## 5. Testing All Integrations End-to-End

### Test Checklist

After deploying to Azure, verify each integration:

#### ✅ Azure SQL Database
```bash
# Login
curl -X POST https://week6-employee-api.azurewebsites.net/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'

# Create employee (verifies SQL write)
curl -X POST https://week6-employee-api.azurewebsites.net/api/employees \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName":"Test","lastName":"User","email":"test@azure.com",
    "position":"Engineer","salary":75000,"departmentId":1
  }'

# Get employees (verifies SQL read)
curl https://week6-employee-api.azurewebsites.net/api/employees \
  -H "Authorization: Bearer <token>"
```

#### ✅ Azure Blob Storage
```bash
# Upload file
curl -X POST https://week6-employee-api.azurewebsites.net/api/employees/1/documents/upload \
  -H "Authorization: Bearer <token>" \
  -F "file=@/path/to/test.pdf"

# Download file
curl https://week6-employee-api.azurewebsites.net/api/employees/1/documents/1/download \
  -H "Authorization: Bearer <token>" \
  -o downloaded.pdf

# Delete file
curl -X DELETE https://week6-employee-api.azurewebsites.net/api/employees/1/documents/1 \
  -H "Authorization: Bearer <token>"
```

#### ✅ Azure Key Vault
```bash
# Check app logs to verify secrets were loaded
az webapp log tail --name week6-employee-api --resource-group Week6-EmployeeManagement-RG

# You should see:
# [KeyVault] Connecting to Azure Key Vault: https://week6-emp-kv.vault.azure.net/
# [KeyVault] Azure Key Vault configuration added successfully.
```

#### ✅ Application Insights
1. Make several API calls
2. Azure Portal → Application Insights → Live Metrics (see traffic in real-time)
3. Check Failures blade → should show failed requests (if any)
4. Check Performance blade → average response times

#### ✅ Health Checks
```bash
# Overall health
curl https://week6-employee-api.azurewebsites.net/health

# Liveness (is API running)
curl https://week6-employee-api.azurewebsites.net/health/live

# Readiness (are dependencies healthy)
curl https://week6-employee-api.azurewebsites.net/health/ready
```

---

## 6. Cost Optimization Tips

| Service | Free Tier / Low-Cost Option |
|---------|------------------------------|
| **App Service** | Free (F1) for testing; B1 (~$13/mo) for prod |
| **Azure SQL** | Basic (5 DTU, ~$5/mo); or use SQL Database Serverless |
| **Blob Storage** | Pay-per-use (~$0.02/GB/month for Hot tier) |
| **Key Vault** | ~$0.03 per 10k operations (nearly free) |
| **App Insights** | 5 GB/month free, then ~$2.30/GB |

**Total estimate**: ~$20-30/month for a production-ready API

**To avoid charges when not in use**:
```bash
# Stop the App Service (doesn't delete it, just pauses billing for compute)
az webapp stop --name week6-employee-api --resource-group Week6-EmployeeManagement-RG

# Start it again when needed
az webapp start --name week6-employee-api --resource-group Week6-EmployeeManagement-RG

# Or delete everything to stop all billing
az group delete --name Week6-EmployeeManagement-RG --yes --no-wait
```
