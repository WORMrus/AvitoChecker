using AvitoChecker.Configuration;
using AvitoChecker.Extensions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using static AvitoChecker.Configuration.YoulaListingQueryOptions;


namespace AvitoChecker.ListingUtilities
{
    public class YoulaParserService : ListingParserServiceBase
    {
        protected YoulaListingOptions _youlaOptions;
        protected bool _isInstanceInitialized;

        public string Category { get; set; }

        public YoulaParserService(HttpClient client, IOptions<YoulaListingQueryOptions> options)
            : base(client, options.Value, "https://youla.ru")
        {
            _youlaOptions = options.Value.YoulaOptions;
            //Prices are implicitly in 1/100 of a rubble 
            _urlTemplate = _baseUrl + "/all/smartfony-planshety/smartfony?attributes[price][from]={0}00&attributes[price][to]={1}00&attributes[sort_field]=date_published&q={2}";

            _isInstanceInitialized = false;
        }

        public async Task InitialyzeInstanceAsync()
        {
            var opts = _youlaOptions;

            YoulaLocationData cityData;

            if (opts.Type == YoulaLocationType.City)
            {
                cityData = await GetCityDataByName(opts.CityName, opts.StrictCityNameMatching.Value);
            }
            else
            {
                cityData = new()
                {
                    Name = opts.CityName,
                    Coords = opts.Coordinates
                };
            }

            cityData.Radius = opts.SearchRadius;
            cityData.LocationType = opts.Type;
            await SetLocation(cityData);
        }

        //Point and City types require different fields to be set
        //Field names are also different from whatever GetCityDataByName returns
        protected async Task SetLocation(YoulaLocationData cityData)
        {
            using var form = new MultipartFormDataContent();

            switch (cityData.LocationType)
            {
                case YoulaLocationType.City:
                    form.Add(new StringContent(cityData.Id), "id");
                    break;

                case YoulaLocationType.Point:
                    form.Add(new StringContent(cityData.Name), "title");
                    form.Add(new StringContent(cityData.Coords.Latitude.ToString()), "lat");
                    form.Add(new StringContent(cityData.Coords.Longitude.ToString()), "lng");
                    break;

                default:
                    throw new NotImplementedException($"Missing implementation for {cityData.LocationType}");
            }
            form.Add(new StringContent(cityData.LocationType.ToString()), "type");
            form.Add(new StringContent(cityData.Radius.ToString()), "r");

            var resp = await _client.PostAsync(_baseUrl + "/web-api/geo/save_location", form);
            resp.EnsureSuccessStatusCode();
        }

        //The API returns an array of matching cities sorted by (apparently) the number of listings in each
        //The assumption is that longer city names have fewer listings and that you either have one result
        //Or that the needed city is the 1st one.
        //A possible issue: you need "City", but "Bigger City" exists and it is also bigger. 
        //strictCityNameMatching hast to be true to get "City" instead of "Bigger City"
        protected async Task<YoulaLocationData> GetCityDataByName(string cityName, bool strictCityNameMatching)
        {
            string searchUrl = $"https://api.youla.io/api/v1/geo/cities?search={HttpUtility.UrlEncode(cityName)}";
            var resp = await _client.GetAsync(searchUrl);
            resp.EnsureSuccessStatusCode();
            var respJson = await resp.Content.ReadAsStringAsync();

            var document = JsonDocument.Parse(respJson, new JsonDocumentOptions { AllowTrailingCommas = true });
            var citiesJson = document.RootElement.GetProperty("data").GetRawText();

            var cities = JsonSerializer.Deserialize<YoulaLocationData[]>(
                citiesJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return strictCityNameMatching ? cities.First(c => c.Name == cityName) : cities[0];
        }

        public async Task<Listing[]> GetAvitoListings()
        {
            if (!_isInstanceInitialized)
            {
                await InitialyzeInstanceAsync();
            }
            return await GetAvitoListingsInternal();
        }

        protected async Task<Listing[]> GetAvitoListingsInternal()
        {
            string formattedQuery = HttpUtility.UrlEncode(Query);
            HttpResponseMessage resp;
            try
            {
                resp = await _client.GetAsync(string.Format(_urlTemplate, PriceMin, PriceMax, formattedQuery));
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
            var contentNode = doc.QuerySelectorAll("ul.product_list")[0];
            var itemListings = contentNode.QuerySelectorAll("li:not([data-banner-type])");

            return HtmlNodesToListings(itemListings);
        }

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

        protected Listing HtmlNodeToListing(HtmlNode node)
        {
            string name = GetTitleFromNode(node);
            if (StrictQueryMatching && !name.ToLower().Contains(Query.ToLower()))
            {
                return null;
            }
            return new Listing()
            {
                Name = name,
                ID = node.GetAttributeValue("data-id", ""),
                Price = int.Parse(GetPriceFromNode(node)),
                Published = GetPublishedStringFromNode(node),
                Link = _baseUrl + GetLinkFromNode(node)
            };
        }

        protected static string GetTitleFromNode(HtmlNode node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, ".product_item__title");
            return singleNode.InnerText;
        }

        protected static string GetLinkFromNode(HtmlNode node)
        {
            var linkNode = QueryNodeAndEnsureSingleResult(node, "a");
            return linkNode.GetAttributeValue("href", "");
        }

        protected static string GetPriceFromNode(HtmlNode node)
        {
            var json = HttpUtility.HtmlDecode(node.GetAttributeValue("data-discount", ""));
            dynamic dyn = JsonSerializer.Deserialize<ExpandoObject>(json); //lazy but easier than looking for the needed key manually
            return dyn.price_after_discount.ToString();
        }

        protected static string GetPublishedStringFromNode(HtmlNode node)
        {
            //TODO: check if some listings do not have a date. E.g. shop-related ones
            try
            {
                return QueryNodeAndEnsureSingleResult(node, ".product_item__date .visible-xs").FirstChild.InnerText;
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

        public class YoulaLocationData
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Radius { get; set; }
            public YoulaLocationType LocationType { get; set; }
            public CityCoordinates Coords { get; set; }
        }
    }

}
