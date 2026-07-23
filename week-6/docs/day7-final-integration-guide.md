# Week 6 Day 7 — Final Integration Testing & Security Guide

## Overview

This document covers the complete end-to-end testing and security verification performed on Day 7 to ensure the Employee Management microservices application is production-ready.

---

## 1. Authentication Testing

### Test: User Registration
```bash
POST /api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "email": "testuser@company.com",
  "password": "Test@123",
  "fullName": "Test User",
  "role": "Employee"
}
```

**Expected Response**: 200 OK with user created message

### Test: User Login
```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123"
}
```

**Expected Response**: 200 OK with JWT token, expiry, username, role

### Test: Token Verification
```bash
POST /api/auth/verify
Authorization: Bearer <your-jwt-token>
```

**Expected Response**: 200 OK with token validation details

### Test: Role-Based Access
| Role | GET | POST | PUT | DELETE |
|------|-----|------|-----|--------|
| Admin | ✅ | ✅ | ✅ | ✅ |
| Manager | ✅ | ✅ | ✅ | ❌ |
| Employee | ✅ | ❌ | ❌ | ❌ |

---

## 2. Employee CRUD Testing

### Create Employee (Admin/Manager only)
```bash
POST /api/employees
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@company.com",
  "position": "Developer",
  "departmentId": 1,
  "salary": 75000.00,
  "hireDate": "2024-01-15"
}
```

### Get All Employees
```bash
GET /api/employees?pageNumber=1&pageSize=10&sortBy=lastName&isAscending=true
Authorization: Bearer <any-token>
```

### Get Employee by ID
```bash
GET /api/employees/1
Authorization: Bearer <any-token>
```

### Update Employee
```bash
PUT /api/employees/1
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "employeeId": 1,
  "firstName": "John",
  "lastName": "Updated",
  "email": "john.updated@company.com",
  "position": "Senior Developer",
  "departmentId": 1,
  "salary": 85000.00,
  "hireDate": "2024-01-15"
}
```

### Delete Employee (Admin only)
```bash
DELETE /api/employees/1
Authorization: Bearer <admin-token>
```

---

## 3. Azure Service Testing

### Azure SQL Database
```bash
# Verify database connectivity via health check
GET /health/ready

# Expected: sql-server status = Healthy
```

### Azure Blob Storage
```bash
# Upload document
POST /api/employees/1/documents/upload
Authorization: Bearer <admin-token>
Content-Type: multipart/form-data
# Attach file in form data

# List documents
GET /api/employees/1/documents
Authorization: Bearer <any-token>

# Download document
GET /api/employees/1/documents/1/download
Authorization: Bearer <any-token>

# Delete document
DELETE /api/employees/1/documents/1
Authorization: Bearer <admin-token>
```

### Azure Functions
```bash
# HTTP Trigger — test notification
POST <function-app-url>/api/EmployeeNotification
Content-Type: application/json

{
  "employeeId": 1,
  "eventType": "Created",
  "message": "New employee onboarded"
}

# Timer Trigger — runs on schedule (verify in Azure Portal → Function App → Monitor)
# Blob Trigger — upload a file to the blob container and verify function execution
# Service Bus Trigger — publish a message and verify function processes it
```

### Azure Service Bus
```bash
# Verify by performing CRUD operations — messages are published automatically
# Check Azure Portal → Service Bus → Topics → employee-events → Subscriptions

# Verify dead-letter queue
# Azure Portal → Service Bus → Queues → employee-events-dlq
```

### Redis Cache
```bash
# First request (cache miss — fetches from DB)
GET /api/employees
# Response header: X-Cache: MISS

# Second request (cache hit — served from Redis)
GET /api/employees
# Response header: X-Cache: HIT

# After creating/updating employee, cache is invalidated
POST /api/employees
# Next GET will be a cache miss again
```

---

## 4. Health Checks

```bash
# Full health report
GET /health
# Returns: all checks (sql-server, self, azure-blob-storage)

# Liveness probe (is the API running?)
GET /health/live
# Returns: self check only

# Readiness probe (are dependencies ready?)
GET /health/ready
# Returns: sql-server, azure-blob-storage checks
```

---

## 5. Monitoring Verification

### Application Insights
1. Azure Portal → Application Insights → Live Metrics
2. Send a few requests to the API
3. Verify requests appear in real-time

### Log Analytics (KQL Queries)
```kql
// Failed requests in last 24 hours
requests
| where success == false
| where timestamp > ago(24h)
| summarize count() by name, resultCode
| order by count_ desc

// Average response time by endpoint
requests
| where timestamp > ago(1h)
| summarize avg(duration) by name
| order by avg_duration desc

// All exceptions
exceptions
| where timestamp > ago(24h)
| project timestamp, type, outerMessage
| order by timestamp desc

// Custom traces from Serilog
traces
| where timestamp > ago(1h)
| where message contains "EmployeeService"
| project timestamp, message, severityLevel
| order by timestamp desc
```

### Serilog Logs
- Console: visible in Docker/terminal output
- File: `logs/employee-service-YYYY-MM-DD.log`
- Application Insights: traces table in Log Analytics

---

## 6. CI/CD Pipeline Verification

### Trigger the Pipeline
```bash
# Make a small code change (e.g., update a comment)
git add .
git commit -m "feat: Day 7 final integration verification"
git push origin main
```

### Monitor Pipeline
1. GitHub → Actions → "Employee Management API – CI/CD"
2. Verify all steps pass:
   - ✅ Checkout
   - ✅ Setup .NET 8
   - ✅ Restore packages
   - ✅ Build
   - ✅ Run tests
   - ✅ Docker build
   - ✅ Push to Azure Container Registry
   - ✅ Deploy to Azure Container Apps
   - ✅ Health check verification

---

## 7. Security Checklist

### Secrets Management
- [x] `JwtSettings:SecretKey` → Azure Key Vault
- [x] `ConnectionStrings:DefaultConnection` → Azure Key Vault
- [x] `AzureBlobStorage:ConnectionString` → Azure Key Vault
- [x] `ApplicationInsights:ConnectionString` → Azure Key Vault
- [x] No secrets in `appsettings.json` or `appsettings.Production.json`
- [x] No secrets in Docker images
- [x] GitHub Secrets used for CI/CD credentials

### Network Security
- [x] HTTPS-only enforced on App Service/Container Apps
- [x] Azure SQL firewall — only Azure services + specific IPs
- [x] Blob container — private access only (no public URLs)
- [x] CORS — restricted origins in production

### Application Security
- [x] JWT tokens with expiration (60 minutes default)
- [x] Role-based authorization (Admin, Manager, Employee)
- [x] Passwords hashed with BCrypt (never plaintext)
- [x] Input validation (FluentValidation + Data Annotations)
- [x] File upload — content type and size validation
- [x] Global exception handling (no stack traces in production)
- [x] Request rate limiting via API Management

### Infrastructure Security
- [x] Managed Identity for Key Vault access (no stored credentials)
- [x] Azure Container Registry — private registry
- [x] GitHub Actions — secrets stored securely
- [x] .gitignore excludes sensitive files

---

## 8. Troubleshooting

| Issue | Solution |
|-------|----------|
| 401 Unauthorized | Re-login to get a fresh JWT token |
| 403 Forbidden | Verify your role has access to the endpoint |
| 500 Internal Server Error | Check Application Insights → Failures blade |
| Database connection failed | Verify SQL firewall rules, check Key Vault secret |
| Blob upload failed | Verify storage connection string, check container exists |
| Redis timeout | Check Redis connectivity, falls back to in-memory cache |
| CI/CD failed | Check GitHub Actions logs for the failed step |
| Container won't start | Check Azure Container Apps → Console → Logs |
| Key Vault 403 | Verify Managed Identity has access policy configured |
| Health check failing | Check /health endpoint for specific component status |
