using AngleSharp.Dom;
using AvitoChecker.Configuration;
using AvitoChecker.Retriers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AvitoChecker.ListingUtilities
{
    public class AvitoParserService : ListingParserServiceBase
    {
        protected readonly string _avitoUrlTemplate;


        public string Category { get; set; }
        public string SearchArea { get; set; }
        public override string ListingSource { get; init; }
        public AvitoListingType ListingType { get; set; }


        public AvitoParserService(HttpClient client, IOptions<AvitoListingQueryOptions> options, IRetrier retrier)
            : base(client, options.Value, "https://www.avito.ru", retrier)
        {
            var opts = options.Value.AvitoOptions;

            ListingType = opts.ListingType;
            Category = opts.Category;
            SearchArea = opts.SearchArea;

            _avitoUrlTemplate = _baseUrl + "/{0}/{1}?cd=2&pmax={2}&pmin={3}&q={4}&s=104&user={5}";

            ListingSource = "Avito";
        }

        public override async Task<IEnumerable<Listing>> GetListings(CancellationToken cancellationToken)
        {
            string formattedQuery = HttpUtility.UrlEncode(Query);
            var urlToGet = string.Format(_avitoUrlTemplate, SearchArea, Category, PriceMax, PriceMin, formattedQuery, (int)ListingType);

            var doc = await GetListingsDocument(urlToGet, cancellationToken);

            var itemListings = doc.QuerySelectorAll("div[data-marker='item']");

            return HtmlNodesToListings(itemListings);
        }

        protected override Listing HtmlNodeToListing(AngleSharp.Dom.IElement node)
        {
            string name = GetTitleFromNode(node);
            if (StrictQueryMatching && !name.ToLower().Contains(Query.ToLower()))
            {
                return null;
            }
            return new Listing()
            {
                Name = name,
                ID = node.GetAttribute("data-item-id"),
                Price = int.Parse(GetPriceFromNode(node)),
                Published = GetPublishedStringFromNode(node),
                Link = _baseUrl + GetLinkFromNode(node),
                Source = ListingSource
            };
        }

        protected static string GetTitleFromNode(IElement node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "[class^='title-root']");
            return singleNode.TextContent;
        }

        protected static string GetLinkFromNode(IElement node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "a[data-marker='item-title']");
            return singleNode.GetAttribute("href");
        }

        protected static string GetPriceFromNode(IElement node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "meta[itemprop='price']");
            return singleNode.GetAttribute("content");
        }

        protected static string GetPublishedStringFromNode(IElement node)
        {
            //Some listings do not have a date. E.g. shop-related ones
            try
            {
                return QueryNodeAndEnsureSingleResult(node, "[data-marker='item-date']").TextContent;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        protected static IElement QueryNodeAndEnsureSingleResult(IElement node, string query)
        {
            var foundNodes = node.QuerySelectorAll(query);
            return foundNodes.Length != 1 ? throw new Exception($"Not exactly one node matched the selector. The count is {foundNodes.Length}") : foundNodes[0];
        }

    }

}
