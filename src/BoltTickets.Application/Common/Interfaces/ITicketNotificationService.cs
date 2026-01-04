using System;
using System.Threading.Tasks;
using BoltTickets.Domain.Entities;

namespace BoltTickets.Application.Common.Interfaces;

public interface ITicketNotificationService
{
    Task NotifyBookingConfirmedAsync(Booking booking);
    Task NotifyInventoryUpdatedAsync(Guid ticketId, int availableCount);
}
