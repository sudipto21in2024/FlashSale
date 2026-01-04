using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BoltTickets.Domain.Entities;
using BoltTickets.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoltTickets.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TicketRepository> _logger;
    private static readonly ActivitySource ActivitySource = new("BoltTickets.Repository");

    public TicketRepository(ApplicationDbContext context, ILogger<TicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Ticket?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("GetTicket");
        activity?.SetTag("ticket.id", id);

        _logger.LogInformation("Getting ticket {TicketId}. TraceId: {TraceId}, SpanId: {SpanId}", id, Activity.Current?.TraceId, Activity.Current?.SpanId);
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
        using var activity = ActivitySource.StartActivity("UpdateTicket");
        activity?.SetTag("ticket.id", ticket.Id);
        activity?.SetTag("available.count", ticket.AvailableCount);

        _logger.LogInformation("Updating ticket {TicketId}. TraceId: {TraceId}, SpanId: {SpanId}", ticket.Id, Activity.Current?.TraceId, Activity.Current?.SpanId);
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
