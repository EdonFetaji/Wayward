namespace Wayward.Domain.DomainModels
{
    public class BookedFlight : BaseEntity
    {
        public Guid FlightId { get; set; }
        public Flight Flight { get; set; }
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; }

        public Seat Seat { get; set; }

    }
}
