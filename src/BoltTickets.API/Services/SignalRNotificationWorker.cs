using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BoltTickets.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BoltTickets.API.Services;

public class SignalRNotificationWorker : BackgroundService
{
    private readonly ILogger<SignalRNotificationWorker> _logger;
    private readonly IHubContext<TicketHub> _hubContext;
    private readonly IConnectionMultiplexer _redis;
    private const string BookingChannel = "booking-notifications";
    private const string InventoryChannel = "inventory-notifications";

    public SignalRNotificationWorker(
        ILogger<SignalRNotificationWorker> logger,
        IHubContext<TicketHub> hubContext,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _hubContext = hubContext;
        _redis = redis;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SignalRNotificationWorker starting background tasks...");

        // Run the main logic in a separate task to avoid blocking the host startup
        _ = Task.Run(async () => 
        {
            try 
            {
                var sub = _redis.GetSubscriber();

                _logger.LogInformation("Subscribing to Redis channels...");

                // Booking confirmation callback
                await sub.SubscribeAsync(BookingChannel, async (channel, message) =>
                {
                    _logger.LogInformation($"[SignalRWorker] Received raw booking message: {message}");
                    try
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(message!);
                        var userId = data.GetProperty("UserId").GetGuid();
                        var bookingId = data.GetProperty("BookingId").GetGuid();
                        _logger.LogInformation($"[SignalRWorker] Deserialized UserId={userId}, BookingId={bookingId}");
                        _logger.LogInformation($"[SignalRWorker] Publishing anybookingconfirmed to all clients");
                        await _hubContext.Clients.All.SendAsync("anybookingconfirmed", new { BookingId = bookingId, UserId = userId });
                        _logger.LogInformation($"[SignalRWorker] Published anybookingconfirmed to all clients");
                        await _hubContext.Clients.Group(userId.ToString()).SendAsync("bookingconfirmed", new { BookingId = bookingId, Status = "Confirmed" });
                        _logger.LogInformation($"[SignalRWorker] Sent bookingconfirmed to group {userId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SignalRWorker] Error processing booking notification: {Message}", ex.Message);
                    }
                });

                // Inventory update callback
                await sub.SubscribeAsync(InventoryChannel, async (channel, message) =>
                {
                    _logger.LogInformation($"[SignalRWorker] Received raw inventory message: {message}");
                    try
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(message!);
                        var ticketId = data.GetProperty("TicketId").GetGuid();
                        var count = data.GetProperty("AvailableCount").GetInt32();
                        _logger.LogInformation($"[SignalRWorker] Publishing inventoryupdated for TicketId={ticketId}, Count={count}");
                        await _hubContext.Clients.All.SendAsync("inventoryupdated", new
                        {
                            TicketId = ticketId,
                            AvailableCount = count
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SignalRWorker] Error processing inventory notification");
                    }
                });

                _logger.LogInformation("Redis subscriptions active. Starting heartbeat loop...");
                
                // Loop until canceled
                while (!stoppingToken.IsCancellationRequested)
                {
                    await _hubContext.Clients.All.SendAsync("heartbeat", new { Timestamp = DateTime.Now }, stoppingToken);
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR IN SignalRNotificationWorker background task");
            }
        }, stoppingToken);

        return Task.CompletedTask;
    }
}
