using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wayward.Service.Interface
{
    public interface IFlightService
    {
        List<Flight> GetAll();
        Flight? GetById(Guid Id);
        Flight Update(Flight product);
        Flight DeleteById(Guid Id);
        Flight Add(Flight product);
        void BookFlight(BookFlightDTO modelDTO, Guid userId);
        void AddToWishList(Guid id, Guid userId);
    }
}
