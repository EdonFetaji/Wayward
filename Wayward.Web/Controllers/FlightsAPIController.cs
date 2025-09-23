using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Climate;
using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
using Wayward.Service.Implementation;
using Wayward.Service.Interface;
namespace Wayward.Web.Controllers
{
    public class FlightsAPIController : Controller
    {
        private readonly IFetchService _fetchService;
        private readonly IFlightService _flightService;

        private static readonly Random _rng = new();

        public FlightsAPIController(IFetchService fetchService, IFlightService flightService)
        {
            _fetchService = fetchService;
            _flightService = flightService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? origin, DateOnly? departureDate, string? maxPrice)
        {
            ViewBag.Origin = origin;
            ViewBag.DepartureDate = departureDate;
            ViewBag.MaxPrice = maxPrice;

            var data = new List<FlightDTO>();
            try
            {
                data = (!string.IsNullOrWhiteSpace(origin))
                    ? await _fetchService.GetAllFlights(origin, departureDate, maxPrice) : new List<FlightDTO>();
            }
            catch (Exception ex)
            {
                
            }

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upload(List<FlightDTO> flights)
        {
            if (flights == null || flights.Count == 0)
            {
                TempData["Error"] = "No flights to upload.";
                return RedirectToAction(nameof(Index),
                    new { origin = (string?)ViewBag.Origin, departureDate = (DateOnly?)ViewBag.DepartureDate, maxPrice = (string?)ViewBag.MaxPrice });
            }

            foreach (var dto in flights)
            {
                var entity = MapToFlight(dto);
                _flightService.Add(entity);
            }

            TempData["Success"] = $"{flights.Count} flights uploaded to database.";
            return RedirectToAction(nameof(Index),
                new { origin = (string?)ViewBag.Origin, departureDate = (DateOnly?)ViewBag.DepartureDate, maxPrice = (string?)ViewBag.MaxPrice });
        }

        private static Flight MapToFlight(FlightDTO dto)
        {
            double price = 0;
            if (!string.IsNullOrWhiteSpace(dto.Price?.Total))
            {
                double.TryParse(dto.Price.Total, NumberStyles.Any, CultureInfo.InvariantCulture, out price);
            }

            // Parse departure date (fallback: now + 14 days)
            DateTime dateDeparture;
            if (!string.IsNullOrWhiteSpace(dto.DepartureDate) &&
                DateTime.TryParse(dto.DepartureDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                dateDeparture = parsed;
            }
            else
            {
                dateDeparture = DateTime.UtcNow.AddDays(14);
            }

            var airlines = new[] { "Wayward Air", "Skopje Jet", "Balkan Wings", "EuroSky", "Cloudline" };
            var airlineName = airlines[_rng.Next(airlines.Length)];

            int hourDur = _rng.Next(1, 7);              
            int minuteDur = new[] { 0, 15, 30, 45 }[_rng.Next(4)];
            int maxSeats = 150 + _rng.Next(0, 61);       

            return new Flight
            {
                Id = Guid.NewGuid(),
                AirlineName = airlineName,
                FlightDeparture = dto.Origin ?? "UNK",
                FlightDestination = dto.Destination ?? "UNK",
                DateDeparture = dateDeparture,
                HourDuration = hourDur,
                MinuteDuration = minuteDur,
                MaxSeats = maxSeats,
                Price = price > 0 ? price : _rng.Next(80, 600),
                Class = FlightClass.Economic
            };
        }
    }

}

