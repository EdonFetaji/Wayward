using Wayward.Domain.DomainModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wayward.Domain.Identity
{
    public class WaywardUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? Surname{ get; set; }
        public string? Address { get; set; }
        public string? PassportNumber{ get; set; }
        public WishList? WishList { get; set; }
        public virtual ICollection<Booking>? Bookings{ get; set; }
    }
}
