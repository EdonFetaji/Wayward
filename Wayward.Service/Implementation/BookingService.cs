using Microsoft.EntityFrameworkCore;
using Wayward.Domain.DomainModels;
using Wayward.Repository;
using Wayward.Service.Interface;

namespace Wayward.Service.Implementation
{
    public class BookingService : IBookingService
    {
        private readonly IRepository<Booking> _repository;

        public BookingService(IRepository<Booking> repository)
        {
            _repository = repository;
        }

        public List<Booking> GetAllBookings(Guid userId)
        {
            return _repository.GetAll(selector: x => x, predicate: x => x.OwnerId == userId.ToString(),
            include: x => x.Include(y => y.BookedFlights)
            .ThenInclude(z => z.Flight).Include(z => z.Owner))
            .ToList();
        }
        public Booking GetBookingDetails(Guid id)
        {
            return _repository.Get(
                selector: x => x,
                predicate: x => x.Id == id,
                include: q => q
                    .Include(b => b.BookedFlights)
                        .ThenInclude(bf => bf.Seat)      // BookedFlight -> Seat
                    .Include(b => b.BookedFlights)
                        .ThenInclude(bf => bf.Flight)    // BookedFlight -> Flight
                    .Include(b => b.Owner)               // Booking -> Owner
            );
        }

    }
}
