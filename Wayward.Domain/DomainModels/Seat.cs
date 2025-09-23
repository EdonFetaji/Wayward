namespace Wayward.Domain.DomainModels
{
    public class Seat : BaseEntity
    {
        public Guid? BookedFlightId { get; set; }
        public BookedFlight? BookedFlight { get; set; }
        public int SeatNumber { get; set; }
        public Boolean IsWindowSeat { get; set; }
    }
}
