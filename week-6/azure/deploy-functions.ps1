# ═══════════════════════════════════════════════════════════════════════
# Week 6 Day 3 – Azure Functions Deployment Script (PowerShell)
# ═══════════════════════════════════════════════════════════════════════
# Prerequisites: az login, .NET 8 SDK, Azure Functions Core Tools v4
#   Install Core Tools: npm install -g azure-functions-core-tools@4
# ═══════════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

# ─── Configuration — CHANGE THESE ────────────────────────────────────
$ResourceGroup      = "Week6-EmployeeManagement-RG"
$Location           = "eastus"
$StorageAccount     = "week6employeestorage"    # Must already exist (from Day 1)
$FunctionAppName    = "week6-employee-functions" # Must be globally unique
$AppInsightsName    = "Week6-EmployeeManagement-Insights"
$KeyVaultName       = "week6-emp-kv"
$AppServiceName     = "week6-employee-api"       # Existing API App Service
$CleanupSchedule    = "0 0 6 * * *"              # Daily at 06:00 UTC
# ─────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Week 6 Day 3: Azure Functions Deployment"             -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ─── Step 1: Get existing resource values ────────────────────────────
Write-Host "► Step 1: Reading existing Azure resources..." -ForegroundColor Yellow

$AiConnectionString = az monitor app-insights component show `
    --app $AppInsightsName `
    --resource-group $ResourceGroup `
    --query "connectionString" -o tsv

$StorageConnectionString = az storage account show-connection-string `
    --name $StorageAccount `
    --resource-group $ResourceGroup `
    --query "connectionString" -o tsv

Write-Host "  ✓ Resource values retrieved." -ForegroundColor Green
Write-Host ""

# ─── Step 2: Create Function App ─────────────────────────────────────
Write-Host "► Step 2: Creating Function App: $FunctionAppName" -ForegroundColor Yellow

az functionapp create `
    --name $FunctionAppName `
    --resource-group $ResourceGroup `
    --storage-account $StorageAccount `
    --consumption-plan-location $Location `
    --runtime dotnet-isolated `
    --runtime-version 8 `
    --functions-version 4 `
    --app-insights $AppInsightsName `
    --os-type Linux

Write-Host "  ✓ Function App created on Consumption Plan (serverless)." -ForegroundColor Green
Write-Host ""

# ─── Step 3: Enable System-Assigned Managed Identity ─────────────────
Write-Host "► Step 3: Enabling Managed Identity..." -ForegroundColor Yellow

az functionapp identity assign `
    --name $FunctionAppName `
    --resource-group $ResourceGroup

$PrincipalId = az functionapp identity show `
    --name $FunctionAppName `
    --resource-group $ResourceGroup `
    --query "principalId" -o tsv

# Grant Key Vault access
az keyvault set-policy `
    --name $KeyVaultName `
    --object-id $PrincipalId `
    --secret-permissions get list

Write-Host "  ✓ Managed Identity enabled and Key Vault access granted." -ForegroundColor Green
Write-Host ""

# ─── Step 4: Configure Application Settings ──────────────────────────
Write-Host "► Step 4: Configuring App Settings (non-secrets only)..." -ForegroundColor Yellow

# Get the SQL connection string from Key Vault
$SqlConnectionString = az keyvault secret show `
    --vault-name $KeyVaultName `
    --name "ConnectionStrings--DefaultConnection" `
    --query "value" -o tsv

# Get blob storage connection from Key Vault
$BlobConnectionString = az keyvault secret show `
    --vault-name $KeyVaultName `
    --name "AzureBlobStorage--ConnectionString" `
    --query "value" -o tsv

# Get Employee API URL
$ApiUrl = "https://${AppServiceName}.azurewebsites.net"

az functionapp config appsettings set `
    --name $FunctionAppName `
    --resource-group $ResourceGroup `
    --settings `
        "AzureKeyVault__VaultUri=https://${KeyVaultName}.vault.azure.net/" `
        "AzureBlobStorage__ContainerName=employee-files" `
        "Cleanup__Schedule=${CleanupSchedule}" `
        "Cleanup__InactiveDaysThreshold=90" `
        "Cleanup__TempFileAgeHours=24" `
        "Report__AdminEmail=admin@company.com" `
        "EmployeeApi__BaseUrl=${ApiUrl}" `
        "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" `
        "FUNCTIONS_EXTENSION_VERSION=~4"

Write-Host "  ✓ App Settings configured." -ForegroundColor Green
Write-Host ""

# ─── Step 5: Update Employee API settings with Function URL ──────────
Write-Host "► Step 5: Updating Employee API with Function URL..." -ForegroundColor Yellow

# Get the Function App's default host key
$FunctionKey = az functionapp keys list `
    --name $FunctionAppName `
    --resource-group $ResourceGroup `
    --query "functionKeys.default" -o tsv

$NotificationUrl = "https://${FunctionAppName}.azurewebsites.net/api/employee-notification"

az webapp config appsettings set `
    --name $AppServiceName `
    --resource-group $ResourceGroup `
    --settings `
        "AzureFunctions__NotificationUrl=${NotificationUrl}" `
        "AzureFunctions__FunctionKey=${FunctionKey}"

Write-Host "  ✓ Employee API updated with Function URL." -ForegroundColor Green
Write-Host ""

# ─── Step 6: Deploy Functions ─────────────────────────────────────────
Write-Host "► Step 6: Building and deploying Azure Functions..." -ForegroundColor Yellow

$PublishPath = "src\AzureFunctions\publish"
dotnet publish "src\AzureFunctions\AzureFunctions.csproj" -c Release -o $PublishPath

Compress-Archive -Path "$PublishPath\*" -DestinationPath "functions-deploy.zip" -Force

az functionapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $FunctionAppName `
    --src "functions-deploy.zip"

Remove-Item "functions-deploy.zip" -Force
Remove-Item $PublishPath -Recurse -Force

Write-Host "  ✓ Azure Functions deployed." -ForegroundColor Green
Write-Host ""

# ─── Step 7: Verify ───────────────────────────────────────────────────
Write-Host "► Step 7: Verifying deployment..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

$HealthUrl = "https://${FunctionAppName}.azurewebsites.net/api/employee-notification/health"
try {
    $Response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing
    if ($Response.StatusCode -eq 200) {
        Write-Host "  ✓ Function health check passed!" -ForegroundColor Green
    }
} catch {
    Write-Host "  ⚠ Health check failed — check logs below:" -ForegroundColor Yellow
    Write-Host "    az functionapp logs tail --name $FunctionAppName --resource-group $ResourceGroup"
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Azure Functions Deployment Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Function App: https://${FunctionAppName}.azurewebsites.net"
Write-Host "  HTTP Trigger: POST /api/employee-notification"
Write-Host "  Health Check: GET  /api/employee-notification/health"
Write-Host "  Timer runs:   $CleanupSchedule (UTC)"
Write-Host ""
Write-Host "  Test with:"
Write-Host "    curl https://${FunctionAppName}.azurewebsites.net/api/employee-notification/health"
Write-Host ""
