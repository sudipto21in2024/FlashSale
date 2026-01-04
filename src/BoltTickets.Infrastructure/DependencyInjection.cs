using BoltTickets.Application.Common.Interfaces;
using BoltTickets.Infrastructure.Messaging;
using BoltTickets.Infrastructure.Persistence;
using BoltTickets.Infrastructure.Persistence.Repositories;
using BoltTickets.Infrastructure.Services;
using BoltTickets.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using StackExchange.Redis;

namespace BoltTickets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost";
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<ITicketCacheService, RedisTicketCacheService>();
        services.AddScoped<ITicketNotificationService, RedisTicketNotificationService>();

        var kafkaBootstrap = configuration.GetConnectionString("Kafka") ?? "localhost:9092";
        services.AddSingleton<IBookingProducer>(sp => new KafkaBookingProducer(kafkaBootstrap));

        // Observability
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation()
                    .AddSource("BoltTickets.API", "BoltTickets.Worker")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(configuration["OTEL_SERVICE_NAME"] ?? "BoltTickets.System"))
                    .AddOtlpExporter(options => 
                    {
                        options.Endpoint = new Uri(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }
}
