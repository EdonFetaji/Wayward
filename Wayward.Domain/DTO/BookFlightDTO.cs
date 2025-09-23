using Wayward.Domain.DomainModels;

namespace Wayward.Domain.DTO
{
    public class BookFlightDTO
    {
        public Guid? BookingId { get; set; }
        public Guid FlightId { get; set; }

        public List<SeatDTO>? AvailableSeats { get; set; }
        public Flight? SelectedFlight { get; set; }
        public int SelectedSeatNumber { get; set; }

    }
}
