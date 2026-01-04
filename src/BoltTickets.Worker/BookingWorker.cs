using System;
using System.Text.Json;
using BoltTickets.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using BoltTickets.Domain.Entities;
using BoltTickets.Domain.Repositories;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace BoltTickets.Worker;

/// <summary>
/// Background Service that consumes Booking Intents from Kafka.
/// Responsible for the "Slow Path" persistence to the Database.
/// </summary>
public class BookingWorker : BackgroundService
{
    private readonly ILogger<BookingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _bootstrapServers;
    private const string Topic = "booking-intents";
    private const string GroupId = "booking-group";

    public BookingWorker(ILogger<BookingWorker> logger, IServiceScopeFactory scopeFactory, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _bootstrapServers = config.GetConnectionString("Kafka") ?? "localhost:9092";
    }

    /// <summary>
    /// Main execution loop listening to Kafka topics.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Null, string>(config).Build();
        consumer.Subscribe(Topic);

        _logger.LogInformation("BookingWorker started consuming...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    // Log headers for debugging
                    var headers = string.Join(", ", result.Message.Headers.Select(h => $"{h.Key}={System.Text.Encoding.UTF8.GetString(h.GetValueBytes())}"));
                    _logger.LogInformation($"[WORKER] Received message headers: {headers}");

                    // Extract trace context from headers
                    var parentContext = Propagators.DefaultTextMapPropagator.Extract(default, result.Message.Headers, (carrier, key) =>
                    {
                        if (carrier.TryGetLastBytes(key, out var bytes))
                        {
                            return new[] { System.Text.Encoding.UTF8.GetString(bytes) };
                        }
                        return new string[0];
                    });

                    // Set the current activity context
                    if (parentContext.ActivityContext != default)
                    {
                        Activity.Current = new Activity("KafkaConsume").SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, parentContext.ActivityContext.TraceFlags);
                        _logger.LogInformation($"[WORKER] Set current activity to parent TraceId: {parentContext.ActivityContext.TraceId}, SpanId: {parentContext.ActivityContext.SpanId}");
                    }
                    else
                    {
                        _logger.LogInformation("[WORKER] No parent context found in headers");
                    }

                    var booking = JsonSerializer.Deserialize<Booking>(result.Message.Value);

                    if (booking != null)
                    {
                        await ProcessBookingAsync(booking, stoppingToken);
                        consumer.Commit(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Consumer fatal error");
        }
        finally
        {
            consumer.Close();
        }
    }

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("BoltTickets.Worker");

    /// <summary>
    /// Persists the booking to the Database and confirms status.
    /// </summary>
    private async Task ProcessBookingAsync(Booking booking, CancellationToken token)
    {
        using var activity = ActivitySource.StartActivity("ProcessBooking");
        activity?.SetTag("booking.id", booking.Id);
        activity?.SetTag("user.id", booking.UserId);
        activity?.SetTag("ticket.id", booking.TicketId);

        _logger.LogInformation("[WORKER] Processing booking intent: {BookingId} for user {UserId}. TraceId: {TraceId}, SpanId: {SpanId}", booking.Id, booking.UserId, Activity.Current?.TraceId, Activity.Current?.SpanId);

        try 
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
            
            // 1. Fetch Ticket and update its DB inventory
            var ticket = await ticketRepo.GetAsync(booking.TicketId, token);
            if (ticket == null)
            {
                 _logger.LogWarning($"[WORKER] Ticket {booking.TicketId} not found in database!");
                 activity?.SetStatus(ActivityStatusCode.Error, "Ticket not found in DB");
                 throw new InvalidOperationException($"Ticket {booking.TicketId} not found in database. Seeding required.");
            }

            ticket.Reserve(); // Decrement AvailableCount in entity
            await ticketRepo.UpdateAsync(ticket, token);
            _logger.LogInformation($"[WORKER] Ticket {ticket.Id} inventory updated. Remaining: {ticket.AvailableCount}");
            
            // 2. Persist Booking as Confirmed
            booking.Confirm();
            await bookingRepo.AddAsync(booking, token);
            _logger.LogInformation($"[WORKER] Booking {booking.Id} persisted and confirmed.");
            
            // 3. Notify via SignalR
            var notificationService = scope.ServiceProvider.GetRequiredService<ITicketNotificationService>();
            await notificationService.NotifyBookingConfirmedAsync(booking);
            
            // Also notify inventory change to UI
            await notificationService.NotifyInventoryUpdatedAsync(ticket.Id, ticket.AvailableCount);
            _logger.LogInformation($"[WORKER] Notifications sent for booking {booking.Id}");
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            _logger.LogError(ex, $"[WORKER] CRITICAL ERROR processing booking {booking.Id}");
            throw;
        }
    }
}
