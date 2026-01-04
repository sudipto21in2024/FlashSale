using System;
using System.Threading;
using System.Threading.Tasks;
using BoltTickets.Domain.Entities;

namespace BoltTickets.Domain.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken);
}
