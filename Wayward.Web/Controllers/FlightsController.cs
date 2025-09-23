using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using Wayward.Domain;
using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
using Wayward.Service.Interface;

namespace Wayward.Web.Controllers
{
    public class FlightsController : Controller
    {
        private readonly IFlightService _flightService;
        private readonly ISeatService _seatService;
        private readonly StripeSettings _stripe;

        public FlightsController(
            IFlightService flightService,
            ISeatService seatService,
            IOptions<StripeSettings> stripeSettings)
        {
            _flightService = flightService;
            _seatService = seatService;
            _stripe = stripeSettings.Value ?? throw new InvalidOperationException("Stripe settings missing.");

            // Set Stripe secret once (global)
            StripeConfiguration.ApiKey = _stripe.SecretKey;
        }
        // GET: Flights
        public IActionResult Index()
        {
            var flights = _flightService.GetAll();
            return View(flights);
        }

        // GET: Flights/Details/5
        public IActionResult Details(Guid? id)
        {
            if (id == null) return NotFound();

            var flight = _flightService.GetById(id.Value);
            if (flight == null) return NotFound();

            return View(flight);
        }

        // GET: Flights/Create
        public IActionResult Create() => View();

        // POST: Flights/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("AirlineName,FlightDeparture,FlightDestination,DateDeparture,HourDuration,MinuteDuration,MaxSeats,Price,Class")] Flight flight)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ",
           ModelState.SelectMany(kvp => kvp.Value.Errors.Select(e =>
               $"{kvp.Key}: {(string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)}")));

                return View(flight);
            }

            _flightService.Add(flight);
            return RedirectToAction(nameof(Index));
        }

        // GET: Flights/Edit/5
        public IActionResult Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var flight = _flightService.GetById(id.Value);
            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flights/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, [Bind("AirlineName,FlightDeparture,FlightDestination,DateDeparture,HourDuration,MinuteDuration,MaxSeats,Price,Class,Id")] Flight flight)
        {
            if (id != flight.Id) return NotFound();

            if (!ModelState.IsValid) return View(flight);

            try
            {
                _flightService.Update(flight);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlightExists(flight.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Flights/Delete/5
        public IActionResult Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var flight = _flightService.GetById(id.Value);
            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flights/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id)
        {
            _flightService.DeleteById(id);
            return RedirectToAction(nameof(Index));
        }

        private bool FlightExists(Guid id) => _flightService.GetById(id) != null;

        [Authorize]
        public IActionResult Book(Guid id)
        {
            if (id == null) return NotFound();

            var flight = _flightService.GetById(id);
            if (flight == null) return NotFound();

            var model = new BookFlightDTO
            {
                FlightId = id,
                SelectedFlight = flight,
                AvailableSeats = _seatService.GetAvailableSeats(id),
                BookingId = Guid.Empty
            };
            return View(model); 
        }

        // POST: Flights/Book
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(BookFlightDTO model)
        {
            if (model.SelectedSeatNumber == null || model.FlightId == null)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _flightService.BookFlight(model, Guid.Parse(userId));
            return RedirectToAction(nameof(Details), new { id = model.FlightId });
        }
        // POST: Flights/PayAndBook – creates Stripe Checkout Session and redirects
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayAndBook(BookFlightDTO model)
        {
            if (model == null || model.FlightId == Guid.Empty || model.SelectedSeatNumber <= 0)
                return View("Book", model);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var flight = _flightService.GetById(model.FlightId);
            if (flight == null) return NotFound();

            if (!_seatService.IsSeatAvailable(model.FlightId, model.SelectedSeatNumber))
            {
                ModelState.AddModelError("", "Selected seat is no longer available.");
                model.AvailableSeats = _seatService.GetAvailableSeats(model.FlightId);
                model.SelectedFlight = flight;
                return View("Book", model);
            }

            var amountCents = (long)(flight.Price * 100);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successBase = Url.Action(nameof(FinalizePayment), "Flights");
            var cancelBase = Url.Action(nameof(CancelPayment), "Flights",
                                new { flightId = model.FlightId, seatNumber = model.SelectedSeatNumber });

            var successUrl = $"{baseUrl}{successBase}?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}{cancelBase}";

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Currency = "eur",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            UnitAmount = amountCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Flight {flight.AirlineName} {flight.FlightDeparture}->{flight.FlightDestination}",
                                Description = $"Seat {model.SelectedSeatNumber}"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "flightId", model.FlightId.ToString() },
                    { "seatNumber", model.SelectedSeatNumber.ToString() },
                    { "userId", userIdStr }
                }
            };

            var session = new SessionService().Create(options);
            if (string.IsNullOrEmpty(session.Url))
                throw new InvalidOperationException("Stripe session URL is missing.");

            return Redirect(session.Url!);
        }

        [Authorize]
        [HttpGet]
        public IActionResult FinalizePayment(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id) || session_id.Contains("{CHECKOUT_SESSION_ID}"))
                return BadRequest("Invalid or missing Stripe session id.");

            var session = new SessionService().Get(session_id);
            if (session.PaymentStatus == "paid" && session.Status == "complete")
            {
                ViewBag["booking_payment_status"] = "Payment confirmed. Enjoy yout flight with us";
                var flightId = Guid.Parse(session.Metadata["flightId"]);
                var seatNumber = int.Parse(session.Metadata["seatNumber"]);
                var userId = Guid.Parse(session.Metadata["userId"]);

                var flight = _flightService.GetById(flightId);
                if (flight == null) return NotFound();

                if (!_seatService.IsSeatAvailable(flightId, seatNumber))
                    return RedirectToAction(nameof(UnSuccessfulPayment)); // seat taken in the meantime

                var dto = new BookFlightDTO
                {
                    FlightId = flightId,
                    SelectedSeatNumber = seatNumber
                };

                _flightService.BookFlight(dto, userId);
                return RedirectToAction(nameof(Index), "Bookings");
            }

            return RedirectToAction(nameof(UnSuccessfulPayment));
        }

        // GET: Stripe cancel return
        [Authorize]
        [HttpGet]
        public IActionResult CancelPayment(Guid flightId, int seatNumber)
        {
            return RedirectToAction(nameof(Details), new { id = flightId });
        }

        // GET: Payment failed view
        [HttpGet]
        public IActionResult UnSuccessfulPayment()
        {
            return View();
        }



        // POST: Flights/AddToWishList/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToWishList(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _flightService.AddToWishList(id, Guid.Parse(userId));
            return RedirectToAction(nameof(Index), "WishLists");
        }
    }
}
