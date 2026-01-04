using System.Threading;
using System.Threading.Tasks;
using BoltTickets.Domain.Entities;

namespace BoltTickets.Domain.Repositories;

public interface IBookingRepository
{
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
}
