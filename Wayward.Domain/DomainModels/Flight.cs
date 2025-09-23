using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wayward.Domain.DomainModels;

namespace Wayward.Domain.DomainModels
{
    public enum FlightClass
    {
        Economic,
        Business
    }

    public class Flight : BaseEntity
    {
        [Required]
        public string? AirlineName { get; set; }
        [Required]
        public string? FlightDeparture { get; set; }
        [Required]
        public string FlightDestination { get; set; }
        [Required]
        public DateTime DateDeparture { get; set; }

        [Range(0, 70, ErrorMessage = "HourDuration must be between 0 and 70.")]
        public int HourDuration { get; set; }

        [Range(0, 59, ErrorMessage = "MinuteDuration must be between 0 and 59.")]
        public int MinuteDuration { get; set; }
        [Required]
        public int MaxSeats { get; set; }
        [Required]
        public double Price { get; set; }
        [Required]
        public FlightClass Class { get; set; }
        public virtual ICollection<WishList>? WishLists { get; set; }
        public IEnumerable<BookedFlight>? BookedFlights { get; set; }

    }
}
