@echo off
setlocal enabledelayedexpansion
REM BoltTickets Minikube Deployment Batch Script for Windows

echo Starting Minikube...
minikube start --driver=docker --memory=4096 --cpus=2
if %errorlevel% neq 0 (
    echo Failed to start Minikube. Error: %errorlevel%
    goto :error
)

minikube addons enable ingress
if %errorlevel% neq 0 (
    echo Failed to enable ingress. Error: %errorlevel%
    goto :error
)

echo Building Docker images...
pushd "%~dp0..\src"
docker build -t bolttickets/api:latest -f BoltTickets.API/Dockerfile .
if %errorlevel% neq 0 (
    echo Failed to build API image. Error: %errorlevel%
    popd
    goto :error
)

docker build -t bolttickets/worker:latest -f BoltTickets.Worker/Dockerfile .
if %errorlevel% neq 0 (
    echo Failed to build Worker image. Error: %errorlevel%
    popd
    goto :error
)

docker build -t bolttickets/frontend:latest -f BoltTickets.Frontend/Dockerfile .
if %errorlevel% neq 0 (
    echo Failed to build Frontend image. Error: %errorlevel%
    popd
    goto :error
)
popd

echo Deploying to Kubernetes...
kubectl apply -f namespace.yaml
if %errorlevel% neq 0 (
    echo Failed to apply namespace. Error: %errorlevel%
    goto :error
)

kubectl apply -f configmaps.yaml
kubectl apply -f secrets.yaml
kubectl apply -f pvcs.yaml
kubectl apply -f statefulsets.yaml
kubectl apply -f services.yaml
kubectl apply -f deployments.yaml
kubectl apply -f monitoring.yaml
kubectl apply -f ingress.yaml

echo Waiting for deployments to be ready...
kubectl wait --for=condition=available --timeout=300s deployment/api -n bolttickets
if %errorlevel% neq 0 (
    echo API deployment not ready. Error: %errorlevel%
    goto :error
)

kubectl wait --for=condition=available --timeout=300s deployment/worker -n bolttickets
kubectl wait --for=condition=available --timeout=300s deployment/frontend -n bolttickets

echo Deployment complete!
echo Access the application:
echo   Frontend: http://app.localhost
echo   API: http://api.localhost
echo   Prometheus: kubectl port-forward -n bolttickets svc/prometheus 9090:9090
echo   Grafana: kubectl port-forward -n bolttickets svc/grafana 3000:3000 (admin/admin)
echo   Jaeger: kubectl port-forward -n bolttickets svc/jaeger 16686:16686

goto :end

:error
echo An error occurred. Check the output above.
pause
exit /b 1

:end
pause