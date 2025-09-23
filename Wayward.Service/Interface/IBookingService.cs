using Wayward.Domain.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wayward.Service.Interface
{
    public interface IBookingService
    {
        public List<Booking> GetAllBookings(Guid userId);
        public Booking GetBookingDetails(Guid Id);

    }
}
