# Week 6 Day 4 – Azure Service Bus & Asynchronous Messaging

## 📋 Overview

Day 4 extends the Employee Management system with **Azure Service Bus** for reliable, asynchronous event-driven communication between the API, microservices, and Azure Functions.

---

## 🎯 What is Azure Service Bus?

Azure Service Bus is a **fully managed enterprise message broker** with message queues and publish-subscribe topics.

**Key features:**
- **Guaranteed delivery** — messages persist until consumed
- **At-least-once delivery** — messages won't be lost
- **Dead-letter queues** — automatically handle failures
- **Sessions** — ordered message processing
- **Transactions** — group operations atomically
- **Duplicate detection** — prevent processing the same message twice
- **Message TTL** — automatic expiration
- **Managed Identity** — no connection strings needed

---

## 🆚 Azure Service Bus vs RabbitMQ

| Feature | Azure Service Bus | RabbitMQ |
|---------|------------------|----------|
| **Hosting** | Fully managed (PaaS) | Self-hosted or managed service |
| **Setup** | Zero infrastructure | Install + configure broker |
| **Scaling** | Auto-scales | Manual cluster setup |
| **High availability** | Built-in (99.9% SLA) | Manual replication config |
| **Monitoring** | Application Insights | Prometheus + custom |
| **Authentication** | Managed Identity, SAS | Username/password |
| **Dead-letter queue** | Built-in | Manual config |
| **Cost** | Pay-per-operation | Server costs |
| **Max message size** | 256 KB (Standard), 100 MB (Premium) | Configurable |
| **Protocols** | AMQP, HTTP/REST | AMQP, MQTT, STOMP |

**When to use Service Bus over RabbitMQ:**
- Azure-native cloud applications
- Don't want to manage message broker infrastructure
- Need enterprise features (sessions, transactions, duplicate detection)
- Want seamless Azure integration (Managed Identity, Key Vault, App Insights)

**When RabbitMQ might be better:**
- Multi-cloud or on-premises
- Need advanced routing (topic exchanges with wildcards)
- Already have RabbitMQ expertise
- Want more control over broker configuration

---

## 🏗️ Core Concepts

### Queue
A **first-in, first-out (FIFO)** buffer that holds messages until consumed.

```
Producer → [Queue] → Consumer
             ↓ (messages stored)
         (FIFO order)
```

**Use case:** Point-to-point communication
- Task processing (order fulfillment, email sending)
- Load leveling (buffer traffic spikes)
- Decoupling services (async processing)

**Characteristics:**
- Each message consumed by **one** consumer
- Messages persist until consumed (or TTL expires)
- Competing consumers pattern (multiple workers pull from same queue)

---

### Topic + Subscription
A **publish-subscribe** pattern where multiple consumers get copies of each message.

```
Publisher → [Topic: EmployeeEvents]
                 ↓
       ┌─────────┼─────────┐
       ▼         ▼         ▼
   [Sub: Email] [Sub: Log] [Sub: Analytics]
       ↓         ↓         ▼
  Email Svc  Log Svc  Analytics Svc
```

**Use case:** Broadcast events to multiple interested parties
- Event notifications (employee created → send email, log audit, update cache)
- Fan-out architecture (one event, many handlers)
- Loose coupling (publishers don't know about subscribers)

**Characteristics:**
- Each subscription gets a **copy** of every message
- Subscriptions can have **filters** (SQL-like expressions)
- Add/remove subscriptions without changing publisher

---

### Publisher
Sends messages to a queue or topic.

```csharp
await serviceBusClient.CreateSender("employee-events")
    .SendMessageAsync(new ServiceBusMessage(jsonPayload));
```

**Best practices:**
- Use batching for high throughput
- Set message properties (ContentType, MessageId, CorrelationId)
- Handle transient failures with retry policies
- Don't block API responses — fire-and-forget or async

---

### Consumer
Receives and processes messages from a queue or subscription.

```csharp
var processor = serviceBusClient.CreateProcessor("employee-events", "email-subscription");
processor.ProcessMessageAsync += async args =>
{
    var body = args.Message.Body.ToString();
    var evt = JsonSerializer.Deserialize<EmployeeCreatedEvent>(body);
    // Process event...
    await args.CompleteMessageAsync(args.Message);
};
await processor.StartProcessingAsync();
```

**Processing options:**
- **Complete** — message successfully processed, remove from queue
- **Abandon** — processing failed, return to queue for retry
- **DeadLetter** — unprocessable message, move to dead-letter queue
- **Defer** — skip for now, process later

---

### Dead-Letter Queue (DLQ)
A sub-queue where messages go when they can't be processed.

**Automatically dead-lettered if:**
- Max delivery count exceeded (default: 10 retries)
- Message TTL expired
- Consumer explicitly calls `DeadLetterMessageAsync()`

**Why it matters:**
- Poison messages don't block the queue
- Failed messages preserved for investigation
- Can resubmit after fixing the issue

**Inspecting DLQ:**
```bash
# List dead-letter messages
az servicebus topic subscription list-dead-letter-messages \
  --resource-group Week6-EmployeeManagement-RG \
  --namespace-name week6-servicebus \
  --topic-name employee-events \
  --name email-subscription

# Peek a message
az servicebus topic subscription dead-letter peek \
  --resource-group Week6-EmployeeManagement-RG \
  --namespace-name week6-servicebus \
  --topic-name employee-events \
  --name email-subscription
```

---

### Service Bus Trigger (Azure Functions)
Azure Functions can be triggered by Service Bus messages.

```csharp
[Function("ProcessEmployeeCreated")]
public async Task Run(
    [ServiceBusTrigger("employee-events", "email-subscription", Connection = "ServiceBusConnection")]
    ServiceBusReceivedMessage message,
    ServiceBusMessageActions messageActions)
{
    var evt = JsonSerializer.Deserialize<EmployeeCreatedEvent>(message.Body.ToString());
    // Process...
    await messageActions.CompleteMessageAsync(message);
}
```

**Benefits:**
- Auto-scaling (Azure adds function instances as queue depth grows)
- Built-in retry logic
- Dead-letter handling
- No need to manage consumer loops

---

## 🔧 Implementation Architecture

```
Employee API
     │
     ├─ Create Employee
     │    └─► Publish EmployeeCreatedEvent → [Topic: employee-events]
     │                                              ↓
     │                                        ┌─────┼─────┐
     │                                        ▼     ▼     ▼
     │                                     [Sub1] [Sub2] [Sub3]
     │                                        ↓     ↓     ▼
     ├─ Update Employee                  Function Function Function
     │    └─► Publish EmployeeUpdatedEvent  (Email) (Audit) (Cache)
     │
     └─ Delete Employee
          └─► Publish EmployeeDeletedEvent

Azure Functions (Service Bus Triggers)
     ├─ ProcessEmployeeCreated → Send welcome email
     ├─ ProcessEmployeeUpdated → Log audit trail
     └─ ProcessEmployeeDeleted → Clean up related data
```

---

## 📦 What Was Built (Day 4)

### 1. Service Bus Publisher (`ServiceBusPublisher.cs`)
- Publishes events to `employee-events` topic
- Connection string from Key Vault
- Fire-and-forget (doesn't block API)
- Logs successes/failures to App Insights

### 2. Extended Event Models (`MessageEvent.cs`)
- Added metadata fields: `Position`, `DepartmentName`, `Salary`, etc.
- All events implement `IEmployeeEvent` interface
- JSON serialization optimized

### 3. Service Bus Trigger Functions (`EmployeeEventFunctions.cs`)
- Three functions in one file (same topic, different subscriptions)
- `ProcessEmployeeCreated` → Email notification
- `ProcessEmployeeUpdated` → Audit logging
- `ProcessEmployeeDeleted` → Cleanup tasks

### 4. Dead-Letter Handler (`DeadLetterFunction.cs`)
- Timer trigger (runs every 10 minutes)
- Reads DLQ messages
- Logs details
- Optionally resubmits or archives

### 5. Integration in Employee API
- `EmployeeAppService` publishes events after CRUD
- Non-blocking (fire-and-forget)
- Failures logged, never propagated to caller

---

## 🚀 Local Development

### Using Service Bus Emulator
Azure doesn't provide an official Service Bus emulator. Options:

**Option A: Use Azure Service Bus (Free Tier)**
```bash
# Create namespace
az servicebus namespace create \
  --name week6-servicebus-dev \
  --resource-group Week6-EmployeeManagement-RG \
  --location eastus \
  --sku Basic

# Get connection string
az servicebus namespace authorization-rule keys list \
  --resource-group Week6-EmployeeManagement-RG \
  --namespace-name week6-servicebus-dev \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv
```

**Option B: Mock for unit tests**
```csharp
// Use a mock IServiceBusPublisher in tests
services.AddScoped<IServiceBusPublisher, MockServiceBusPublisher>();
```

### Running Functions with Service Bus Trigger
```bash
cd src/AzureFunctions

# Set connection string in local.settings.json
# "ServiceBusConnection": "Endpoint=sb://..."

func start

# Functions will automatically poll the Service Bus subscription
```

---

## 🧪 Testing End-to-End

### 1. Create an employee via API
```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}' \
  | jq -r '.data.token')

# Create employee
curl -X POST http://localhost:5000/api/employees \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName":"Sarah","lastName":"Connor",
    "email":"sarah@test.com","position":"Engineer",
    "salary":85000,"departmentId":1
  }'

# ✅ API publishes EmployeeCreatedEvent to Service Bus topic
```

### 2. Verify in Azure Portal
```
Azure Portal
→ Service Bus Namespace
→ Topics → employee-events
→ Check "Messages" (should show 1 sent)
→ Click subscription → Check "Active Messages" (should show 1)
```

### 3. Azure Function processes the event
```bash
# Check function logs
az functionapp log tail \
  --name week6-employee-functions \
  --resource-group Week6-EmployeeManagement-RG

# You should see:
# [ProcessEmployeeCreated] Received EmployeeCreatedEvent for Employee 5
# [ProcessEmployeeCreated] Sent welcome email to sarah@test.com
```

---

## 🔐 Security Configuration

### Using Managed Identity (Production)
```bash
# Enable Managed Identity on App Service
az webapp identity assign \
  --name week6-employee-api \
  --resource-group Week6-EmployeeManagement-RG

# Grant Service Bus Data Sender role
PRINCIPAL_ID=$(az webapp identity show \
  --name week6-employee-api \
  --resource-group Week6-EmployeeManagement-RG \
  --query principalId -o tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Azure Service Bus Data Sender" \
  --scope "/subscriptions/<SUB_ID>/resourceGroups/Week6-EmployeeManagement-RG/providers/Microsoft.ServiceBus/namespaces/week6-servicebus"

# Grant Function App Data Receiver role
FUNC_PRINCIPAL_ID=$(az functionapp identity show \
  --name week6-employee-functions \
  --resource-group Week6-EmployeeManagement-RG \
  --query principalId -o tsv)

az role assignment create \
  --assignee $FUNC_PRINCIPAL_ID \
  --role "Azure Service Bus Data Receiver" \
  --scope "/subscriptions/<SUB_ID>/resourceGroups/Week6-EmployeeManagement-RG/providers/Microsoft.ServiceBus/namespaces/week6-servicebus"
```

Then use connection string with Managed Identity:
```
ServiceBusConnection = <namespace>.servicebus.windows.net
```

---

## 📊 Monitoring with Application Insights

### KQL Queries

**Messages sent by API:**
```kql
dependencies
| where type == "Azure Service Bus"
| where name contains "employee-events"
| where timestamp > ago(24h)
| project timestamp, target, success, duration, resultCode
| order by timestamp desc
```

**Messages received by Functions:**
```kql
traces
| where message contains "ProcessEmployee"
| where timestamp > ago(24h)
| project timestamp, message, severityLevel, operation_Name
| order by timestamp desc
```

**Dead-letter messages:**
```kql
traces
| where message contains "DeadLetter"
| where timestamp > ago(7d)
| project timestamp, message, customDimensions
| order by timestamp desc
```

**Average processing duration:**
```kql
requests
| where operation_Name startswith "ProcessEmployee"
| summarize avg(duration), count() by operation_Name
| order by avg_duration desc
```

---

## 🛠️ Common Issues & Fixes

### "Unauthorized access. 'Send' claim(s) are required"
→ Connection string missing Send permission or Managed Identity not granted role.

### "The messaging entity could not be found"
→ Topic or subscription doesn't exist. Create via Azure CLI or Portal.

### Messages go to DLQ immediately
→ Check message deserialization. Likely JSON mismatch.

### Function trigger not firing
→ Verify `ServiceBusConnection` in app settings points to correct namespace.

---

## 📚 Key Takeaways

✅ Service Bus = reliable, managed pub/sub messaging  
✅ Topics = broadcast to multiple subscribers  
✅ Dead-letter queue = automatic failure handling  
✅ Service Bus Trigger = serverless event processing  
✅ Managed Identity = no connection strings in code  
✅ Application Insights = full message tracing  

---

## Next Steps

- Add message filters to subscriptions (SQL expressions)
- Implement sessions for ordered processing
- Add duplicate detection
- Configure auto-forwarding between queues
- Set up geo-replication for disaster recovery
