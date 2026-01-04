using System;

namespace BoltTickets.Domain.Entities;

/// <summary>
/// Represents an Event Ticket in the system.
/// Acts as the Aggregate Root for inventory management.
/// </summary>
public class Ticket
{
    /// <summary>
    /// Unique identifier for the Ticket type (e.g., "Gold Tier").
    /// </summary>
    public Guid Id { get; private set; }

    public Ticket(Guid id, string eventName, int totalCount)
    {
        Id = id;
        EventName = eventName;
        TotalCount = totalCount;
        AvailableCount = totalCount;
    }

    /// <summary>
    /// Name of the event (e.g., "Rock Concert 2026").
    /// </summary>
    public string EventName { get; private set; } = string.Empty;

    /// <summary>
    /// Total tickets originally issued.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Current number of tickets available for sale.
    /// Decremented atomically in Redis first, then eventually consistent in DB.
    /// </summary>
    public int AvailableCount { get; private set; }
    
    /// <summary>
    /// Concurrency token for Optimistic Concurrency Control (mapped to xmin in Postgres).
    /// Used to prevent lost updates if multiple threads try to update the Ticket row simultaneously.
    /// </summary>
    public uint RowVersion { get; private set; }

    private Ticket() { } // For EF Core

    public Ticket(string eventName, int totalCount)
    {
        Id = Guid.NewGuid();
        EventName = eventName;
        TotalCount = totalCount;
        AvailableCount = totalCount;
    }

    /// <summary>
    /// Attempts to reserve a ticket by decrementing the available count.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no tickets are available.</exception>
    public void Reserve()
    {
        if (AvailableCount <= 0)
        {
            throw new InvalidOperationException("Sold out!");
        }
        AvailableCount--;
    }
}
