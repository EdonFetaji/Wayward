using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
namespace Wayward.Service.Interface
{
    public interface ISeatService
    {
        List<SeatDTO> GetAvailableSeats(Guid booked_flight_id);

        Seat BookSeat(Seat seat);
        Boolean IsSeatAvailable(Guid flight_id, int seatNumber);
    }
}
