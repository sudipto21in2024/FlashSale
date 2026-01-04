using System;
using BoltTickets.Domain.Entities;
using Xunit;

namespace BoltTickets.Domain.Tests;

public class TicketTests
{
    [Fact]
    public void Reserve_ShouldDecrementAvailableCount_WhenAvailable()
    {
        // Arrange
        var ticket = new Ticket("Concert", 100);
        var initialCount = ticket.AvailableCount;

        // Act
        ticket.Reserve();

        // Assert
        Assert.Equal(initialCount - 1, ticket.AvailableCount);
    }

    [Fact]
    public void Reserve_ShouldThrowException_WhenSoldOut()
    {
        // Arrange
        var ticket = new Ticket("Concert", 1);
        ticket.Reserve(); // Count becomes 0

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ticket.Reserve());
    }
}
