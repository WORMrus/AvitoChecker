using AvitoChecker.Configuration;
using AvitoChecker.Extensions;
using AvitoChecker.Retriers;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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

        protected abstract Listing HtmlNodeToListing(HtmlNode node);

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

        public abstract Task<Listing[]> GetListings(CancellationToken cancellationToken);

        protected Listing[] HtmlNodesToListings(IList<HtmlNode> nodes)
        {
            //we will never need more than nodes.Count, so better allocate now then do it now and later again
            List<Listing> listings = new(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                listings.AddIfNotNull(HtmlNodeToListing(nodes[i]));
            }
            return listings.ToArray();
        }

        protected async Task<HtmlDocument> GetListingsDocument(string listingGetUrl, CancellationToken cancellationToken)
        {
            //It kinda seems clunky. I could've used a closure, but then I would still have to set that variable to null
            async Task<HttpResponseMessage> func()
            {
                HttpResponseMessage resp = await _client.GetAsync(listingGetUrl);
                resp.EnsureSuccessStatusCode();
                return resp;
            }

            HttpResponseMessage resp = await _retrier.AttemptAsync(func, cancellationToken);

            HtmlDocument doc = new();
            doc.OptionFixNestedTags = true;

            string res = await resp.Content.ReadAsStringAsync();

            doc.LoadHtml(res);

            return doc;
        }
    }
}