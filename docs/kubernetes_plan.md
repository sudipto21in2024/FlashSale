# Kubernetes Deployment Plan for BoltTickets with Minikube

## Overview
This plan outlines the migration of the BoltTickets system from Docker Compose to Kubernetes using Minikube for local development. The system consists of microservices (API, Worker, Frontend), databases (PostgreSQL), caches (Redis), messaging (Kafka), and observability tools (Jaeger, Prometheus, Grafana). The goal is to create a production-ready K8s setup while maintaining local development simplicity.

## Current System Analysis
- **Services**: 11 services in docker-compose.yml (API, Worker, Frontend, PostgreSQL, Redis, Kafka, Zookeeper, Jaeger, Prometheus, Grafana, PgAdmin)
- **Architecture**: Clean Architecture with .NET services, React frontend, event-driven messaging
- **Dependencies**: Inter-service communication via Kafka/Redis, external access via ports
- **State**: PostgreSQL and Redis require persistent storage
- **Observability**: Integrated OpenTelemetry, Prometheus scraping, Jaeger tracing

## Recommended Kubernetes Architecture
Use a single namespace (`bolttickets`) for simplicity in Minikube. Separate concerns with labels and selectors. Leverage K8s primitives for scalability, self-healing, and declarative management.

### Namespace Structure
- **bolttickets**: Main namespace for all services
- Optional: `bolttickets-monitoring` for observability if scaling

### Pod Design
- **Stateless Pods**: API, Worker, Frontend (horizontal scaling)
- **Stateful Pods**: PostgreSQL, Redis, Kafka (with PVCs)
- **Singleton Pods**: Jaeger, Prometheus, Grafana (single replicas)

## Detailed Implementation Plan

### 1. Container Images and Registries
- **Build Images**: Use multi-stage Dockerfiles for .NET services (base on `mcr.microsoft.com/dotnet/aspnet:9.0` for runtime)
- **Registry**: Push to Docker Hub or local Minikube registry
- **Frontend**: Build React app with Nginx (`nginx:alpine`)
- **Infrastructure**: Use official images (postgres:15, redis:alpine, etc.)
- **Tagging**: Semantic versioning (e.g., `bolttickets/api:v1.0.0`)

### 2. Stateful Services Manifests
- **PostgreSQL**:
  - Use StatefulSet with 1 replica
  - PVC for `/var/lib/postgresql/data`
  - ConfigMap for `postgresql.conf` (enable logical replication for CDC)
  - Secret for credentials
  - Service: ClusterIP
- **Redis**:
  - Deployment/StatefulSet with PVC
  - ConfigMap for redis.conf
  - Service: ClusterIP
- **Kafka/Zookeeper**:
  - StatefulSets with persistent volumes
  - ConfigMaps for broker/zookeeper configs
  - Services: ClusterIP with headless for internal communication

### 3. Stateless Services Manifests
- **API**:
  - Deployment with rolling updates
  - Replicas: 2-3 for HA
  - ConfigMap for appsettings.json
  - Secret for connection strings
  - Service: ClusterIP + Ingress for external access
  - Readiness/Liveness probes: `/health` endpoint
- **Worker**:
  - Deployment with replicas: 1-2
  - Same configs as API
  - Probes: Custom health check
- **Frontend**:
  - Deployment with Nginx
  - Replicas: 1-2
  - Ingress for routing

### 4. Networking and Ingress
- **Services**: ClusterIP for internal, LoadBalancer/NodePort for external in Minikube
- **Ingress**: NGINX Ingress Controller
  - Routes: `api.localhost` → API, `app.localhost` → Frontend
  - TLS: Self-signed certs for local dev
- **Internal DNS**: K8s service discovery (e.g., `postgres.bolttickets.svc.cluster.local`)

### 5. Configuration Management
- **ConfigMaps**:
  - Database configs, Kafka topics, app settings
  - Environment-specific overrides
- **Secrets**:
  - DB passwords, API keys, certificates
  - Use `kubectl create secret` or Sealed Secrets for production
- **Environment Variables**: Injected via ConfigMaps/Secrets

### 6. Persistent Storage
- **PVCs**: Use `hostPath` in Minikube for local persistence
- **StorageClass**: Default Minikube class
- **Backup**: No automated backups in local setup; document manual procedures

### 7. Observability in K8s
- **Prometheus**: Deploy via Helm or manifests, scrape K8s metrics + app metrics
- **Grafana**: Deploy with dashboards, import existing JSON
- **Jaeger**: Deploy operator or manifests for tracing
- **Logging**: Use Fluent Bit or similar for centralized logging
- **Monitoring**: K8s metrics server, custom alerts

### 8. Security Considerations
- **RBAC**: ServiceAccounts for pods accessing K8s API
- **Network Policies**: Restrict pod-to-pod communication
- **Secrets Management**: Avoid plain text, use encrypted secrets
- **Image Security**: Scan images, use non-root users

### 9. CI/CD Pipeline
- **Build**: GitHub Actions to build/push images on commit
- **Deploy**: ArgoCD or Flux for GitOps
- **Testing**: Helm tests, integration tests in K8s

### 10. Local Minikube Setup
- **Prerequisites**: Minikube, kubectl, Docker
- **Start**: `minikube start --driver=docker`
- **Enable Ingress**: `minikube addons enable ingress`
- **Deploy**: `kubectl apply -f k8s/`
- **Access**: `minikube service` or Ingress URLs
- **Debug**: `kubectl logs`, `kubectl exec`

### 11. Migration Steps
1. Containerize remaining services if needed
2. Create base manifests (namespaces, secrets)
3. Deploy infrastructure (DB, messaging)
4. Deploy apps with dependencies
5. Configure ingress and monitoring
6. Test end-to-end functionality
7. Optimize resources and scaling

## Potential Challenges and Mitigations
- **Resource Constraints**: Minikube limited RAM/CPU; reduce replicas or use lighter images
- **Networking**: Minikube DNS issues; use `minikube tunnel`
- **Persistence**: Data loss on restarts; use PVCs
- **Complexity**: Start simple, add features incrementally
- **Local vs Prod**: Document differences (e.g., LoadBalancer vs Ingress)

## Estimated Effort
- Planning/Documentation: 1-2 days
- Manifest Creation: 3-5 days
- Testing/Debugging: 2-3 days
- Total: 1-2 weeks for MVP

## Next Steps
Review plan, refine based on feedback. Proceed to Code mode for implementation if approved.