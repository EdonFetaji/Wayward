using System;
using System.Collections.Generic;
using System.Linq;
using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
using Wayward.Repository;
using Wayward.Service.Interface;

namespace Wayward.Service.Implementation
{
    public class SeatService : ISeatService
    {
        private readonly IRepository<BookedFlight> _bookedFlightRepository;
        private readonly IRepository<Flight> _flightRepository;
        private readonly IRepository<Seat> _seatRepository;

        public SeatService(IRepository<BookedFlight> bookedFlightRepository, IRepository<Flight> flightRepository, IRepository<Seat> seatRepository)
        {
            _bookedFlightRepository = bookedFlightRepository;
            _flightRepository = flightRepository;
            _seatRepository = seatRepository;
        }

        public Seat BookSeat(Seat seat)
        {

            return _seatRepository.Insert(seat);
        }

        public List<SeatDTO> GetAvailableSeats(Guid flight_id)
        {
            var bookedSeats = _bookedFlightRepository.GetAll(
                selector: bf => bf.Seat.SeatNumber,
                predicate: bf => bf.FlightId == flight_id
            ).ToHashSet();


            var maxSeats = _flightRepository.Get(
                selector: f => f.MaxSeats,
                predicate: f => f.Id == flight_id);

            if (maxSeats <= 0)
                return new List<SeatDTO>();

            var result = new List<SeatDTO>(Math.Max(0, maxSeats - bookedSeats.Count));
            for (int i = 1; i < maxSeats; i++)
            {
                if (!bookedSeats.Contains(i))
                {
                    result.Add(new SeatDTO
                    {
                        SeatNumber = i,
                        IsWindowSeat = (i % 3) == 0
                    });
                }
            }
            return result;
        }

        public bool IsSeatAvailable(Guid flight_id, int seatNumber)
        {
            var bookedSeats = _bookedFlightRepository.GetAll(
               selector: bf => bf.Seat.SeatNumber,
               predicate: bf => bf.FlightId == flight_id
           ).ToHashSet();

            return ! bookedSeats.Contains(seatNumber);

        }
    }
}
