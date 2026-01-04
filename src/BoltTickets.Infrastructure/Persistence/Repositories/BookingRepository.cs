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

    public BookingRepository(ApplicationDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CDC] Attempting to persist BookingId={BookingId} for TicketId={TicketId} (UserId={UserId})", booking.Id, booking.TicketId, booking.UserId);
        await _context.Bookings.AddAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        var count = await _context.Bookings.CountAsync(cancellationToken);
        _logger.LogInformation("[CDC] Successfully persisted BookingId={BookingId} – Current DB count: {Count}", booking.Id, count);
    }
}
