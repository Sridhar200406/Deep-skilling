# Week 6 Day 2 – CI/CD Setup Guide

## What is CI/CD?

**CI (Continuous Integration)** — Every code push triggers an automated pipeline that:
1. Checks out the code
2. Builds it
3. Runs all tests
4. Reports pass/fail immediately

**CD (Continuous Deployment)** — After CI passes, automatically:
1. Publishes the compiled app
2. Deploys it to Azure App Service
3. Verifies the deployment health

**Why it matters**: eliminates manual deployments, catches bugs early, and ensures every push to `main` is always deployable.

---

## GitHub Actions

GitHub Actions is GitHub's built-in CI/CD platform.

**Key concepts**:

| Term | Meaning |
|------|---------|
| **Workflow** | A YAML file in `.github/workflows/` — defines the full pipeline |
| **Job** | A group of steps that run on one runner machine |
| **Step** | A single command or action inside a job |
| **Runner** | The virtual machine that executes the job (`ubuntu-latest`, `windows-latest`) |
| **Action** | A reusable unit of work — e.g. `actions/checkout@v4`, `azure/webapps-deploy@v3` |
| **Secret** | Encrypted variable stored in GitHub, injected as `${{ secrets.MY_SECRET }}` |
| **Trigger** | The event that starts the workflow (`push`, `pull_request`, `workflow_dispatch`) |

---

## Pipeline Flow

```
Developer pushes to main
         ↓
GitHub Actions triggered
         ↓
Job 1: build-and-test (ubuntu-latest)
  ├── Checkout code
  ├── Setup .NET 8
  ├── Cache NuGet packages
  ├── dotnet restore
  ├── dotnet build --configuration Release
  ├── dotnet test (xUnit)
  ├── dotnet publish
  └── Upload artifact
         ↓
Job 2: deploy (ubuntu-latest) — only on push, not PR
  ├── Download artifact
  ├── az login (using AZURE_CREDENTIALS secret)
  ├── dotnet ef database update (run migrations)
  ├── azure/webapps-deploy (deploy to App Service)
  └── Health check verification
         ↓
Azure App Service running updated code
```

---

## Step-by-Step Setup

### 1. Create the Azure Service Principal

The pipeline needs credentials to log into Azure. A **Service Principal** is like a machine account.

```bash
# Replace with your actual subscription ID and resource group
az ad sp create-for-rbac \
  --name "week6-github-actions-sp" \
  --role "Contributor" \
  --scopes "/subscriptions/<SUBSCRIPTION_ID>/resourceGroups/Week6-EmployeeManagement-RG" \
  --sdk-auth
```

This outputs JSON like:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  ...
}
```
**Copy the entire JSON output** — you'll paste it as the `AZURE_CREDENTIALS` secret.

### 2. Get the App Service Publish Profile

1. Azure Portal → **App Services** → `week6-employee-api`
2. Click **Overview** → **Get publish profile** (top bar)
3. This downloads a `.PublishSettings` XML file
4. Open it and copy the **entire contents** — paste as `AZURE_WEBAPP_PUBLISH_PROFILE`

### 3. Configure GitHub Secrets

1. Go to your GitHub repo: `https://github.com/Sridhar200406/Deep-skilling`
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each:

| Secret Name | Value | Where to get it |
|-------------|-------|-----------------|
| `AZURE_WEBAPP_NAME` | `week6-employee-api` | Your App Service name |
| `AZURE_CREDENTIALS` | The full JSON from Step 1 | `az ad sp create-for-rbac` output |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Contents of `.PublishSettings` file | Azure Portal |
| `SQL_CONNECTION_STRING` | Full Azure SQL connection string | Azure Portal → SQL DB → Connection strings |

> **Never** put these values directly in any file committed to Git.

### 4. Configure GitHub Environment (Optional but recommended)

Environments add an approval gate before production deployments.

1. GitHub repo → **Settings** → **Environments** → **New environment**
2. Name: `production`
3. Add **Required reviewers** (yourself)
4. This makes the deploy job wait for manual approval

---

## Managed Identity Explained

### The Problem
Traditional approach: store credentials (username/password) in config files or env vars. But credentials can leak if:
- Someone reads the config file
- Secrets are accidentally committed to Git
- Env vars are exposed in logs

### The Solution: Managed Identity

Azure can give your App Service an **automatic identity** — like an employee badge that Azure issues and manages automatically. No passwords, no secrets, no rotation needed.

```
App Service (has Managed Identity)
         ↓
  "I need to read secrets"
         ↓
Azure AD (verifies the identity automatically)
         ↓
Key Vault (checks access policy for this identity)
         ↓
Returns the secret value
```

**Setup steps** (already in `deploy.sh`):
```bash
# 1. Enable Managed Identity on the App Service
az webapp identity assign --name "week6-employee-api" --resource-group "Week6-EmployeeManagement-RG"

# 2. Get the Principal ID
PRINCIPAL_ID=$(az webapp identity show --name "week6-employee-api" \
  --resource-group "Week6-EmployeeManagement-RG" --query "principalId" -o tsv)

# 3. Grant Key Vault access to that identity
az keyvault set-policy --name "week6-emp-kv" \
  --object-id "$PRINCIPAL_ID" \
  --secret-permissions get list
```

**In the app code** (`KeyVaultConfiguration.cs`), `DefaultAzureCredential` automatically detects the Managed Identity when running on Azure:
```csharp
var credential = new DefaultAzureCredential();  // uses Managed Identity on Azure
builder.AddAzureKeyVault(new Uri(vaultUri), credential);
```

Locally it falls back to: environment variables → Visual Studio login → Azure CLI login.

---

## Azure App Service Deployment Explained

### How `azure/webapps-deploy` works

1. GitHub Actions calls the Azure REST API with the publish profile credentials
2. Azure creates a deployment package from your published artifacts
3. App Service runs the deployment (Kudu deployment engine)
4. App Service restarts with the new code
5. Old requests finish on the old instance before cutover (zero-downtime)

### Deployment Slots (Advanced)
For zero-downtime deploys:
1. Deploy to **staging slot** first
2. Run smoke tests against staging
3. **Swap** staging ↔ production (instantaneous)

```bash
# Create a staging slot
az webapp deployment slot create --name "week6-employee-api" \
  --resource-group "Week6-EmployeeManagement-RG" --slot staging

# Deploy to staging (in the workflow, add --slot staging)
# Then swap:
az webapp deployment slot swap --name "week6-employee-api" \
  --resource-group "Week6-EmployeeManagement-RG" \
  --slot staging --target-slot production
```

---

## Running and Testing the Pipeline

### Trigger the pipeline manually
1. GitHub repo → **Actions** tab
2. Select **Employee Management API – CI/CD**
3. Click **Run workflow** → select branch → **Run workflow**

### Watch it run
1. Click the running workflow
2. Click **build-and-test** job to see each step in real time
3. After it passes, the **deploy** job starts automatically

### Verify after deployment
```bash
# Check health
curl https://week6-employee-api.azurewebsites.net/health/live

# View live logs
az webapp log tail --name week6-employee-api --resource-group Week6-EmployeeManagement-RG

# Check recent deployments
az webapp deployment list --name week6-employee-api --resource-group Week6-EmployeeManagement-RG
```

---

## Troubleshooting

### Pipeline fails at "Build"
- Check `dotnet build` output in the Actions log
- Common causes: missing package references, compile errors in new code

### Pipeline fails at "Test"
- Click the failed test step to see which tests failed
- Download the `test-results.trx` artifact for detailed output

### Pipeline fails at "Deploy" — authentication error
- Verify `AZURE_CREDENTIALS` secret is the complete JSON (not truncated)
- Re-run `az ad sp create-for-rbac` and update the secret

### Pipeline deploys but app crashes
```bash
# Stream live logs
az webapp log tail --name week6-employee-api --resource-group Week6-EmployeeManagement-RG

# Most common cause: missing Key Vault URI in app settings
# Verify:
az webapp config appsettings list --name week6-employee-api \
  --resource-group Week6-EmployeeManagement-RG
```

### EF migrations step fails
- This step uses `continue-on-error: true` so it won't block deployment
- Run migrations manually: `dotnet ef database update --connection "<connection-string>"`
- Check the IP firewall on Azure SQL allows the GitHub Actions runner IP

### "401 Unauthorized" from Key Vault
- Managed Identity may not be assigned
- Check: `az webapp identity show --name week6-employee-api --resource-group Week6-EmployeeManagement-RG`
- Re-run the Key Vault access policy grant command

---

## Application Insights Monitoring

After deployment, monitor your API:

**Azure Portal → Application Insights → `Week6-EmployeeManagement-Insights`**

| What to check | Where |
|--------------|-------|
| API request rate | Live Metrics |
| Failed requests | Failures blade |
| Response time P95 | Performance blade |
| Exception details | Failures → Exceptions |
| Slow requests | Performance → Slow Requests |
| Custom logs | Logs (KQL) |

### Useful KQL Queries
```kql
// All requests in last hour
requests
| where timestamp > ago(1h)
| project timestamp, name, duration, resultCode, success
| order by timestamp desc

// Failed requests grouped by endpoint
requests
| where success == false and timestamp > ago(24h)
| summarize count() by name, resultCode
| order by count_ desc

// P50/P95/P99 response times
requests
| where timestamp > ago(1h)
| summarize
    p50 = percentile(duration, 50),
    p95 = percentile(duration, 95),
    p99 = percentile(duration, 99)
  by name
| order by p95 desc

// Exceptions by type
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc

// Authenticated user activity
requests
| where customDimensions["AuthenticatedUser"] != ""
| summarize requestCount = count() by tostring(customDimensions["AuthenticatedUser"])
| order by requestCount desc
```
