# Week 6 – Azure Cloud Fundamentals, Integration & CI/CD
## Employee Management Microservices — Azure Edition

> **Day 1**: Azure resources + full API deployment  
> **Day 2**: Azure service integration + GitHub Actions CI/CD pipeline + Unit Tests

---

## 🏗️ Project Architecture

```
User
 └── HTTPS
      └── Azure App Service (ASP.NET Core 8)
           ├── Azure SQL Database        ← Employee/User/Dept data
           ├── Azure Blob Storage        ← Employee documents/files
           ├── Azure Key Vault           ← All secrets (no hardcoding)
           └── Application Insights      ← Monitoring & telemetry
```

---

## 📚 Azure Concepts Explained

### Azure Subscription
Your billing account with Microsoft. Everything you create in Azure belongs to a subscription. 
Think of it as your "Azure account" — it has a spending limit, billing contact, and access controls.
- One subscription can contain many Resource Groups
- You get billed per subscription
- Multiple subscriptions are common in enterprises (Dev/Staging/Production)

### Resource Group
A **logical container** that groups related Azure resources for an application or project.
- Acts like a folder for all your Azure resources
- All resources in a group share the same lifecycle (deploy, monitor, delete together)
- Resources in a group can be in different Azure regions
- **Best practice**: one resource group per application/environment
- Example: `Week6-EmployeeManagement-RG` holds the App Service, SQL, Storage, Key Vault, App Insights

### Azure Region
A geographic location where Azure has data centers. Examples: `eastus`, `westeurope`, `southeastasia`.
- Choose a region closest to your users for lower latency
- Some services are only available in certain regions
- Data sovereignty laws may require specific regions (e.g., GDPR → European regions)
- **Paired regions** provide disaster recovery (eastus ↔ westus)

### Azure Resource
Any individual service you create in Azure — a Web App, SQL Database, Storage Account, Key Vault, etc.
- Every resource has a unique Resource ID
- Resources are billed individually
- Resources can communicate with each other within the same subscription

---

## 🚀 Azure Services Used

### 1. Azure App Service
A fully managed **Platform as a Service (PaaS)** for hosting web applications.
- No server management — Microsoft handles OS patches, scaling, load balancing
- Supports .NET, Node.js, Python, Java, PHP, Ruby, Docker
- Built-in features: SSL/TLS, custom domains, auto-scaling, deployment slots
- **App Service Plan**: defines the hardware tier (CPU, memory, region)
  - Free/Shared: dev/test only
  - Basic (B1): entry-level production (~$13/month)
  - Standard (S1): production with autoscale
  - Premium: high-performance production

**This project uses**: B1 Linux plan with .NET 8 runtime

### 2. Azure SQL Database
A fully managed **relational database** based on SQL Server.
- Handles backups, patching, high availability automatically
- Supports all T-SQL syntax
- Built-in firewall rules control network access
- Connection string uses the Azure SQL server endpoint: `<server>.database.windows.net`
- **Service Tiers**: Basic, Standard, Premium, Hyperscale
- Geo-replication available for disaster recovery

**This project uses**: Basic tier (5 DTUs, 2 GB max)

### 3. Azure Blob Storage
**Object storage** for unstructured data — files, images, videos, backups, logs.
- Blob = Binary Large Object
- Organized as: `Storage Account → Containers → Blobs`
- Three blob types: Block Blobs (files), Append Blobs (logs), Page Blobs (VHDs)
- Access tiers: Hot (frequent), Cool (infrequent), Archive (rare)
- **Public access disabled** in this project — files are only accessible via API

**This project uses**: Standard LRS, Hot tier, private container

### 4. Azure Key Vault
A **secure secret store** — like a locked safe for sensitive configuration.
- Stores secrets (passwords, connection strings, API keys)
- Stores certificates (TLS/SSL certificates)
- Stores cryptographic keys (encryption keys)
- Fine-grained access policies (who can get/list/set secrets)
- Full audit log of every access
- **Managed Identity integration**: App Service can read secrets WITHOUT storing any credentials

**Secret Naming Convention** (ASP.NET Core):
```
Config key:    ConnectionStrings:DefaultConnection
Key Vault name: ConnectionStrings--DefaultConnection
                              ↑↑ double-dash replaces colon
```

### 5. Application Insights
Azure's **Application Performance Management (APM)** service.
- Tracks: HTTP requests, response times, exceptions, failures, dependencies
- Live Metrics: real-time traffic monitoring
- Application Map: visualizes dependencies between services
- Alerts: notify on error rate spikes or performance degradation
- Log Analytics: powerful KQL query language for searching logs
- **Connection string** is the modern way to configure it (replaces Instrumentation Key)

---

## 🔐 Security Architecture

```
App Service
  │
  ├── System-Assigned Managed Identity
  │     └── No passwords stored anywhere!
  │           └── Azure grants temporary tokens automatically
  │
  └── Key Vault Access Policy
        └── Managed Identity Principal ID → get, list secrets
```

**The flow**:
1. App Service has a Managed Identity (auto-provisioned by Azure)
2. Key Vault has an access policy granting that identity read access
3. At startup, ASP.NET Core's `DefaultAzureCredential` gets a token from Azure AD
4. It uses that token to read secrets from Key Vault
5. Secrets are loaded into `IConfiguration` — no code changes needed

**Secrets stored in Key Vault** (never in code or Git):
- `ConnectionStrings--DefaultConnection` (Azure SQL connection string)
- `JwtSettings--SecretKey` (JWT signing secret)
- `AzureBlobStorage--ConnectionString` (Storage account connection string)
- `ApplicationInsights--ConnectionString` (App Insights connection string)

---

## 📁 Project Structure

```
week-6/
├── EmployeeManagement.sln
├── docker-compose.yml                    # Local dev stack
├── README.md
├── .github/
│   └── workflows/
│       └── azure-deploy.yml              # Day 2: GitHub Actions CI/CD
├── azure/
│   ├── deploy.sh                         # Bash deployment script
│   └── deploy-windows.ps1                # PowerShell deployment script
├── docs/
│   ├── cicd-setup.md                     # Day 2: CI/CD & Secrets setup guide
│   └── azure-integration-guide.md        # Day 2: Azure integration guide
├── k8s/
│   ├── namespace.yaml
│   ├── employee-service-deployment.yaml
│   └── ingress.yaml
└── src/
    ├── Shared/
    ├── EmployeeService.Tests/             # Day 2: Unit + integration tests
    │   ├── Services/
    │   │   ├── EmployeeServiceTests.cs    # 10 CRUD unit tests
    │   │   ├── AuthServiceTests.cs        # 5 auth unit tests
    │   │   └── BlobStorageServiceTests.cs # Blob validation tests
    │   ├── Controllers/
    │   │   └── EmployeesControllerTests.cs
    │   └── Integration/
    │       └── HealthCheckTests.cs
    └── EmployeeService/
        ├── Controllers/
        │   ├── AuthController.cs         # POST /api/auth/login, /register, /verify
        │   ├── EmployeesController.cs    # CRUD /api/employees
        │   ├── DocumentsController.cs    # Blob /api/employees/{id}/documents
        │   ├── DepartmentsController.cs  # /api/departments
        │   └── HealthController.cs       # /health, /health/live, /health/ready
        ├── Application/
        │   ├── DTOs/
        │   └── Services/
        ├── Domain/Entities/
        ├── Infrastructure/
        │   ├── Auth/JwtTokenService.cs
        │   ├── Azure/
        │   │   ├── BlobStorageService.cs
        │   │   └── KeyVaultConfiguration.cs
        │   ├── Data/EmployeeDbContext.cs
        │   └── Telemetry/                # Day 2: App Insights enrichment
        │       ├── AppInsightsTelemetryInitializer.cs
        │       └── RequestTelemetryMiddleware.cs
        ├── Migrations/
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── Program.cs
        └── Dockerfile
```

---

## ⚡ Quick Start (Local Development)

### Prerequisites
- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8
- Docker Desktop: https://www.docker.com/products/docker-desktop/
- Azure Storage Explorer (optional): https://azure.microsoft.com/en-us/products/storage/storage-explorer/

### Option A: Docker Compose (Recommended)
```bash
# Start all services (SQL Server, Redis, RabbitMQ, Azurite, Employee API)
docker-compose up --build

# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# RabbitMQ UI: http://localhost:15672 (guest/guest)
```

### Option B: Run locally with LocalDB
```bash
cd src/EmployeeService

# Restore packages
dotnet restore

# Apply migrations
dotnet ef database update

# Run
dotnet run

# Swagger: https://localhost:7001 (or check launchSettings.json)
```

---

## 🔧 EF Core Migration Commands

```bash
# Navigate to EmployeeService
cd src/EmployeeService

# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to local DB
dotnet ef database update

# Apply migrations to Azure SQL (production)
dotnet ef database update --connection "Server=tcp:<server>.database.windows.net,1433;..."

# Generate SQL script (for review before applying)
dotnet ef migrations script --output migration.sql

# List all migrations
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove
```

---

## ☁️ Azure Deployment Steps

### Step 1: Install Azure CLI
```powershell
# Windows (PowerShell)
winget install Microsoft.AzureCLI
# or download from: https://aka.ms/installazurecliwindows
```

### Step 2: Login to Azure
```bash
az login
# Opens browser for authentication

# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription "<subscription-id-or-name>"
```

### Step 3: Run the deployment script
```powershell
# Windows PowerShell (edit variables in the script first!)
.\azure\deploy-windows.ps1

# Or Linux/Mac
chmod +x azure/deploy.sh
./azure/deploy.sh
```

### Manual Azure CLI Commands

#### Resource Group
```bash
az group create \
  --name "Week6-EmployeeManagement-RG" \
  --location "eastus"
```

#### App Service
```bash
# Create plan
az appservice plan create \
  --name "Week6-Plan" \
  --resource-group "Week6-EmployeeManagement-RG" \
  --sku B1 --is-linux

# Create web app
az webapp create \
  --name "week6-employee-api" \
  --resource-group "Week6-EmployeeManagement-RG" \
  --plan "Week6-Plan" \
  --runtime "DOTNETCORE:8.0"

# Enable HTTPS only
az webapp update \
  --name "week6-employee-api" \
  --resource-group "Week6-EmployeeManagement-RG" \
  --https-only true
```

#### Publish from Visual Studio
1. Right-click `EmployeeService` project → **Publish**
2. Target: **Azure** → **Azure App Service (Linux)**
3. Select your subscription and the created App Service
4. Click **Publish**

#### Publish from CLI
```bash
dotnet publish src/EmployeeService -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
az webapp deployment source config-zip \
  --resource-group "Week6-EmployeeManagement-RG" \
  --name "week6-employee-api" \
  --src "../deploy.zip"
```

---

## 🧪 API Testing Guide (Swagger)

### Base URL
- Local: `http://localhost:5000`
- Azure: `https://week6-employee-api.azurewebsites.net`

### 1. Login to get JWT token
```
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
```
Copy the `token` from the response.

### 2. Authorize in Swagger
Click the **Authorize 🔒** button → Enter: `Bearer <your-token>`

### 3. Test Employee CRUD
```
GET    /api/employees              → List all employees
GET    /api/employees/1            → Get employee by ID
POST   /api/employees              → Create employee (Admin/Manager)
PUT    /api/employees/1            → Update employee (Admin/Manager)
DELETE /api/employees/1            → Delete employee (Admin)
```

### 4. Test Blob Storage (Documents)
```
POST   /api/employees/1/documents/upload   → Upload file (multipart/form-data)
GET    /api/employees/1/documents          → List documents
GET    /api/employees/1/documents/1/download → Download file
DELETE /api/employees/1/documents/1        → Delete document
```

### 5. Health Checks
```
GET /health        → Full health report (SQL, Blob, API)
GET /health/live   → Liveness probe (API running)
GET /health/ready  → Readiness probe (dependencies ready)
```

---

## 📊 Application Insights Monitoring

After deployment, in Azure Portal → Application Insights:

| Feature | Path |
|---------|------|
| Live request traffic | Live Metrics |
| Request rates & failures | Failures blade |
| Average response time | Performance blade |
| All logged events | Logs (KQL) |
| Service dependencies | Application Map |

### Sample KQL Queries
```kql
-- Failed requests in last 24h
requests
| where success == false
| where timestamp > ago(24h)
| summarize count() by name, resultCode
| order by count_ desc

-- Average response time by endpoint
requests
| where timestamp > ago(1h)
| summarize avg(duration) by name
| order by avg_duration desc

-- Exceptions
exceptions
| where timestamp > ago(24h)
| project timestamp, type, outerMessage, stack
| order by timestamp desc
```

---

## 🐛 Troubleshooting

### App won't start on Azure
```bash
# View live logs
az webapp log tail \
  --name "week6-employee-api" \
  --resource-group "Week6-EmployeeManagement-RG"

# Download all logs
az webapp log download \
  --name "week6-employee-api" \
  --resource-group "Week6-EmployeeManagement-RG"
```

### Key Vault: 403 Forbidden
- Managed Identity may not have been granted Key Vault access
- Check: Key Vault → Access Policies → verify App Service identity is listed
- Fix:
```bash
az keyvault set-policy \
  --name "week6-emp-kv" \
  --object-id "<principal-id>" \
  --secret-permissions get list
```

### Azure SQL: Cannot connect
- Check firewall rules: Azure Portal → SQL Server → Networking
- Add your current IP: `az sql server firewall-rule create ...`
- Ensure "Allow Azure services" is enabled

### Blob Storage: 403 errors
- Verify the connection string in Key Vault is correct
- Check container name matches `AzureBlobStorage:ContainerName` setting
- Verify container was created: Azure Portal → Storage → Containers

### JWT: 401 Unauthorized
- Token might be expired — re-login to get a new token
- Verify `JwtSettings:SecretKey` in Key Vault matches what was used to issue the token
- Check issuer/audience config matches on both token generation and validation

### EF Migration fails on Azure SQL
- Ensure your IP is in the SQL Server firewall rules
- Use `--connection` flag with the Azure SQL connection string
- Check that `TrustServerCertificate=False` in the production connection string

---

## 🔒 Security Checklist

- [x] JWT secret stored in Azure Key Vault (not in code)
- [x] DB connection string stored in Azure Key Vault
- [x] Storage connection string stored in Azure Key Vault
- [x] No secrets in `appsettings.Production.json` or Git
- [x] App Service uses Managed Identity (no stored credentials)
- [x] Blob container has public access disabled
- [x] HTTPS-only enforced on App Service
- [x] SQL Server firewall — Azure services + specific IP only
- [x] Passwords hashed with BCrypt (never stored in plaintext)
- [x] File upload validates content type and size
- [x] Soft delete (employees are deactivated, not hard-deleted)
- [x] Role-based access (Admin, Manager, User)

---

## 💰 Azure Cost Estimate (Monthly)

| Service | Tier | Est. Cost |
|---------|------|-----------|
| App Service | B1 Linux | ~$13 |
| Azure SQL | Basic (5 DTU) | ~$5 |
| Storage Account | Standard LRS | ~$0.02/GB |
| Key Vault | Standard | ~$0.03/10k ops |
| Application Insights | Pay-per-use | ~$2.30/GB data |
| **Total** | | **~$20-25/month** |

> **Tip**: Delete the resource group when done to stop all billing:
> ```bash
> az group delete --name "Week6-EmployeeManagement-RG" --yes --no-wait
> ```

---

## 🔄 Day 2 – CI/CD with GitHub Actions

### What Was Added on Day 2

| Area | What's New |
|------|-----------|
| **CI/CD Pipeline** | `.github/workflows/azure-deploy.yml` — full build → test → deploy pipeline |
| **Unit Tests** | 24 tests across `EmployeeService.Tests` (Services + Controllers + Integration) |
| **App Insights enrichment** | `AppInsightsTelemetryInitializer` + `RequestTelemetryMiddleware` |
| **Docs** | `docs/cicd-setup.md` and `docs/azure-integration-guide.md` |

---

### CI/CD Pipeline Overview

```
git push to main / week-6-azure-deployment
            ↓
  GitHub Actions triggered
            ↓
  ┌─── Job 1: build-and-test ────────────────┐
  │  1. Checkout code                         │
  │  2. Setup .NET 8                          │
  │  3. Cache NuGet packages                  │
  │  4. dotnet restore                        │
  │  5. dotnet build --configuration Release  │
  │  6. dotnet test (xUnit — 24 tests)        │
  │  7. dotnet publish                        │
  │  8. Upload artifact                       │
  └───────────────────────────────────────────┘
            ↓ (only on push, not PRs)
  ┌─── Job 2: deploy ────────────────────────┐
  │  1. Download artifact                     │
  │  2. az login (AZURE_CREDENTIALS secret)  │
  │  3. dotnet ef database update             │
  │  4. azure/webapps-deploy                  │
  │  5. Health check verification             │
  │  6. az logout                             │
  └───────────────────────────────────────────┘
            ↓
  https://week6-employee-api.azurewebsites.net
```

### Required GitHub Secrets

Configure at: **GitHub repo → Settings → Secrets and variables → Actions**

| Secret | Value | How to get |
|--------|-------|-----------|
| `AZURE_WEBAPP_NAME` | `week6-employee-api` | Your App Service name |
| `AZURE_CREDENTIALS` | JSON from service principal | `az ad sp create-for-rbac --sdk-auth` |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | XML from Azure Portal | App Service → Get publish profile |
| `SQL_CONNECTION_STRING` | Azure SQL connection string | Azure Portal → SQL DB → Connection strings |

### Create the Azure Service Principal

```bash
az ad sp create-for-rbac \
  --name "week6-github-actions-sp" \
  --role "Contributor" \
  --scopes "/subscriptions/<SUBSCRIPTION_ID>/resourceGroups/Week6-EmployeeManagement-RG" \
  --sdk-auth
```
Copy the full JSON output → paste as `AZURE_CREDENTIALS` secret.

### Trigger the Pipeline

```bash
# Push a code change to trigger automatically
git add .
git commit -m "feat: trigger CI/CD pipeline"
git push origin week-6-azure-deployment

# Or trigger manually:
# GitHub → Actions → "Employee Management API – CI/CD" → Run workflow
```

### Monitor the Pipeline

1. Go to: `https://github.com/Sridhar200406/Deep-skilling/actions`
2. Click the running workflow to see live logs
3. Green ✅ = all steps passed, app is deployed
4. Red ❌ = check the failed step for error details

---

## 🧪 Running Tests Locally

```bash
cd src/EmployeeService.Tests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run only unit tests (skip integration)
dotnet test --filter "FullyQualifiedName!~Integration"
```

**Test coverage**:

| Test Class | Tests | What it covers |
|-----------|-------|----------------|
| `EmployeeServiceTests` | 10 | Create, Read, Update, Delete, Pagination, Search |
| `AuthServiceTests` | 5 | Register, Login, Duplicate detection |
| `BlobStorageServiceTests` | 4 | Content type validation, config validation |
| `EmployeesControllerTests` | 5 | HTTP responses, 404 handling |
| `HealthCheckTests` | 4 | Live endpoint, Auth guard, Swagger |

---

## 📖 Additional Documentation

- **CI/CD Setup Guide**: `docs/cicd-setup.md` — step-by-step pipeline setup, secrets config, troubleshooting
- **Azure Integration Guide**: `docs/azure-integration-guide.md` — all Azure services explained with code examples and KQL queries
