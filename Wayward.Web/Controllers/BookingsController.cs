using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Wayward.Service.Interface;

namespace Wayward.Web.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IBoardingPassPdfService _boardingPassPdf;

        public BookingsController(IBookingService bookingService, IBoardingPassPdfService boardingPassPdf)
        {
            _bookingService = bookingService;
            _boardingPassPdf = boardingPassPdf;
        }

        public IActionResult Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return View(_bookingService.GetAllBookings(Guid.Parse(userIdStr)));
        }
        public IActionResult Details(Guid id)
        {
            return View(_bookingService.GetBookingDetails(id));
        }
        [HttpGet]
        public IActionResult PrintBoardingPass(Guid id)
        {
            // Ensure GetBookingDetails eagerly loads: Owner, BookedFlights -> Flight, Seat
            var booking = _bookingService.GetBookingDetails(id);
            if (booking == null)
                return NotFound();

            var pdfBytes = _boardingPassPdf.Generate(booking);
            var fileName = $"BoardingPass_{id.ToString().Substring(0, 8)}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }




        public IActionResult SuccessPayment()
        {
            return View();
        }

    }
}
