using BoltTickets.Infrastructure;
using BoltTickets.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<BookingWorker>();

var host = builder.Build();
host.Run();
