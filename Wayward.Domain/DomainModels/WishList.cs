using Wayward.Domain.Identity;
namespace Wayward.Domain.DomainModels
{
    public class WishList : BaseEntity
    {
        public string OwnerId { get; set; }
        public WaywardUser Owner { get; set; }
        public virtual ICollection<Flight>? Flights { get; set; }
    }
}
