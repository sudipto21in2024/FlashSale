# BoltTickets Kubernetes Deployment Runbook

## Overview
This runbook provides step-by-step procedures for deploying the BoltTickets application to a Kubernetes cluster using Minikube for local development and testing.

## Prerequisites

### System Requirements
- Windows 10/11 with WSL2 or native Docker support
- At least 8GB RAM available
- At least 20GB free disk space
- Docker Desktop installed and running
- kubectl installed
- Minikube installed

### Software Dependencies
- .NET 9.0 SDK
- Node.js 18+
- Docker
- Minikube
- kubectl

### Network Requirements
- Internet access for pulling Docker images
- Local network access for Minikube

## Pre-Deployment Checklist

### 1. Environment Verification
- [ ] Docker Desktop is running
- [ ] Minikube is installed: `minikube version`
- [ ] kubectl is installed: `kubectl version --client`
- [ ] Docker has sufficient resources allocated (4GB RAM minimum)

### 2. Code and Configuration
- [ ] All source code is committed and pushed to repository
- [ ] Configuration files are updated for the target environment
- [ ] Secrets are properly configured in `k8s/secrets.yaml`
- [ ] ConfigMaps are updated in `k8s/configmaps.yaml`

### 3. Infrastructure Dependencies
- [ ] External services (Kafka, Redis, PostgreSQL) are accessible
- [ ] Network policies allow communication between services
- [ ] Ingress controller is available (NGINX Ingress for Minikube)

### 4. Resource Availability
- [ ] Sufficient CPU and memory for all pods
- [ ] Persistent volume claims can be satisfied
- [ ] Image registry is accessible

## Deployment Steps

### Phase 1: Environment Setup

1. **Start Minikube**
   ```bash
   minikube start --driver=docker --memory=4096 --cpus=2
   ```

2. **Enable Ingress Addon**
   ```bash
   minikube addons enable ingress
   ```

3. **Verify Minikube Status**
   ```bash
   minikube status
   kubectl get nodes
   ```

### Phase 2: Image Preparation (Optional but Recommended)

1. **Pre-pull Base Images** (to cache locally)
   ```bash
   # Run the pull-images.bat script
   pull-images.bat
   ```

2. **Build Application Images**
   ```bash
   # Run the deployment script
   k8s/deploy.bat
   ```

### Phase 3: Kubernetes Deployment

1. **Apply Namespace**
   ```bash
   kubectl apply -f k8s/namespace.yaml
   ```

2. **Apply Configuration**
   ```bash
   kubectl apply -f k8s/configmaps.yaml
   kubectl apply -f k8s/secrets.yaml
   ```

3. **Apply Storage**
   ```bash
   kubectl apply -f k8s/pvcs.yaml
   kubectl apply -f k8s/statefulsets.yaml
   ```

4. **Apply Services and Deployments**
   ```bash
   kubectl apply -f k8s/services.yaml
   kubectl apply -f k8s/deployments.yaml
   ```

5. **Apply Ingress and Monitoring**
   ```bash
   kubectl apply -f k8s/ingress.yaml
   kubectl apply -f k8s/monitoring.yaml
   ```

## Post-Deployment Verification

### 1. Pod Status
```bash
kubectl get pods -n bolttickets
kubectl get deployments -n bolttickets
kubectl get services -n bolttickets
```

### 2. Health Checks
```bash
# Check pod readiness
kubectl wait --for=condition=available --timeout=300s deployment/api -n bolttickets
kubectl wait --for=condition=available --timeout=300s deployment/worker -n bolttickets
kubectl wait --for=condition=available --timeout=300s deployment/frontend -n bolttickets
```

### 3. Application Access
- Frontend: http://app.localhost
- API: http://api.localhost
- API Health: http://api.localhost/health

### 4. Monitoring Setup
```bash
# Port forward monitoring services
kubectl port-forward -n bolttickets svc/prometheus 9090:9090
kubectl port-forward -n bolttickets svc/grafana 3000:3000
kubectl port-forward -n bolttickets svc/jaeger 16686:16686
```

## Monitoring and Observability

### Health Endpoints
- API Health: `GET /health`
- Worker Health: `kubectl exec <worker-pod> -- dotnet BoltTickets.Worker.dll --health`

### Logs
```bash
# View logs
kubectl logs -f deployment/api -n bolttickets
kubectl logs -f deployment/worker -n bolttickets
kubectl logs -f deployment/frontend -n bolttickets

# View events
kubectl get events -n bolttickets --sort-by=.metadata.creationTimestamp
```

### Metrics
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)
- Jaeger: http://localhost:16686

## Troubleshooting

### Common Issues

#### Minikube Issues
```bash
# Reset Minikube
minikube delete
minikube start --driver=docker --memory=4096 --cpus=2

# Check Minikube logs
minikube logs
```

#### Image Pull Issues
```bash
# Check image status
kubectl describe pod <pod-name> -n bolttickets

# Manually pull images
docker pull bolttickets/api:latest
docker pull bolttickets/worker:latest
docker pull bolttickets/frontend:latest

# Load images into Minikube
minikube image load bolttickets/api:latest
minikube image load bolttickets/worker:latest
minikube image load bolttickets/frontend:latest
```

#### Pod Startup Issues
```bash
# Check pod events
kubectl describe pod <pod-name> -n bolttickets

# Check resource usage
kubectl top pods -n bolttickets

# Check configuration
kubectl get configmap -n bolttickets
kubectl get secret -n bolttickets
```

#### Network Issues
```bash
# Check ingress
kubectl get ingress -n bolttickets
kubectl describe ingress bolttickets-ingress -n bolttickets

# Check services
kubectl get endpoints -n bolttickets

# Update hosts file (Windows)
# Add to C:\Windows\System32\drivers\etc\hosts:
# 127.0.0.1 app.localhost
# 127.0.0.1 api.localhost
```

### Health Check Failures
```bash
# Manual health check
kubectl exec <worker-pod> -n bolttickets -- dotnet BoltTickets.Worker.dll --health

# Check API health
curl http://api.localhost/health
```

## Rollback Procedures

### Emergency Rollback
```bash
# Scale down problematic deployment
kubectl scale deployment api --replicas=0 -n bolttickets

# Rollback to previous version
kubectl rollout undo deployment/api -n bolttickets

# Check rollout status
kubectl rollout status deployment/api -n bolttickets
```

### Clean Redeployment
```bash
# Delete all resources
kubectl delete namespace bolttickets

# Wait for cleanup
kubectl get namespaces

# Redeploy
k8s/deploy.bat
```

## Maintenance Tasks

### Log Rotation
- Application logs are configured with rolling intervals
- Monitor disk usage on persistent volumes

### Backup
- Database backups (external to this runbook)
- Configuration backups: `kubectl get configmap,secret -n bolttickets -o yaml > backup.yaml`

### Updates
1. Update application code
2. Build new images with updated tags
3. Update deployment YAMLs with new image tags
4. Apply changes: `kubectl apply -f k8s/deployments.yaml`
5. Monitor rollout: `kubectl rollout status deployment/api -n bolttickets`

## Security Considerations

- Secrets are stored in Kubernetes secrets (base64 encoded)
- Consider using external secret management (Vault, AWS Secrets Manager)
- Network policies should be implemented for production
- RBAC should be configured for access control

## Performance Tuning

### Resource Limits
- Monitor resource usage: `kubectl top pods -n bolttickets`
- Adjust resource requests/limits in deployment YAMLs as needed

### Scaling
```bash
# Scale API deployment
kubectl scale deployment api --replicas=3 -n bolttickets

# Horizontal Pod Autoscaler (if configured)
kubectl get hpa -n bolttickets
```

## Support Contacts

- Development Team: [team@company.com]
- Infrastructure Team: [infra@company.com]
- On-call Engineer: [oncall@company.com]

## Version History

- v1.0: Initial deployment runbook
- Date: [Current Date]
- Author: [Your Name]