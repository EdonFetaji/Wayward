using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using Wayward.Domain;
using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
using Wayward.Service.Interface;

namespace Wayward.Web.Controllers
{
    [Authorize]
    public class WishListsController : Controller
    {
        private readonly IWishListService _wishListService;
        private readonly IFlightService _flightService;
        private readonly ISeatService _seatService;
        private readonly StripeSettings _stripe; 

        public WishListsController(
            IWishListService wishListService,
            IFlightService flightService,
            ISeatService seatService,
            IOptions<StripeSettings> stripeOptions)   
        {
            _wishListService = wishListService;
            _flightService = flightService;
            _seatService = seatService;

            _stripe = stripeOptions.Value ?? throw new InvalidOperationException("Stripe settings missing.");
            StripeConfiguration.ApiKey = _stripe.SecretKey; 
        }
        // GET: WishLists
        public IActionResult Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr)) return Unauthorized();

            var wl = _wishListService.GetByUserId(Guid.Parse(userIdStr));
            var flights = wl?.Flights?.ToList() ?? new System.Collections.Generic.List<Flight>();

            return View(flights);
        }

        // POST: WishLists/BookAllFlights
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult BookAllFlights()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var wl = _wishListService.GetByUserId(userId);
            if (wl?.Flights == null || !wl.Flights.Any())
            {
                TempData["Msg"] = "Your wishlist is empty.";
                return RedirectToAction(nameof(Index));
            }

            Guid BookingId = Guid.NewGuid();
            string flight_not_booked = "";
            foreach (var flight in wl.Flights)
            {

                var seats = _seatService.GetAvailableSeats(flight.Id) ?? new System.Collections.Generic.List<SeatDTO>();
                var random = new Random();
                int value = random.Next(0, seats.Count);
                var firstSeat = seats[value];


                if (firstSeat == null)
                {
                    flight_not_booked = $"{flight.AirlineName}: {flight.FlightDeparture}->{flight.FlightDestination}";
                    break;
                }

                var dto = new BookFlightDTO
                {
                    FlightId = flight.Id,
                    BookingId = BookingId,
                    SelectedFlight = flight,
                    SelectedSeatNumber = firstSeat.SeatNumber
                };

                _flightService.BookFlight(dto, userId);

            }

            if (flight_not_booked !="")
            {
                TempData["Msg"] += $"Flight cannot be booked - no free seats for {flight_not_booked}\n";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCheckoutSession()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var wl = _wishListService.GetByUserId(userId);
            if (wl?.Flights == null || !wl.Flights.Any())
            {
                TempData["Msg"] = "Your wishlist is empty.";
                return RedirectToAction(nameof(Index));
            }

            var domain = $"{Request.Scheme}://{Request.Host}";

            var lineItems = wl.Flights.Select(f => new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    UnitAmount = (long)(f.Price * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{f.AirlineName}: {f.FlightDeparture} → {f.FlightDestination}",
                        Description = $"Date: {f.DateDeparture:yyyy-MM-dd}"
                    }
                }
            }).ToList();

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{domain}/WishLists/SuccessPayment",
                CancelUrl = $"{domain}/WishLists/Index",
                LineItems = lineItems,
                Currency = "eur",

                Metadata = new Dictionary<string, string> { { "userId", userIdStr } },
                PaymentMethodTypes = new List<string> { "card" }
            };

            var session = new SessionService().Create(options);

            return Redirect(session.Url!);
        }

        public IActionResult SuccessPayment()
        {

            TempData["Msg"] = "You successfully paid and booked your flights!";

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                this.BookAllFlights();
            }
            return View();
        }
    }
}
