using AvitoChecker.Configuration;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace AvitoChecker.ListingUtilities
{
    public class AvitoParserService
    {
        protected readonly HttpClient _client;
        protected readonly string _avitoUrlTemplate;
        protected static readonly string _avitoBaseUrl = "https://www.avito.ru";

        public string Query { get; set; }
        public int PriceMin { get; set; }
        public int PriceMax { get; set; }
        public string Category { get; set; }
        public string SearchArea { get; set; }

        public AvitoListingType ListingType { get; set; }


        public AvitoParserService(HttpClient client, IOptions<AvitoListingQueryOptions> options)
        {
            _client = client;
            _client.DefaultRequestHeaders.Add("accept", "text/html");
            _client.DefaultRequestHeaders.Add("accept-encoding", "utf-8");

            AvitoListingQueryOptions opts = options.Value;
            Query = opts.Query;
            PriceMin = opts.PriceMin;
            PriceMax = opts.PriceMax;

            ListingType = opts.AvitoOptions.ListingType;
            Category = opts.AvitoOptions.Category;
            SearchArea = opts.AvitoOptions.SearchArea;

            _avitoUrlTemplate = _avitoBaseUrl + "/{0}/{1}?cd=2&pmax={2}&pmin={3}&q={4}&s=104&user={5}";
        }

        public async Task<Listing[]> GetAvitoListings()
        {
            string formattedQuery = HttpUtility.UrlEncode(Query);
            HttpResponseMessage resp;
            try
            {
                resp = await _client.GetAsync(string.Format(_avitoUrlTemplate, SearchArea, Category, PriceMax, PriceMin, formattedQuery, (int)ListingType));
            }
            catch (Exception)
            {
                throw;
            }

            resp.EnsureSuccessStatusCode();

            HtmlDocument doc = new();
            doc.OptionFixNestedTags = true;

            string res = await resp.Content.ReadAsStringAsync();


            doc.LoadHtml(res);
            var contentNode = doc.QuerySelectorAll("div[class^='index-content']:not([calss$='category-map'])")[0];
            var itemListings = contentNode.QuerySelectorAll("[class$='js-catalog-item-enum']");

            return HtmlNodesToListings(itemListings);
        }

        protected static Listing[] HtmlNodesToListings(IList<HtmlNode> nodes)
        {
            Listing[] listings = new Listing[nodes.Count];
            for (int i = 0; i < listings.Length; i++)
            {
                listings[i] = HtmlNodeToListing(nodes[i]);
            }
            return listings;
        }

        protected static Listing HtmlNodeToListing(HtmlNode node)
        {
            return new Listing()
            {
                Name = GetTitleFromNode(node),
                ID = node.Attributes.AttributesWithName("data-item-id").First().Value,
                Price = int.Parse(GetPriceFromNode(node)),
                Published = GetPublishedStringFromNode(node),
                Link = _avitoBaseUrl + GetLinkFromNode(node)
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
