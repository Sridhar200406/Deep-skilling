# ═══════════════════════════════════════════════════════════════════════
# Week 6 Day 6 – Azure API Management Deployment (PowerShell)
# ═══════════════════════════════════════════════════════════════════════
# Prerequisites:
#   az login
#   .NET 8 SDK
#   Run from solution root: week-6\
# ═══════════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

# ─── Configuration ───────────────────────────────────────────────────
$ResourceGroup        = "Week6-EmployeeManagement-RG"
$Location             = "eastus"
$ApimName             = "week6-apim"              # Must be globally unique
$ApimSku              = "Consumption"              # Consumption = serverless APIM
$ApimPublisherEmail   = "admin@company.com"
$ApimPublisherName    = "Employee Management Team"
$KeyVaultName         = "week6-emp-kv"
$AppInsightsName      = "Week6-EmployeeManagement-Insights"
$ContainerEnvName     = "week6-container-env"

# Container App internal URLs
$EmployeeServiceUrl   = "http://employee-service"
$AuthServiceUrl       = "http://auth-service"
$DepartmentServiceUrl = "http://department-service"

# API versioning
$ApiVersion           = "v1"
# ─────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Week 6 Day 6: Azure API Management Deployment"       -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ─── Step 1: Get existing resource values ────────────────────────────
Write-Host "► Step 1: Reading existing resource values..." -ForegroundColor Yellow

$AiConnectionString = az monitor app-insights component show `
    --app $AppInsightsName `
    --resource-group $ResourceGroup `
    --query "connectionString" -o tsv

$JwtSecret = az keyvault secret show `
    --vault-name $KeyVaultName `
    --name "JwtSettings--SecretKey" `
    --query "value" -o tsv

Write-Host "  ✓ Resource values retrieved." -ForegroundColor Green
Write-Host ""

# ─── Step 2: Create APIM Instance ────────────────────────────────────
Write-Host "► Step 2: Creating Azure API Management (Consumption tier)..." -ForegroundColor Yellow
Write-Host "  Note: APIM creation takes 5-10 minutes." -ForegroundColor Gray

az apim create `
    --name $ApimName `
    --resource-group $ResourceGroup `
    --location $Location `
    --publisher-email $ApimPublisherEmail `
    --publisher-name $ApimPublisherName `
    --sku-name $ApimSku `
    --enable-client-certificate false

Write-Host "  ✓ APIM instance created: https://${ApimName}.azure-api.net" -ForegroundColor Green
Write-Host ""

# ─── Step 3: Enable Managed Identity ─────────────────────────────────
Write-Host "► Step 3: Enabling Managed Identity on APIM..." -ForegroundColor Yellow

az apim update `
    --name $ApimName `
    --resource-group $ResourceGroup `
    --enable-managed-identity true

$ApimPrincipalId = az apim show `
    --name $ApimName `
    --resource-group $ResourceGroup `
    --query "identity.principalId" -o tsv

# Grant APIM access to Key Vault
az keyvault set-policy `
    --name $KeyVaultName `
    --object-id $ApimPrincipalId `
    --secret-permissions get list

Write-Host "  ✓ Managed Identity enabled and Key Vault access granted." -ForegroundColor Green
Write-Host ""

# ─── Step 4: Create Named Values (from Key Vault) ────────────────────
Write-Host "► Step 4: Creating Named Values (secrets from Key Vault)..." -ForegroundColor Yellow

# JWT secret — referenced as {{jwt-secret-key}} in policies
$KeyVaultSecretId = "https://${KeyVaultName}.vault.azure.net/secrets/JwtSettings--SecretKey"

az apim nv create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --named-value-id "jwt-secret-key" `
    --display-name "JWT Secret Key" `
    --secret true `
    --value $JwtSecret

Write-Host "  ✓ Named Values created." -ForegroundColor Green
Write-Host ""

# ─── Step 5: Configure Application Insights ──────────────────────────
Write-Host "► Step 5: Connecting Application Insights to APIM..." -ForegroundColor Yellow

$AiResourceId = az monitor app-insights component show `
    --app $AppInsightsName `
    --resource-group $ResourceGroup `
    --query "id" -o tsv

$AiInstrumentationKey = az monitor app-insights component show `
    --app $AppInsightsName `
    --resource-group $ResourceGroup `
    --query "instrumentationKey" -o tsv

# Create App Insights logger
az apim logger create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --logger-id "appinsights-logger" `
    --logger-type applicationInsights `
    --description "Application Insights logger" `
    --credentials "{'instrumentationKey': '$AiInstrumentationKey'}"

Write-Host "  ✓ Application Insights connected." -ForegroundColor Green
Write-Host ""

# ─── Step 6: Create API Products ─────────────────────────────────────
Write-Host "► Step 6: Creating API Products and Subscriptions..." -ForegroundColor Yellow

# Starter product — limited rate
az apim product create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --product-id "starter" `
    --product-name "Starter" `
    --description "50 calls/minute — for development and testing" `
    --subscription-required true `
    --approval-required false `
    --state published

# Standard product — normal rate
az apim product create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --product-id "standard" `
    --product-name "Standard" `
    --description "100 calls/minute — for production use" `
    --subscription-required true `
    --approval-required false `
    --state published

# Unlimited product — for internal services
az apim product create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --product-id "unlimited" `
    --product-name "Unlimited" `
    --description "No rate limit — internal services only" `
    --subscription-required true `
    --approval-required true `
    --state published

Write-Host "  ✓ Products created: Starter, Standard, Unlimited." -ForegroundColor Green
Write-Host ""

# ─── Step 7: Import APIs from OpenAPI spec ───────────────────────────
Write-Host "► Step 7: Importing APIs from OpenAPI spec..." -ForegroundColor Yellow

# Import Employee Management API
az apim api import `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --api-id "employee-management-v1" `
    --display-name "Employee Management API v1" `
    --path "api/v1" `
    --specification-format OpenApiJson `
    --specification-path "apim/api-definitions/employee-api.yaml" `
    --api-version $ApiVersion `
    --api-version-set-name "Employee Management API" `
    --description "Complete Employee Management REST API"

Write-Host "  ✓ APIs imported." -ForegroundColor Green
Write-Host ""

# ─── Step 8: Add APIs to Products ────────────────────────────────────
Write-Host "► Step 8: Adding APIs to Products..." -ForegroundColor Yellow

az apim product api add `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --product-id "starter" `
    --api-id "employee-management-v1"

az apim product api add `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --product-id "standard" `
    --api-id "employee-management-v1"

az apim product api add `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --product-id "unlimited" `
    --api-id "employee-management-v1"

Write-Host "  ✓ APIs added to products." -ForegroundColor Green
Write-Host ""

# ─── Step 9: Apply Global Policy ─────────────────────────────────────
Write-Host "► Step 9: Applying policies..." -ForegroundColor Yellow

# Global policy
$GlobalPolicy = Get-Content "apim/policies/global-policy.xml" -Raw
az apim policy create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --xml-content $GlobalPolicy

Write-Host "  ✓ Policies applied." -ForegroundColor Green
Write-Host ""

# ─── Step 10: Configure Backend Services ─────────────────────────────
Write-Host "► Step 10: Configuring backend services..." -ForegroundColor Yellow

# Employee Service backend
az apim backend create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --backend-id "employee-service" `
    --url $EmployeeServiceUrl `
    --protocol http `
    --title "Employee Service"

# Auth Service backend
az apim backend create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --backend-id "auth-service" `
    --url $AuthServiceUrl `
    --protocol http `
    --title "Auth Service"

# Department Service backend
az apim backend create `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --backend-id "department-service" `
    --url $DepartmentServiceUrl `
    --protocol http `
    --title "Department Service"

Write-Host "  ✓ Backend services configured." -ForegroundColor Green
Write-Host ""

# ─── Step 11: Get subscription keys ──────────────────────────────────
Write-Host "► Step 11: Retrieving subscription keys..." -ForegroundColor Yellow

$StarterKey = az apim subscription show `
    --resource-group $ResourceGroup `
    --service-name $ApimName `
    --subscription-id "starter" `
    --query "primaryKey" -o tsv 2>$null

Write-Host "  Starter subscription key: $StarterKey" -ForegroundColor White
Write-Host ""

# ─── Summary ─────────────────────────────────────────────────────────
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Azure API Management Deployment Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  APIM Gateway URL: https://${ApimName}.azure-api.net"
Write-Host "  Developer Portal: https://${ApimName}.developer.azure-api.net"
Write-Host ""
Write-Host "  API Base URLs:"
Write-Host "    Auth:        https://${ApimName}.azure-api.net/api/v1/auth"
Write-Host "    Employees:   https://${ApimName}.azure-api.net/api/v1/employees"
Write-Host "    Departments: https://${ApimName}.azure-api.net/api/v1/departments"
Write-Host ""
Write-Host "  Test login:"
Write-Host "    curl -X POST https://${ApimName}.azure-api.net/api/v1/auth/login \"
Write-Host "      -H 'Content-Type: application/json' \"
Write-Host "      -H 'Ocp-Apim-Subscription-Key: $StarterKey' \"
Write-Host "      -d '{`"username`":`"admin`",`"password`":`"Admin@123`"}'"
Write-Host ""
