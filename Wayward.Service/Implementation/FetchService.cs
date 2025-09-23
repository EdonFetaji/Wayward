using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Wayward.Domain.DTO;
using Wayward.Service.Interface;

namespace Wayward.Service.Implementation
{
    public class FetchService : IFetchService
    {
        private readonly HttpClient _httpClient;

        private string? _accessToken;
        private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        string clientId = Environment.GetEnvironmentVariable("AMADEUS_CLIENT_ID");
        string clientSecret = Environment.GetEnvironmentVariable("AMADEUS_CLIENT_SECRET");


        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public FetchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://test.api.amadeus.com/");
        }

        public async Task<List<FlightDTO>> GetAllFlights(
        string origin,
        DateOnly? departureDate = null,
        string? maxPrice = null,
        int max = 20)
        {

            async Task<(HttpResponseMessage resp, string body, string url)> CallAsync(Dictionary<string, string> q)
            {
                var url = "v1/shopping/flight-destinations" + ToQueryString(q);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
                req.Headers.Accept.ParseAdd("application/json");

                var resp = await _httpClient.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                return (resp, body, url);
            }

            var query = new Dictionary<string, string>
            {
                ["origin"] = origin
            };
            if (departureDate.HasValue)
                query["departureDate"] = departureDate.Value.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(maxPrice))
                query["maxPrice"] = maxPrice;
            if (max > 0) 
                query["max"] = max.ToString();

            var (resp, body, url) = await CallAsync(query);

            if ((int)resp.StatusCode == 500 && body.Contains("\"code\": 38189"))
            {
                var fallback = new Dictionary<string, string> { ["origin"] = origin };
                if (!string.IsNullOrWhiteSpace(maxPrice))
                    fallback["maxPrice"] = maxPrice;

                var (resp2, body2, url2) = await CallAsync(fallback);
                if (!resp2.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        $"Amadeus error {(int)resp2.StatusCode} {resp2.ReasonPhrase}. url:{url2} body:{body2}");

                var ok2 = await JsonSerializer.DeserializeAsync<FlightSearchResponse>(
                    new MemoryStream(Encoding.UTF8.GetBytes(body2)), _jsonOptions);
                return ok2?.Data ?? new List<FlightDTO>();
            }

            if (!resp.IsSuccessStatusCode)
                return new List<FlightDTO>();

            var ok = await JsonSerializer.DeserializeAsync<FlightSearchResponse>(
                new MemoryStream(Encoding.UTF8.GetBytes(body)), _jsonOptions);

            return ok?.Data ?? new List<FlightDTO>();
        }



        public async Task<FlightDTO> GetFlightDetails(long id)
        {
            // Not applicable for "flight-destination" results. You’d usually call a different endpoint
            // or keep details in your DB. Return an empty DTO for now.
            return new FlightDTO();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
                return _accessToken!;

            await _tokenLock.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
                    return _accessToken!;
                ;

                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId!,
                    ["client_secret"] = clientSecret!
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, "v1/security/oauth2/token")
                {
                    Content = new FormUrlEncodedContent(form)
                };

                var resp = await _httpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();

                var tokenPayload = await resp.Content.ReadFromJsonAsync<TokenResponse>()
                                  ?? throw new InvalidOperationException("Invalid token response");

                _accessToken = tokenPayload.access_token;
                _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenPayload.expires_in);
                return _accessToken!;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private static string ToQueryString(IDictionary<string, string> kv)
        {
            if (kv == null || kv.Count == 0) return string.Empty;
            var sb = new StringBuilder("?");

            foreach (var kvp in kv)
            {
                if (sb.Length > 1) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kvp.Key))
                  .Append('=')
                  .Append(Uri.EscapeDataString(kvp.Value));
            }
            return sb.ToString();
        }

        private sealed class TokenResponse
        {
            public string access_token { get; set; } = "";
            public string token_type { get; set; } = "";
            public int expires_in { get; set; }
            public string? scope { get; set; }
            public string? state { get; set; }
        }
    }
}
