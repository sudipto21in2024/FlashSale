using System;
using System.Threading.Tasks;

namespace BoltTickets.Application.Common.Interfaces;

public interface ITicketCacheService
{
    Task InitializeCounterAsync(Guid ticketId, int count);
    Task<bool> TryReserveTicketAsync(Guid ticketId);
    Task IncrementTicketAsync(Guid ticketId); // Rollback
    Task<int> GetAvailableCountAsync(Guid ticketId);
}
