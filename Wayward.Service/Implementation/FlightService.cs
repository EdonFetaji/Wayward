using Wayward.Domain;
using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
using Wayward.Repository;
using Wayward.Service.Interface;

namespace Wayward.Service.Implementation
{
    public class FlightService : IFlightService
    {
        private readonly IRepository<Flight> _flightRepository;
        private readonly ISeatService _seatService;
        private readonly IRepository<BookedFlight> _bookedFlightRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IWishListService _wishListService;
        private readonly IEmailService  _emailService;

        public FlightService(IRepository<Flight> flightRepository, ISeatService seatService, IRepository<BookedFlight> bookedFlightRepository, IRepository<Booking> bookingRepository, IWishListService wishListService, IEmailService emailService)
        {
            _flightRepository = flightRepository;
            _seatService = seatService;
            _bookedFlightRepository = bookedFlightRepository;
            _bookingRepository = bookingRepository;
            _wishListService = wishListService;
            _emailService = emailService;
        }

        public Flight Add(Flight flight)
        {
            flight.Id = Guid.NewGuid();
            return _flightRepository.Insert(flight);
        }

        public Flight DeleteById(Guid Id)
        {
            var flight = _flightRepository.Get(selector: x => x,
                                                predicate: x => x.Id == Id);
            return _flightRepository.Delete(flight);
        }

        public List<Flight> GetAll()
        {
            return _flightRepository.GetAll(selector: x => x).ToList();
        }

        public Flight? GetById(Guid Id)
        {
            return _flightRepository.Get(selector: x => x,
                                            predicate: x => x.Id == Id);
        }

        public Flight Update(Flight flight)
        {
            return _flightRepository.Update(flight);
        }

        public void BookFlight(BookFlightDTO modelDTO, Guid userId)
        {

            if (modelDTO == null) throw new ArgumentNullException(nameof(modelDTO));
            if (modelDTO.FlightId == Guid.Empty) throw new ArgumentException("FlightId is required.");
            if (modelDTO.SelectedSeatNumber <= 0) throw new ArgumentException("SelectedSeatNumber is required.");

            Booking booking = null;

            if (modelDTO.BookingId.HasValue)
            {
                booking = _bookingRepository.Get(selector: x => x, predicate: x => x.Id == modelDTO.BookingId);
            }

            if (booking == null)
            {
                var idToUse = modelDTO.BookingId ?? Guid.NewGuid();

                booking = new Booking
                {
                    Id = idToUse,
                    OwnerId = userId.ToString(),
                    BookedFlights = new List<BookedFlight>()
                };
                _bookingRepository.Insert(booking);
            }


            BookedFlight bf = new BookedFlight
            {
                Id = Guid.NewGuid(),
                FlightId = modelDTO.FlightId,
                BookingId = booking.Id,

            };

            bf = _bookedFlightRepository.Insert(bf);

            Seat seat = new Seat()
            {
                BookedFlight = bf,
                SeatNumber = modelDTO.SelectedSeatNumber,
                IsWindowSeat = modelDTO.SelectedSeatNumber % 3 == 0
            };

            bf.Seat = seat;
            _bookedFlightRepository.Update(bf);

            booking.BookedFlights.Add(bf);
            _bookingRepository.Update(booking);
            _wishListService.DeleteFromWishList(modelDTO.FlightId, userId);

            string recipientEmail = _bookingRepository.Get(selector: x=>  x.Owner, predicate: x=> x.Id == booking.Id).Email;

            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {

                Flight flight = GetById(bf.FlightId);
                var airline = string.IsNullOrWhiteSpace(flight?.AirlineName) ? "Wayward Air" : flight.AirlineName;
                var dep = string.IsNullOrWhiteSpace(flight?.FlightDeparture) ? "TBA" : flight!.FlightDeparture!;
                var dest = string.IsNullOrWhiteSpace(flight?.FlightDestination) ? "TBA" : flight!.FlightDestination;
                var depDate = flight?.DateDeparture == default ? "(TBA)" : flight!.DateDeparture.ToString("yyyy-MM-dd HH:mm");
                var price = (flight?.Price ?? 0).ToString("0.00");

                var msg = new EmailMessage
                {
                    MailTo = recipientEmail,
                    Subject = "Your booking is confirmed ✈️",
                    Content =
                                $@"Hi,

                        Your booking is confirmed! 🎉

                        Booking ID: {booking.Id}

                        Flight:
                          Airline   : {airline}
                          Route     : {dep} → {dest}
                          Departure : {depDate}
                          Class     : {flight?.Class}
                          Price     : {price} EUR

                        Seat:
                          Number    : {seat.SeatNumber}
                          {(seat.IsWindowSeat ? "Window seat" : "Aisle/Middle")}

                        Thanks for choosing Wayward & Co!
                        "
                };

                // fire-and-forget style (your IEmailService is sync bool)
                _emailService.SendEmailAsync(msg);
            }
        }



        public void AddToWishList(Guid id, Guid userId)
        {
            Flight f = GetById(id);
            if (f == null)
            {
                return;
            }
            WishList wishList = _wishListService.GetByUserId(userId);
            if (wishList == null)
            {
                wishList = new WishList
                {
                    Id = Guid.NewGuid(),
                    OwnerId = userId.ToString(),
                    Flights = new List<Flight>()
                };
                _wishListService.Insert(wishList);

            }
            else
            {
                if (wishList.Flights == null)
                {
                    wishList.Flights = new List<Flight>();
                }
                wishList.Flights.Add(f);
                _wishListService.Update(wishList);
            }

            this.Update(f);
        }
    }
}
