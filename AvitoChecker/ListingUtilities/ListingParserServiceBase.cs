using AngleSharp;
using AngleSharp.Dom;
using AvitoChecker.Configuration;
using AvitoChecker.Retriers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AvitoChecker.ListingUtilities
{
    public abstract class ListingParserServiceBase : IListingGetter
    {
        protected readonly string _baseUrl;
        protected readonly HttpClient _client;
        protected string _urlTemplate;
        protected IRetrier _retrier;

        public string Query { get; set; }
        public int PriceMin { get; set; }
        public int PriceMax { get; set; }
        public bool StrictQueryMatching { get; set; }
        public abstract string ListingSource { get; init; }

        protected abstract Listing HtmlNodeToListing(IElement node);

        protected ListingParserServiceBase(HttpClient client, ListingQueryOptions queryOptions, string baseUrl,
                                           IRetrier retrier)
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
            _retrier = retrier;
        }

        public abstract Task<IEnumerable<Listing>> GetListings(CancellationToken cancellationToken);

        protected IEnumerable<Listing> HtmlNodesToListings(IEnumerable<IElement> nodes)
        {
            var listings = nodes.Select(node => HtmlNodeToListing(node))
                                .Where(lst => lst != null);
            return listings;
        }

        protected async Task<IDocument> GetListingsDocument(string listingGetUrl, CancellationToken cancellationToken)
        {
            //It kinda seems clunky. I could've used a closure, but then I would still have to set that variable to null
            async Task<HttpResponseMessage> func()
            {
                HttpResponseMessage resp = await _client.GetAsync(listingGetUrl, cancellationToken);
                resp.EnsureSuccessStatusCode();
                return resp;
            }

            HttpResponseMessage resp = await _retrier.AttemptAsync(func, cancellationToken);

            string res = await resp.Content.ReadAsStringAsync(cancellationToken);

            var context = BrowsingContext.New();
            var doc = await context.OpenAsync(req => req.Content(res), cancellationToken);

            return doc;
        }
    }
}