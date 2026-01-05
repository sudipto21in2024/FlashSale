#!/bin/bash
# BoltTickets Minikube Deployment Script

set -e

echo "Starting Minikube..."
minikube start --driver=docker --memory=4096 --cpus=2
minikube addons enable ingress

echo "Building Docker images..."
docker build -t bolttickets/api:latest ../src/BoltTickets.API
docker build -t bolttickets/worker:latest ../src/BoltTickets.Worker
docker build -t bolttickets/frontend:latest ../src/BoltTickets.Frontend

echo "Loading images into Minikube..."
minikube image load bolttickets/api:latest
minikube image load bolttickets/worker:latest
minikube image load bolttickets/frontend:latest

echo "Deploying to Kubernetes..."
kubectl apply -f namespace.yaml
kubectl apply -f configmaps.yaml
kubectl apply -f secrets.yaml
kubectl apply -f pvcs.yaml
kubectl apply -f statefulsets.yaml
kubectl apply -f services.yaml
kubectl apply -f deployments.yaml
kubectl apply -f monitoring.yaml
kubectl apply -f ingress.yaml

echo "Waiting for deployments to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/api -n bolttickets
kubectl wait --for=condition=available --timeout=300s deployment/worker -n bolttickets
kubectl wait --for=condition=available --timeout=300s deployment/frontend -n bolttickets

echo "Deployment complete!"
echo "Access the application:"
echo "  Frontend: http://app.localhost"
echo "  API: http://api.localhost"
echo "  Prometheus: kubectl port-forward -n bolttickets svc/prometheus 9090:9090"
echo "  Grafana: kubectl port-forward -n bolttickets svc/grafana 3000:3000 (admin/admin)"
echo "  Jaeger: kubectl port-forward -n bolttickets svc/jaeger 16686:16686"