# ═══════════════════════════════════════════════════════════════════════
# Week 6 – Azure Cloud Deployment Script (PowerShell / Windows)
# Employee Management API → Azure App Service
# ═══════════════════════════════════════════════════════════════════════
# Prerequisites:
#   - Azure CLI: winget install Microsoft.AzureCLI  (or https://aka.ms/installazurecliwindows)
#   - .NET 8 SDK
#   - Run: az login   before executing this script
#   - Run from the solution root: week-6\
# ═══════════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

# ─── Configuration — CHANGE THESE ────────────────────────────────────
$ResourceGroup     = "Week6-EmployeeManagement-RG"
$Location          = "eastus"
$AppServicePlan    = "Week6-EmployeeManagement-Plan"
$AppServiceName    = "week6-employee-api"       # Must be globally unique
$SqlServerName     = "week6-sql-server"          # Must be globally unique
$SqlDbName         = "EmployeeManagementDb"
$SqlAdminUser      = "sqladmin"
$SqlAdminPassword  = 'YourStrong!Passw0rd99'     # Change before running!
$StorageAccount    = "week6employeestorage"      # Lowercase, 3-24 chars, globally unique
$BlobContainer     = "employee-files"
$KeyVaultName      = "week6-emp-kv"              # 3-24 chars, globally unique
$AppInsightsName   = "Week6-EmployeeManagement-Insights"
$JwtSecretKey      = "Production-Super-Secret-Key-Min-32-Chars-Azure-2024!!"
# ─────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Week 6: Azure Employee Management Deployment (PS)"    -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ─── Step 1: Verify login ─────────────────────────────────────────────
Write-Host "► Step 1: Verifying Azure login..." -ForegroundColor Yellow
az account show --output table
Write-Host ""

# ─── Step 2: Resource Group ───────────────────────────────────────────
Write-Host "► Step 2: Creating Resource Group..." -ForegroundColor Yellow
az group create `
    --name $ResourceGroup `
    --location $Location `
    --tags Project=EmployeeManagement Week=6 Environment=Production
Write-Host "  ✓ Resource Group: $ResourceGroup" -ForegroundColor Green
Write-Host ""

# ─── Step 3: Application Insights ────────────────────────────────────
Write-Host "► Step 3: Creating Application Insights..." -ForegroundColor Yellow
az monitor app-insights component create `
    --app $AppInsightsName `
    --location $Location `
    --resource-group $ResourceGroup `
    --kind web `
    --application-type web

$AiConnectionString = az monitor app-insights component show `
    --app $AppInsightsName `
    --resource-group $ResourceGroup `
    --query "connectionString" -o tsv

Write-Host "  ✓ Application Insights created." -ForegroundColor Green
Write-Host ""

# ─── Step 4: Azure SQL ────────────────────────────────────────────────
Write-Host "► Step 4: Creating Azure SQL Server and Database..." -ForegroundColor Yellow
az sql server create `
    --name $SqlServerName `
    --resource-group $ResourceGroup `
    --location $Location `
    --admin-user $SqlAdminUser `
    --admin-password $SqlAdminPassword

# Allow Azure services
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name "AllowAzureServices" `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0

# Get current IP for migrations
$MyIP = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content.Trim()
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name "AllowDevMachine" `
    --start-ip-address $MyIP `
    --end-ip-address $MyIP

az sql db create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name $SqlDbName `
    --edition "Basic" `
    --capacity 5

$SqlConnectionString = "Server=tcp:${SqlServerName}.database.windows.net,1433;Database=${SqlDbName};User ID=${SqlAdminUser};Password=${SqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "  ✓ Azure SQL: ${SqlServerName}.database.windows.net" -ForegroundColor Green
Write-Host ""

# ─── Step 5: Azure Storage ───────────────────────────────────────────
Write-Host "► Step 5: Creating Storage Account..." -ForegroundColor Yellow
az storage account create `
    --name $StorageAccount `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku "Standard_LRS" `
    --kind "StorageV2" `
    --https-only true `
    --min-tls-version "TLS1_2" `
    --allow-blob-public-access false

$StorageConnectionString = az storage account show-connection-string `
    --name $StorageAccount `
    --resource-group $ResourceGroup `
    --query "connectionString" -o tsv

az storage container create `
    --name $BlobContainer `
    --account-name $StorageAccount `
    --public-access off

Write-Host "  ✓ Storage Account: ${StorageAccount}.blob.core.windows.net" -ForegroundColor Green
Write-Host ""

# ─── Step 6: Key Vault ───────────────────────────────────────────────
Write-Host "► Step 6: Creating Azure Key Vault and storing secrets..." -ForegroundColor Yellow
az keyvault create `
    --name $KeyVaultName `
    --resource-group $ResourceGroup `
    --location $Location `
    --enable-rbac-authorization false

# Store secrets (double-dash replaces colon in config hierarchy)
az keyvault secret set --vault-name $KeyVaultName --name "ConnectionStrings--DefaultConnection" --value $SqlConnectionString
az keyvault secret set --vault-name $KeyVaultName --name "JwtSettings--SecretKey"               --value $JwtSecretKey
az keyvault secret set --vault-name $KeyVaultName --name "AzureBlobStorage--ConnectionString"   --value $StorageConnectionString
az keyvault secret set --vault-name $KeyVaultName --name "ApplicationInsights--ConnectionString" --value $AiConnectionString

Write-Host "  ✓ Key Vault: https://${KeyVaultName}.vault.azure.net/" -ForegroundColor Green
Write-Host ""

# ─── Step 7: App Service Plan ────────────────────────────────────────
Write-Host "► Step 7: Creating App Service Plan (B1 Linux)..." -ForegroundColor Yellow
az appservice plan create `
    --name $AppServicePlan `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku "B1" `
    --is-linux
Write-Host "  ✓ App Service Plan created." -ForegroundColor Green
Write-Host ""

# ─── Step 8: Web App + Managed Identity + Settings ───────────────────
Write-Host "► Step 8: Creating Web App and configuring..." -ForegroundColor Yellow
az webapp create `
    --name $AppServiceName `
    --resource-group $ResourceGroup `
    --plan $AppServicePlan `
    --runtime "DOTNETCORE:8.0"

# Enable System-Assigned Managed Identity
az webapp identity assign `
    --name $AppServiceName `
    --resource-group $ResourceGroup

$PrincipalId = az webapp identity show `
    --name $AppServiceName `
    --resource-group $ResourceGroup `
    --query "principalId" -o tsv

# Grant Managed Identity access to Key Vault
az keyvault set-policy `
    --name $KeyVaultName `
    --object-id $PrincipalId `
    --secret-permissions get list

# App Settings (non-secret values only — secrets come from Key Vault)
az webapp config appsettings set `
    --name $AppServiceName `
    --resource-group $ResourceGroup `
    --settings `
        ASPNETCORE_ENVIRONMENT="Production" `
        "AzureKeyVault__VaultUri=https://${KeyVaultName}.vault.azure.net/" `
        "JwtSettings__Issuer=EmployeeManagementAPI" `
        "JwtSettings__Audience=EmployeeManagementClient" `
        "JwtSettings__ExpirationMinutes=60" `
        "AzureBlobStorage__ContainerName=${BlobContainer}"

# HTTPS only
az webapp update `
    --name $AppServiceName `
    --resource-group $ResourceGroup `
    --https-only true

Write-Host "  ✓ App Service: https://${AppServiceName}.azurewebsites.net" -ForegroundColor Green
Write-Host ""

# ─── Step 9: Publish and Deploy ──────────────────────────────────────
Write-Host "► Step 9: Building and deploying application..." -ForegroundColor Yellow

$PublishPath = "src\EmployeeService\publish"
dotnet publish "src\EmployeeService\EmployeeService.csproj" -c Release -o $PublishPath

Compress-Archive -Path "$PublishPath\*" -DestinationPath "deploy.zip" -Force

az webapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $AppServiceName `
    --src "deploy.zip"

Remove-Item "deploy.zip" -Force
Remove-Item $PublishPath -Recurse -Force

Write-Host "  ✓ Application deployed." -ForegroundColor Green
Write-Host ""

# ─── Step 10: Run EF Migrations ──────────────────────────────────────
Write-Host "► Step 10: Running EF Core migrations on Azure SQL..." -ForegroundColor Yellow

Set-Location "src\EmployeeService"
dotnet ef database update --connection $SqlConnectionString
Set-Location "..\..\"

Write-Host "  ✓ Database migrations applied." -ForegroundColor Green
Write-Host ""

# ─── Step 11: Verify ─────────────────────────────────────────────────
Write-Host "► Step 11: Verifying deployment..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

try {
    $response = Invoke-WebRequest -Uri "https://${AppServiceName}.azurewebsites.net/health/live" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✓ Health check passed! API is live." -ForegroundColor Green
    }
} catch {
    Write-Host "  ⚠ Health check failed. Check app logs." -ForegroundColor Yellow
    Write-Host "    az webapp log tail --name $AppServiceName --resource-group $ResourceGroup"
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  App URL:    https://${AppServiceName}.azurewebsites.net"
Write-Host "  Swagger:    https://${AppServiceName}.azurewebsites.net/swagger"
Write-Host "  Health:     https://${AppServiceName}.azurewebsites.net/health"
Write-Host "  Key Vault:  https://${KeyVaultName}.vault.azure.net/"
Write-Host "  SQL:        ${SqlServerName}.database.windows.net"
Write-Host ""
Write-Host "  Default login: admin / Admin@123"
Write-Host ""
