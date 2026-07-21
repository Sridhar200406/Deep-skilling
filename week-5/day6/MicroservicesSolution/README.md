# Week 5 – Day 6: Docker & Kubernetes Deployment

## What's New in Day 6

| Feature | Files |
|---|---|
| Dockerfiles (4 services) | `Docker/Dockerfile.*` |
| Full docker-compose.yml | `docker-compose.yml` (7 containers + networking) |
| Kubernetes namespace | `k8s/namespace.yaml` |
| Kubernetes ConfigMap | `k8s/configmap.yaml` |
| Kubernetes Secrets | `k8s/secrets.yaml` |
| K8s deployments (7) | `k8s/*-deployment.yaml` |
| K8s services (7) | `k8s/*-service.yaml` |
| Updated ocelot.json | Uses Docker container names, not localhost |

---

## Project Structure

```
MicroservicesSolution/
├── Docker/
│   ├── Dockerfile.ApiGateway
│   ├── Dockerfile.AuthenticationService
│   ├── Dockerfile.DepartmentService
│   └── Dockerfile.EmployeeService
├── k8s/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── secrets.yaml
│   ├── sqlserver-deployment.yaml  + sqlserver-service.yaml
│   ├── rabbitmq-deployment.yaml   + rabbitmq-service.yaml
│   ├── redis-deployment.yaml      + redis-service.yaml
│   ├── authentication-deployment.yaml + authentication-service.yaml
│   ├── department-deployment.yaml     + department-service.yaml
│   ├── employee-deployment.yaml       + employee-service.yaml
│   └── api-gateway-deployment.yaml    + api-gateway-service.yaml
├── docker-compose.yml
└── ... (all Day 1-5 files preserved)
```

---

## Key Concepts

### Docker Image
A read-only template built from a Dockerfile. Contains the app binary, runtime, and dependencies.
Each service produces one image: `employee-mgmt/employee-service:latest`

### Docker Container
A running instance of an image. Isolated process with its own filesystem, network, and env vars.
Multiple containers can run from the same image.

### Dockerfile
A text file with instructions to build an image:
- `FROM` — base image (sdk for build, aspnet for runtime)
- `COPY` — copies project files
- `RUN dotnet restore / publish` — builds the app
- `ENTRYPOINT` — the command that runs when the container starts

### Docker Compose
Defines and runs multiple containers together. Our compose file:
- Declares 7 services with container names, ports, env vars, and health checks
- Creates one shared bridge network `microservices-net` so services resolve each other by name
- Uses `depends_on` with `condition: service_healthy` to control startup order

### Kubernetes Pod
The smallest deployable unit in K8s. Contains one or more containers that share a network.
Pods are ephemeral — if one crashes, K8s replaces it.

### Deployment
Manages a set of identical Pods. Declares the desired state (e.g. replicas: 2).
K8s continuously reconciles actual state to match desired state.

### Service
Stable network endpoint for a set of Pods. Types used here:
- `ClusterIP` — internal only (SQL Server, Redis, RabbitMQ)
- `NodePort` — exposes via localhost:30000-30003 for local dev

### ConfigMap
Non-sensitive key-value configuration. Used for service URLs, RabbitMQ host, cache TTLs.

### Secret
Base64-encoded sensitive data. Used for SQL password, JWT secret, RabbitMQ password.
Never commit real secrets to git — use K8s Secret management or a vault in production.

---

## Docker Compose Commands

```bash
# Build all images and start all 7 containers
docker-compose up --build

# Start in background (detached)
docker-compose up --build -d

# View logs from all services
docker-compose logs -f

# View logs from one service
docker-compose logs -f employee-service

# Stop all containers
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v

# Rebuild a single service
docker-compose up --build employee-service

# Check running containers
docker-compose ps
```

---

## Kubernetes Commands

### Prerequisites
- Docker Desktop with Kubernetes enabled, OR Minikube installed
- kubectl installed

### Step 1 — Build images (must be accessible to K8s)

```bash
# If using Docker Desktop Kubernetes (images already in local registry)
docker build -t employee-mgmt/authentication-service:latest -f Docker/Dockerfile.AuthenticationService .
docker build -t employee-mgmt/department-service:latest     -f Docker/Dockerfile.DepartmentService .
docker build -t employee-mgmt/employee-service:latest       -f Docker/Dockerfile.EmployeeService .
docker build -t employee-mgmt/api-gateway:latest            -f Docker/Dockerfile.ApiGateway .

# If using Minikube — load images into Minikube's registry
minikube image load employee-mgmt/authentication-service:latest
minikube image load employee-mgmt/department-service:latest
minikube image load employee-mgmt/employee-service:latest
minikube image load employee-mgmt/api-gateway:latest
```

### Step 2 — Deploy to Kubernetes

```bash
# Create namespace first
kubectl apply -f k8s/namespace.yaml

# Apply ConfigMap and Secrets
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml

# Deploy infrastructure
kubectl apply -f k8s/sqlserver-deployment.yaml
kubectl apply -f k8s/sqlserver-service.yaml
kubectl apply -f k8s/rabbitmq-deployment.yaml
kubectl apply -f k8s/rabbitmq-service.yaml
kubectl apply -f k8s/redis-deployment.yaml
kubectl apply -f k8s/redis-service.yaml

# Wait for infrastructure to be ready
kubectl wait --for=condition=ready pod -l app=sqlserver  -n employee-mgmt --timeout=120s
kubectl wait --for=condition=ready pod -l app=rabbitmq   -n employee-mgmt --timeout=60s
kubectl wait --for=condition=ready pod -l app=redis      -n employee-mgmt --timeout=30s

# Deploy microservices
kubectl apply -f k8s/authentication-deployment.yaml
kubectl apply -f k8s/authentication-service.yaml
kubectl apply -f k8s/department-deployment.yaml
kubectl apply -f k8s/department-service.yaml
kubectl apply -f k8s/employee-deployment.yaml
kubectl apply -f k8s/employee-service.yaml
kubectl apply -f k8s/api-gateway-deployment.yaml
kubectl apply -f k8s/api-gateway-service.yaml
```

### Step 3 — Check status

```bash
# List all pods
kubectl get pods -n employee-mgmt

# List all services
kubectl get services -n employee-mgmt

# List deployments
kubectl get deployments -n employee-mgmt

# Describe a pod (useful for debugging)
kubectl describe pod <pod-name> -n employee-mgmt

# View pod logs
kubectl logs -f deployment/employee-service -n employee-mgmt

# View events
kubectl get events -n employee-mgmt --sort-by='.lastTimestamp'
```

### Step 4 — Delete everything

```bash
# Delete all resources in the namespace
kubectl delete namespace employee-mgmt

# Or delete individual resources
kubectl delete -f k8s/
```

---

## Testing the APIs

### Via Docker Compose (localhost ports)

**1. Register a user**
```http
POST http://localhost:5001/api/auth/register
Content-Type: application/json

{ "firstName":"John", "lastName":"Doe", "email":"john@test.com", "password":"Password1" }
```

**2. Login and get token**
```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{ "email":"john@test.com", "password":"Password1" }
```

**3. Test via API Gateway**
```http
GET  http://localhost:5000/gateway/employees
POST http://localhost:5000/gateway/employees
GET  http://localhost:5000/gateway/departments
Authorization: Bearer <token>
```

**4. Health checks**
```http
GET http://localhost:5002/health
GET http://localhost:5002/health/live
GET http://localhost:5002/health/ready
GET http://localhost:5003/health
```

**5. RabbitMQ Management UI**
```
http://localhost:15672  (guest / guest)
```

### Via Kubernetes (NodePort)

```http
# API Gateway
POST http://localhost:30000/gateway/auth/register
GET  http://localhost:30000/gateway/employees
Authorization: Bearer <token>

# Direct service access
GET http://localhost:30001/api/auth/health      # Auth
GET http://localhost:30002/api/employees/health # Employee
GET http://localhost:30003/api/departments/health # Department
```

---

## Docker Networking

All containers connect to `microservices-net` (bridge network).
Inside this network, containers resolve each other by service name:

| From | To | Connection |
|---|---|---|
| employee-service | department-service | `http://department-service:5003` |
| employee-service | sqlserver | `Server=sqlserver,1433` |
| employee-service | redis | `redis:6379` |
| employee-service | rabbitmq | `rabbitmq:5672` |
| api-gateway | employee-service | `http://employee-service:5002` |
| api-gateway | authentication-service | `http://authentication-service:5001` |
| api-gateway | department-service | `http://department-service:5003` |

**Important:** Never use `localhost` inside containers. Always use the container/service name.
