using AvitoChecker.Configuration;
using System.Net.Http;

namespace AvitoChecker.ListingUtilities
{
    public class ListingParserServiceBase
    {
        protected readonly string _baseUrl;
        protected readonly HttpClient _client;
        protected string _urlTemplate;

        public string Query { get; set; }
        public int PriceMin { get; set; }
        public int PriceMax { get; set; }
        public bool StrictQueryMatching { get; set; }

        protected ListingParserServiceBase(HttpClient client, ListingQueryOptions queryOptions, string baseUrl)
        {
            _client = client;

            _client = client;
            _client.DefaultRequestHeaders.Add("accept", "text/html");
            _client.DefaultRequestHeaders.Add("accept-encoding", "utf-8");

            Query = queryOptions.Query;
            PriceMin = queryOptions.PriceMin;
            PriceMax = queryOptions.PriceMax;
            StrictQueryMatching = queryOptions.StrictQueryMatching;

            _baseUrl = baseUrl;
        }
    }
}