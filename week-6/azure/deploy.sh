#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════
# Week 6 – Azure Cloud Deployment Script
# Employee Management API → Azure App Service
# ═══════════════════════════════════════════════════════════════════════
# Prerequisites:
#   - Azure CLI installed: https://aka.ms/installazurecliwindows
#   - Logged in: az login
#   - .NET 8 SDK installed
#   - Run from the solution root (week-6/)
# ═══════════════════════════════════════════════════════════════════════

set -e  # Exit on any error

# ─── Configuration — CHANGE THESE ────────────────────────────────────
RESOURCE_GROUP="Week6-EmployeeManagement-RG"
LOCATION="eastus"                          # az account list-locations -o table
APP_SERVICE_PLAN="Week6-EmployeeManagement-Plan"
APP_SERVICE_NAME="week6-employee-api"      # Must be globally unique
SQL_SERVER_NAME="week6-sql-server"         # Must be globally unique
SQL_DB_NAME="EmployeeManagementDb"
SQL_ADMIN_USER="sqladmin"
SQL_ADMIN_PASSWORD="YourStrong!Passw0rd99"  # Change before running!
STORAGE_ACCOUNT_NAME="week6employeestorage" # Lowercase, 3-24 chars, globally unique
BLOB_CONTAINER_NAME="employee-files"
KEY_VAULT_NAME="week6-emp-kv"              # 3-24 chars, globally unique
APP_INSIGHTS_NAME="Week6-EmployeeManagement-Insights"
JWT_SECRET_KEY="Production-Super-Secret-Key-Min-32-Chars-Azure-2024!!"
# ─────────────────────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════"
echo "  Week 6: Azure Employee Management Deployment"
echo "═══════════════════════════════════════════════════════"
echo ""

# ─── 1. LOGIN & SET SUBSCRIPTION ─────────────────────────────────────
echo "► Step 1: Verifying Azure login..."
az account show --output table
echo ""

# ─── 2. RESOURCE GROUP ───────────────────────────────────────────────
echo "► Step 2: Creating Resource Group: $RESOURCE_GROUP in $LOCATION"
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --tags Project=EmployeeManagement Week=6 Environment=Production

echo "  ✓ Resource Group created."
echo ""

# ─── 3. APPLICATION INSIGHTS ─────────────────────────────────────────
echo "► Step 3: Creating Application Insights: $APP_INSIGHTS_NAME"
az monitor app-insights component create \
  --app "$APP_INSIGHTS_NAME" \
  --location "$LOCATION" \
  --resource-group "$RESOURCE_GROUP" \
  --kind web \
  --application-type web \
  --tags Project=EmployeeManagement Week=6

AI_CONNECTION_STRING=$(az monitor app-insights component show \
  --app "$APP_INSIGHTS_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "connectionString" -o tsv)

echo "  ✓ Application Insights created."
echo "  Connection String: $AI_CONNECTION_STRING"
echo ""

# ─── 4. AZURE SQL SERVER & DATABASE ──────────────────────────────────
echo "► Step 4: Creating Azure SQL Server: $SQL_SERVER_NAME"
az sql server create \
  --name "$SQL_SERVER_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --admin-user "$SQL_ADMIN_USER" \
  --admin-password "$SQL_ADMIN_PASSWORD"

echo "  Configuring SQL Server firewall rules..."
# Allow Azure services
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Get current machine IP and allow it (for running migrations)
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name "AllowDevMachine" \
  --start-ip-address "$MY_IP" \
  --end-ip-address "$MY_IP"

echo "  Creating SQL Database: $SQL_DB_NAME"
az sql db create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name "$SQL_DB_NAME" \
  --edition "Basic" \
  --capacity 5 \
  --max-size 2GB \
  --backup-storage-redundancy "Local"

SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Database=${SQL_DB_NAME};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

echo "  ✓ Azure SQL created: ${SQL_SERVER_NAME}.database.windows.net"
echo ""

# ─── 5. AZURE STORAGE ACCOUNT & BLOB CONTAINER ───────────────────────
echo "► Step 5: Creating Storage Account: $STORAGE_ACCOUNT_NAME"
az storage account create \
  --name "$STORAGE_ACCOUNT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku "Standard_LRS" \
  --kind "StorageV2" \
  --access-tier "Hot" \
  --https-only true \
  --min-tls-version "TLS1_2" \
  --allow-blob-public-access false

STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
  --name "$STORAGE_ACCOUNT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "connectionString" -o tsv)

echo "  Creating Blob Container: $BLOB_CONTAINER_NAME"
az storage container create \
  --name "$BLOB_CONTAINER_NAME" \
  --account-name "$STORAGE_ACCOUNT_NAME" \
  --public-access off

echo "  ✓ Storage Account and container created."
echo ""

# ─── 6. AZURE KEY VAULT ───────────────────────────────────────────────
echo "► Step 6: Creating Azure Key Vault: $KEY_VAULT_NAME"
az keyvault create \
  --name "$KEY_VAULT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --enable-rbac-authorization false \
  --enabled-for-deployment true

echo "  Storing secrets in Key Vault..."

# Secret naming: double-dash (--) replaces colon (:) in config key hierarchy
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "ConnectionStrings--DefaultConnection" \
  --value "$SQL_CONNECTION_STRING"

az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "JwtSettings--SecretKey" \
  --value "$JWT_SECRET_KEY"

az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "AzureBlobStorage--ConnectionString" \
  --value "$STORAGE_CONNECTION_STRING"

az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "ApplicationInsights--ConnectionString" \
  --value "$AI_CONNECTION_STRING"

echo "  ✓ Key Vault created and secrets stored."
echo "  Vault URI: https://${KEY_VAULT_NAME}.vault.azure.net/"
echo ""

# ─── 7. APP SERVICE PLAN ──────────────────────────────────────────────
echo "► Step 7: Creating App Service Plan: $APP_SERVICE_PLAN"
az appservice plan create \
  --name "$APP_SERVICE_PLAN" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku "B1" \
  --is-linux

echo "  ✓ App Service Plan created (B1 Linux)."
echo ""

# ─── 8. APP SERVICE (WEB APP) ─────────────────────────────────────────
echo "► Step 8: Creating App Service: $APP_SERVICE_NAME"
az webapp create \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --plan "$APP_SERVICE_PLAN" \
  --runtime "DOTNETCORE:8.0"

echo "  Enabling System-Assigned Managed Identity..."
az webapp identity assign \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP"

PRINCIPAL_ID=$(az webapp identity show \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "principalId" -o tsv)

echo "  Granting Key Vault access to Managed Identity (principalId: $PRINCIPAL_ID)..."
az keyvault set-policy \
  --name "$KEY_VAULT_NAME" \
  --object-id "$PRINCIPAL_ID" \
  --secret-permissions get list

echo "  Configuring App Service settings..."
az webapp config appsettings set \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    "AzureKeyVault__VaultUri=https://${KEY_VAULT_NAME}.vault.azure.net/" \
    "JwtSettings__Issuer=EmployeeManagementAPI" \
    "JwtSettings__Audience=EmployeeManagementClient" \
    "JwtSettings__ExpirationMinutes=60" \
    "AzureBlobStorage__ContainerName=${BLOB_CONTAINER_NAME}"

echo "  Enabling HTTPS-only..."
az webapp update \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --https-only true

echo "  Enabling logging..."
az webapp log config \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --application-logging filesystem \
  --detailed-error-messages true \
  --failed-request-tracing true \
  --web-server-logging filesystem

echo "  ✓ App Service configured."
echo "  App URL: https://${APP_SERVICE_NAME}.azurewebsites.net"
echo ""

# ─── 9. DEPLOY APPLICATION ────────────────────────────────────────────
echo "► Step 9: Building and deploying application..."

cd src/EmployeeService
dotnet publish -c Release -o ./publish

cd publish
zip -r ../deploy.zip .
cd ..

az webapp deployment source config-zip \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_SERVICE_NAME" \
  --src "deploy.zip"

rm -f deploy.zip
cd ../..

echo "  ✓ Application deployed."
echo ""

# ─── 10. RUN EF CORE MIGRATIONS ──────────────────────────────────────
echo "► Step 10: Running EF Core migrations against Azure SQL..."
cd src/EmployeeService

dotnet ef database update \
  --connection "$SQL_CONNECTION_STRING" \
  --no-build

cd ../..
echo "  ✓ Database migrations applied."
echo ""

# ─── 11. VERIFY DEPLOYMENT ───────────────────────────────────────────
echo "► Step 11: Verifying deployment..."
sleep 10  # Wait for app to start

HEALTH_URL="https://${APP_SERVICE_NAME}.azurewebsites.net/health/live"
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL" || echo "000")

if [ "$HTTP_STATUS" = "200" ]; then
  echo "  ✓ Health check passed! API is live."
else
  echo "  ⚠ Health check returned HTTP $HTTP_STATUS. Check logs with:"
  echo "     az webapp log tail --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP"
fi

echo ""
echo "═══════════════════════════════════════════════════════"
echo "  Deployment Complete!"
echo "═══════════════════════════════════════════════════════"
echo ""
echo "  App URL:      https://${APP_SERVICE_NAME}.azurewebsites.net"
echo "  Swagger UI:   https://${APP_SERVICE_NAME}.azurewebsites.net/swagger"
echo "  Health:       https://${APP_SERVICE_NAME}.azurewebsites.net/health"
echo "  Key Vault:    https://${KEY_VAULT_NAME}.vault.azure.net/"
echo "  SQL Server:   ${SQL_SERVER_NAME}.database.windows.net"
echo "  Storage:      https://${STORAGE_ACCOUNT_NAME}.blob.core.windows.net"
echo ""
echo "  Default login: admin / Admin@123"
echo ""
