using Wayward.Domain.DomainModels;

namespace Wayward.Service.Interface
{
    public interface IBoardingPassPdfService
    {
        byte[] Generate(Booking booking);
    }
}
