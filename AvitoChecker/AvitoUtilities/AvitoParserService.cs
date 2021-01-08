using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace AvitoChecker
{
    public class AvitoParserService
    {
        protected readonly HttpClient _client;
        protected readonly string avitoUrlTemplate;

        public AvitoParserService(HttpClient client)
        {
            _client = client;
            _client.DefaultRequestHeaders.Add("accept", "text/html");
            _client.DefaultRequestHeaders.Add("accept-encoding", "utf-8");

            avitoUrlTemplate = "https://www.avito.ru/rossiya/telefony?cd=2&pmax={0}&pmin={1}&q={2}&s=104&user={3}";

        }

        public async Task<AvitoListing[]> GetAvitoPhoneListings(string searchQuery, int priceFrom, int priceTo, AvitoListingType type)
        {
            string formattedQuery = HttpUtility.UrlEncode(searchQuery);
            HttpResponseMessage resp = null;
            try
            {
                resp = await _client.GetAsync(string.Format(avitoUrlTemplate, priceTo, priceFrom, formattedQuery, (int)type));
            }
            catch (Exception e)
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

        protected AvitoListing[] HtmlNodesToListings(IList<HtmlNode> nodes)
        {
            AvitoListing[] listings = new AvitoListing[nodes.Count];
            for (int i = 0; i < listings.Length; i++)
            {
                listings[i] = htmlNodeToListing(nodes[i]);
            }
            return listings;
        }

        protected AvitoListing htmlNodeToListing(HtmlNode node)
        {
            return new AvitoListing()
            {
                Name = GetTitleFromNode(node),
                ID = node.Attributes.AttributesWithName("data-item-id").First().Value,
                Price = int.Parse(GetPriceFromNode(node)),
                Published = GetPublishedStringFromNode(node)
            };
        }

        protected string GetTitleFromNode(HtmlNode node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "span[class^='title-root']");
            return singleNode.InnerText;
        }

        protected string GetPriceFromNode(HtmlNode node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, "meta[itemprop='price']");
            return singleNode.Attributes.AttributesWithName("content").First().Value;
        }

        protected string GetPublishedStringFromNode(HtmlNode node)
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

        protected HtmlNode QueryNodeAndEnsureSingleResult(HtmlNode node, string query)
        {
            var foundNodes = node.QuerySelectorAll(query);
            return foundNodes.Count != 1 ? throw new Exception($"More than one node matched the selector. The count is {foundNodes.Count}") : foundNodes[0];
        }
    }
}
