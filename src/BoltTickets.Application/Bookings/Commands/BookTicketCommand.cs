using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BoltTickets.Application.Common.Interfaces;
using BoltTickets.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace BoltTickets.Application.Bookings.Commands;

/// <summary>
/// Command to initiate a ticket booking.
/// </summary>
/// <param name="TicketId">The ID of the ticket to book.</param>
/// <param name="UserId">The ID of the user booking the ticket.</param>
public record BookTicketCommand(Guid TicketId, Guid UserId) : IRequest<Guid>;

/// <summary>
/// Handles the booking command using the "Fast Path" strategy.
/// 1. Reserves inventory in Redis (Atomic).
/// 2. Publishes intent to Kafka (Async Persistence).
/// </summary>
public class BookTicketCommandHandler : IRequestHandler<BookTicketCommand, Guid>
{
    private readonly ITicketCacheService _ticketCache;
    private readonly IBookingProducer _bookingProducer;
    private readonly ILogger<BookTicketCommandHandler> _logger;

    public BookTicketCommandHandler(ITicketCacheService ticketCache, IBookingProducer bookingProducer, ILogger<BookTicketCommandHandler> logger)
    {
        _ticketCache = ticketCache;
        _bookingProducer = bookingProducer;
        _logger = logger;
    }

    /// <summary>
    /// Executes the booking logic.
    /// </summary>
    /// <returns>The generated Booking ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown if tickets are sold out.</exception>
    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("BoltTickets.API");

    public async Task<Guid> Handle(BookTicketCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("BookTicket");
        activity?.SetTag("ticket.id", request.TicketId);
        activity?.SetTag("user.id", request.UserId);

        _logger.LogInformation("BookTicket activity started. TraceId: {TraceId}, SpanId: {SpanId}", Activity.Current?.TraceId, Activity.Current?.SpanId);

        // 1. Fast check against Redis
        var reserved = await _ticketCache.TryReserveTicketAsync(request.TicketId);
        if (!reserved)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Tickets target sold out");
            throw new InvalidOperationException("Tickets are sold out or unavailable.");
        }

        // 2. Create Booking Entity (Pending)
        var booking = new Booking(request.TicketId, request.UserId);
        activity?.SetTag("booking.id", booking.Id);

        // 3. Publish to Kafka for async processing (Persistence)
        try 
        {
            await _bookingProducer.PublishBookingIntentAsync(booking);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }

        return booking.Id;
    }
}
