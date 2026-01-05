# Minikube Deployment Guide for BoltTickets

This guide provides step-by-step instructions to deploy the BoltTickets application to a local Kubernetes cluster using Minikube.

## Prerequisites

- **Minikube**: Install from https://minikube.sigs.k8s.io/docs/start/
- **kubectl**: Install from https://kubernetes.io/docs/tasks/tools/
- **Docker**: For building images
- **Git**: To clone the repository

## Step 1: Clone and Prepare the Repository

```bash
git clone https://github.com/sudipto21in2024/FlashSale.git
cd FlashSale
```

## Step 2: Start Minikube

```bash
minikube start --driver=docker --memory=4096 --cpus=2
```

Enable ingress addon:
```bash
minikube addons enable ingress
```

## Step 3: Build Docker Images

Build the .NET services:
```bash
# Build API
cd src/BoltTickets.API
docker build -t bolttickets/api:latest .

# Build Worker
cd ../BoltTickets.Worker
docker build -t bolttickets/worker:latest .

# Build Frontend
cd ../BoltTickets.Frontend
docker build -t bolttickets/frontend:latest .
```

Load images into Minikube:
```bash
minikube image load bolttickets/api:latest
minikube image load bolttickets/worker:latest
minikube image load bolttickets/frontend:latest
```

## Step 4: Deploy to Kubernetes

Apply all manifests in order:
```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmaps.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/pvcs.yaml
kubectl apply -f k8s/statefulsets.yaml
kubectl apply -f k8s/services.yaml
kubectl apply -f k8s/deployments.yaml
kubectl apply -f k8s/monitoring.yaml
kubectl apply -f k8s/ingress.yaml
```

## Step 5: Verify Deployment

Check pod status:
```bash
kubectl get pods -n bolttickets
```

Check services:
```bash
kubectl get services -n bolttickets
```

Check ingress:
```bash
kubectl get ingress -n bolttickets
```

## Step 6: Access the Application

Update `/etc/hosts` (or `C:\Windows\System32\drivers\etc\hosts` on Windows):
```
127.0.0.1 api.localhost
127.0.0.1 app.localhost
```

Access the applications:
- **Frontend**: http://app.localhost
- **API**: http://api.localhost
- **API Swagger**: http://api.localhost/swagger

## Step 7: Access Monitoring Tools

Get service URLs:
```bash
minikube service list -n bolttickets
```

Or port-forward:
```bash
# Prometheus
kubectl port-forward -n bolttickets svc/prometheus 9090:9090

# Grafana (admin/admin)
kubectl port-forward -n bolttickets svc/grafana 3000:3000

# Jaeger
kubectl port-forward -n bolttickets svc/jaeger 16686:16686
```

Access:
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)
- **Jaeger**: http://localhost:16686

## Step 8: Test the Application

1. Open the frontend at http://app.localhost
2. Make a booking request
3. Check logs: `kubectl logs -n bolttickets deployment/api`
4. Verify traces in Jaeger
5. Check metrics in Grafana dashboard

## Step 9: Troubleshooting

### Common Issues

**Pods not starting**:
```bash
kubectl describe pod <pod-name> -n bolttickets
kubectl logs <pod-name> -n bolttickets
```

**Ingress not working**:
```bash
kubectl get ingress -n bolttickets
minikube tunnel  # May be needed for ingress
```

**Images not found**:
Ensure images are loaded: `minikube image list`

**Database connection**:
Check PostgreSQL pod logs and ensure PVC is bound.

### Useful Commands

```bash
# Delete all resources
kubectl delete namespace bolttickets

# Restart deployment
kubectl rollout restart deployment/api -n bolttickets

# Scale deployment
kubectl scale deployment api --replicas=3 -n bolttickets

# View events
kubectl get events -n bolttickets
```

## Step 10: Cleanup

```bash
minikube delete
```

## Notes

- This setup uses hostPath for persistence; data will be lost on Minikube restart
- For production, use proper storage classes and secrets management
- Adjust resource limits based on your machine capabilities
- The ingress uses self-signed certificates; configure TLS for production