using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BoltTickets.Domain.Entities;
using BoltTickets.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // Added for CountAsync

namespace BoltTickets.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingRepository> _logger;
    private static readonly ActivitySource ActivitySource = new("BoltTickets.Repository");

    public BookingRepository(ApplicationDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("AddBooking");
        activity?.SetTag("booking.id", booking.Id);
        activity?.SetTag("ticket.id", booking.TicketId);
        activity?.SetTag("user.id", booking.UserId);

        _logger.LogInformation("[CDC] Attempting to persist BookingId={BookingId} for TicketId={TicketId} (UserId={UserId}). TraceId: {TraceId}, SpanId: {SpanId}", booking.Id, booking.TicketId, booking.UserId, Activity.Current?.TraceId, Activity.Current?.SpanId);
        await _context.Bookings.AddAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        var count = await _context.Bookings.CountAsync(cancellationToken);
        _logger.LogInformation("[CDC] Successfully persisted BookingId={BookingId} – Current DB count: {Count}", booking.Id, count);
    }
}
