using BoltTickets.Infrastructure;
using BoltTickets.Worker;
using Serilog;
using Serilog.Sinks.File;

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
