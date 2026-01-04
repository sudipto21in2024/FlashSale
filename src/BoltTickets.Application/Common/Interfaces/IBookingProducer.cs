using System.Threading.Tasks;
using BoltTickets.Domain.Entities;

namespace BoltTickets.Application.Common.Interfaces;

public interface IBookingProducer
{
    Task PublishBookingIntentAsync(Booking booking);
}
