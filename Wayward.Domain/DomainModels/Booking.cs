using Wayward.Domain.Identity;


namespace Wayward.Domain.DomainModels
{
    public class Booking : BaseEntity
    {
        public string? OwnerId { get; set; }
        public WaywardUser Owner { get; set; }
        public virtual ICollection<BookedFlight>? BookedFlights { get; set; }
    }
}
