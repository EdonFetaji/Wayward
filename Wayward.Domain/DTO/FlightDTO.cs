using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Wayward.Domain.DTO
{
    public  class FlightSearchResponse
    {
        [JsonPropertyName("data")]
        public List<FlightDTO> Data { get; set; } = new();
    }

    public  class FlightDTO
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("origin")]
        public string? Origin { get; set; }

        [JsonPropertyName("destination")]
        public string? Destination { get; set; }
        // "2022-09-11"

        public string? DepartureDate{ get; set; }

        // "2022-09-11"
        [JsonPropertyName("returnDate")]
        public string? ReturnDate{ get; set; }

        // { "total": "161.90" }
        [JsonPropertyName("price")]
        public PriceDTO? Price { get; set; }


       
    }

    public  class PriceDTO
    {
        [JsonPropertyName("total")]
        public string? Total { get; set; }
    }
}
