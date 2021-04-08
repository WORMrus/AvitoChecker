using AvitoChecker.Configuration;
using AvitoChecker.Retriers;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
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

        public override async Task<Listing[]> GetListings(CancellationToken cancellationToken)
        {
            throw new Exception();
            string formattedQuery = HttpUtility.UrlEncode(Query);
            var urlToGet = string.Format(_avitoUrlTemplate, SearchArea, Category, PriceMax, PriceMin, formattedQuery, (int)ListingType);

            var doc = await GetListingsDocument(urlToGet, cancellationToken);

            var contentNode = doc.QuerySelectorAll("div[class^='index-content']:not([calss$='category-map'])")[0];
            var itemListings = contentNode.QuerySelectorAll("[class$='js-catalog-item-enum']");

            return HtmlNodesToListings(itemListings);
        }

        protected override Listing HtmlNodeToListing(HtmlNode node)
        {
            string name = GetTitleFromNode(node);
            if (StrictQueryMatching && !name.ToLower().Contains(Query.ToLower()))
            {
                return null;
            }
            return new Listing()
            {
                Name = name,
                ID = node.Attributes.AttributesWithName("data-item-id").First().Value,
                Price = int.Parse(GetPriceFromNode(node)),
                Published = GetPublishedStringFromNode(node),
                Link = _baseUrl + GetLinkFromNode(node),
                Source = ListingSource
            };
        }

        protected static string GetTitleFromNode(HtmlNode node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "[class^='title-root']");
            return singleNode.InnerText;
        }

        protected static string GetLinkFromNode(HtmlNode node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "a[data-marker='item-title']");
            return singleNode.GetAttributeValue("href", "");
        }

        protected static string GetPriceFromNode(HtmlNode node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "meta[itemprop='price']");
            return singleNode.Attributes.AttributesWithName("content").First().Value;
        }

        protected static string GetPublishedStringFromNode(HtmlNode node)
        {
            //Some listings do not have a date. E.g. shop-related ones
            try
            {
                return QueryNodeAndEnsureSingleResult(node, "[data-marker='item-date']").InnerText;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        protected static HtmlNode QueryNodeAndEnsureSingleResult(HtmlNode node, string query)
        {
            var foundNodes = node.QuerySelectorAll(query);
            return foundNodes.Count != 1 ? throw new Exception($"Not exactly one node matched the selector. The count is {foundNodes.Count}") : foundNodes[0];
        }

    }

}
