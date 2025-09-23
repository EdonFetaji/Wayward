using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wayward.Domain.DTO;
namespace Wayward.Service.Interface
{
    public interface IFetchService
    {
        public Task<List<FlightDTO>> GetAllFlights(string origin,
             DateOnly? departureDate = null,
             string? maxPrice = null,
             int max = 20);

        public Task<FlightDTO> GetFlightDetails(long id);
    }
}
