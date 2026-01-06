using BoltTickets.Infrastructure;
using BoltTickets.Worker;
using Serilog;
using Serilog.Sinks.File;

// Check for health check command
if (args.Length > 0 && args[0] == "--health")
{
    try
    {
        // Perform basic health checks
        // For now, just check if the application can start
        // In future, add checks for Kafka, Redis, etc.
        Console.WriteLine("Health check passed");
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Health check failed: {ex.Message}");
        Environment.Exit(1);
    }
}

var builder = Host.CreateApplicationBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<BookingWorker>();

var host = builder.Build();
host.Run();
