using System;
using System.Text.Json;
using System.Threading.Tasks;
using BoltTickets.Application.Common.Interfaces;
using BoltTickets.Domain.Entities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BoltTickets.Infrastructure.Services;

public class RedisTicketNotificationService : ITicketNotificationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTicketNotificationService> _logger;
    private const string BookingChannel = "booking-notifications";
    private const string InventoryChannel = "inventory-notifications";

    public RedisTicketNotificationService(IConnectionMultiplexer redis, ILogger<RedisTicketNotificationService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task NotifyBookingConfirmedAsync(Booking booking)
    {
        var db = _redis.GetDatabase();
        var message = JsonSerializer.Serialize(new
        {
            Type = "BookingConfirmed",
            BookingId = booking.Id,
            UserId = booking.UserId,
            TicketId = booking.TicketId
        });
        _logger.LogInformation("[NOTIFY] Publishing booking confirmation for BookingId={BookingId}", booking.Id);
        await _redis.GetSubscriber().PublishAsync(BookingChannel, message);
    }

    public async Task NotifyInventoryUpdatedAsync(Guid ticketId, int availableCount)
    {
        var message = JsonSerializer.Serialize(new
        {
            Type = "InventoryUpdated",
            TicketId = ticketId,
            AvailableCount = availableCount
        });
        _logger.LogInformation("[NOTIFY] Publishing inventory update for TicketId={TicketId}, Count={Count}", ticketId, availableCount);
        await _redis.GetSubscriber().PublishAsync(InventoryChannel, message);
    }
}
