using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            _client.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            _client.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br");
            _client.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9");

            var proxy = new WebProxy
            {
                Address = new Uri("http://localhost:8888"),
                BypassProxyOnLocal = false,
            };

            // Now create a client handler which uses that proxy
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };
            _client = new(httpClientHandler, true);

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
