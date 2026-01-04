using BoltTickets.API.Middleware;
using BoltTickets.API.Hubs;
using BoltTickets.Application;
using BoltTickets.Infrastructure;
using BoltTickets.API.Services;
using Serilog;
using Serilog.Sinks.File;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

// Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<SignalRNotificationWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:5174") 
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Ensure Database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BoltTickets.Infrastructure.Persistence.ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.UseCors("AllowAll");

// Configure Pipeline
if (app.Environment.IsDevelopment())
{
    // Swagger disabled for dependency issues
}

app.UseMiddleware<GlobalExceptionMiddleware>();
//app.UseHttpsRedirection();

app.UseAuthorization();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapControllers();
app.MapHub<TicketHub>("/hubs/tickets");

app.Run();
