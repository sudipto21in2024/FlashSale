using System;

namespace BoltTickets.Domain.Entities;

/// <summary>
/// Status of the Booking lifecycle.
/// </summary>
public enum BookingStatus
{
    Pending,
    Confirmed,
    Failed
}

/// <summary>
/// Represents a User's successful reservation of a Ticket.
/// Created asynchronously by the Background Worker.
/// </summary>
public class Booking
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    private Booking() { }

    public Booking(Guid ticketId, Guid userId)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        UserId = userId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the booking as confirmed after successful processing.
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
    }

    /// <summary>
    /// Marks the booking as failed (e.g., payment failure or data inconsistency).
    /// </summary>
    public void Fail()
    {
        Status = BookingStatus.Failed;
    }
}
