# Worker Health Checks Implementation Guide

## Overview
The BoltTickets Worker service is currently deployed without health and readiness checks in Kubernetes. This document outlines the implementation options and steps for adding health checks to ensure proper monitoring and lifecycle management in Kubernetes.

## Current State
- Worker uses `Microsoft.NET.Sdk.Worker` SDK
- No HTTP endpoints exposed
- Kubernetes deployment has no `livenessProbe` or `readinessProbe` configured
- Service runs as background process processing Kafka messages

## Implementation Options

### Option 1: HTTP-Based Health Checks (Recommended for Complex Monitoring)

#### Changes Required:

1. **Add NuGet Packages** (`BoltTickets.Worker.csproj`):
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.Hosting" Version="9.0.0" />
   <PackageReference Include="Microsoft.AspNetCore.HealthChecks" Version="9.0.0" />
   <PackageReference Include="Microsoft.AspNetCore.Diagnostics.HealthChecks" Version="9.0.0" />
   ```

2. **Update Program.cs**:
   ```csharp
   using Microsoft.AspNetCore.Hosting;
   using Microsoft.AspNetCore.Builder;
   using Microsoft.AspNetCore.Diagnostics.HealthChecks;
   using Microsoft.Extensions.Diagnostics.HealthChecks;

   var builder = Host.CreateApplicationBuilder(args);

   // ... existing logging setup ...

   // Add health checks
   builder.Services.AddHealthChecks()
       .AddCheck("worker-health", () => HealthCheckResult.Healthy("Worker is running"));

   // Configure web host for health checks
   builder.Services.AddHttpClient();
   builder.WebHost.Configure(app =>
   {
       app.UseRouting();
       app.UseEndpoints(endpoints =>
       {
           endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
           {
               Predicate = _ => true
           });
           endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
           {
               Predicate = _ => true
           });
       });
   }).UseKestrel(options =>
   {
       options.ListenAnyIP(8080); // Health check port
   });

   // ... rest of existing code ...
   ```

3. **Update Dockerfile**:
   ```dockerfile
   # ... existing build stages ...

   # Runtime stage
   FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
   WORKDIR /app
   COPY --from=build /app/publish .
   EXPOSE 8080  # Health check port
   ENTRYPOINT ["dotnet", "BoltTickets.Worker.dll"]
   ```

4. **Update Kubernetes Deployment** (`k8s/deployments.yaml`):
   ```yaml
   apiVersion: apps/v1
   kind: Deployment
   metadata:
     name: worker
     namespace: bolttickets
   spec:
     replicas: 1
     selector:
       matchLabels:
         app: worker
     template:
       metadata:
         labels:
           app: worker
       spec:
         containers:
         - name: worker
           image: bolttickets/worker:latest
           ports:
           - containerPort: 8080  # Health check port
           volumeMounts:
           - name: config
             mountPath: /app/appsettings.json
             subPath: appsettings.json
           livenessProbe:
             httpGet:
               path: /health/live
               port: 8080
             initialDelaySeconds: 30
             periodSeconds: 10
           readinessProbe:
             httpGet:
               path: /health/ready
               port: 8080
             initialDelaySeconds: 5
             periodSeconds: 5
   ```

### Option 2: Command-Based Health Checks (Simpler, No Web Hosting)

#### Changes Required:

1. **Update Program.cs**:
   ```csharp
   // At the beginning of Program.cs
   if (args.Length > 0 && args[0] == "--health")
   {
       // Perform health checks
       try
       {
           // Add health check logic here
           // e.g., check Kafka connection, Redis connection, etc.
           Console.WriteLine("Health check passed");
           Environment.Exit(0);
       }
       catch (Exception ex)
       {
           Console.WriteLine($"Health check failed: {ex.Message}");
           Environment.Exit(1);
       }
   }

   // Continue with normal worker startup
   var builder = Host.CreateApplicationBuilder(args);
   // ... rest of existing code ...
   ```

2. **Update Kubernetes Deployment** (`k8s/deployments.yaml`):
   ```yaml
   apiVersion: apps/v1
   kind: Deployment
   metadata:
     name: worker
     namespace: bolttickets
   spec:
     replicas: 1
     selector:
       matchLabels:
         app: worker
     template:
       metadata:
         labels:
           app: worker
       spec:
         containers:
         - name: worker
           image: bolttickets/worker:latest
           volumeMounts:
           - name: config
             mountPath: /app/appsettings.json
             subPath: appsettings.json
           livenessProbe:
             exec:
               command:
                 - dotnet
                 - BoltTickets.Worker.dll
                 - --health
             initialDelaySeconds: 30
             periodSeconds: 10
           readinessProbe:
             exec:
               command:
                 - dotnet
                 - BoltTickets.Worker.dll
                 - --health
             initialDelaySeconds: 5
             periodSeconds: 5
   ```

## Health Check Implementation Details

### What to Check:
- Kafka broker connectivity
- Redis cache connectivity
- Database connectivity (if applicable)
- Hosted service status
- Message processing queue status

### Example Health Check Implementation:
```csharp
private static async Task<bool> PerformHealthChecks(IServiceProvider services)
{
    // Check Kafka connection
    var producer = services.GetRequiredService<IBookingProducer>();
    // Implement connection test

    // Check Redis connection
    var cache = services.GetRequiredService<ITicketCacheService>();
    // Implement connection test

    return true; // or false based on checks
}
```

## Testing
1. Deploy the updated worker
2. Verify health check endpoints respond correctly
3. Test pod restarts on health check failures
4. Monitor Kubernetes events for probe failures

## Benefits
- Automatic pod restarts on failures
- Traffic routing only to healthy pods
- Better observability in Kubernetes dashboard
- Prevents cascading failures

## Notes
- Option 2 is simpler and maintains pure worker architecture
- Option 1 provides more detailed health information via HTTP
- Choose based on monitoring requirements and complexity tolerance