using System;
using System.Threading;
using System.Threading.Tasks;
using BoltTickets.Domain.Entities;
using BoltTickets.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BoltTickets.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        await _context.Tickets.AddAsync(ticket, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
