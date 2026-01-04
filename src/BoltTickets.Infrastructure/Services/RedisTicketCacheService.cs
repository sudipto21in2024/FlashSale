using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BoltTickets.Application.Common.Interfaces;
using StackExchange.Redis;

namespace BoltTickets.Infrastructure.Services;

/// <summary>
/// Implements high-speed inventory management using Redis.
/// Uses atomic integer decrement to prevent race conditions during the flash sale.
/// </summary>
public class RedisTicketCacheService : ITicketCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisTicketCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    /// <summary>
    /// Initializes the available ticket count in Redis if it doesn't exist.
    /// </summary>
    public async Task InitializeCounterAsync(Guid ticketId, int count)
    {
        // Only set if not exists to prevent overwriting during sale
        await _db.StringSetAsync($"ticket:{ticketId}", count, when: When.NotExists);
    }

    /// <summary>
    /// Attempts to reserve a ticket atomically.
    /// Returns true if successful (count >= 0), false otherwise.
    /// </summary>
    public async Task<bool> TryReserveTicketAsync(Guid ticketId)
    {
        // Decrement: returns new value. If < 0, then we oversold locally
        var newValue = await _db.StringDecrementAsync($"ticket:{ticketId}");
        
        if (newValue < 0)
        {
            // Rollback: Logic determines we oversold, so we increment back to keep the number accurate (roughly).
            await _db.StringIncrementAsync($"ticket:{ticketId}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Manually increments the ticket count (e.g., if a booking fails later).
    /// </summary>
    public async Task IncrementTicketAsync(Guid ticketId)
    {
        await _db.StringIncrementAsync($"ticket:{ticketId}");
    }

    public async Task<int> GetAvailableCountAsync(Guid ticketId)
    {
        var value = await _db.StringGetAsync($"ticket:{ticketId}");
        if (value.IsNull) return 0;
        return (int)value;
    }
}
