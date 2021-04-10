using AngleSharp.Dom;
using AvitoChecker.Configuration;
using AvitoChecker.Retriers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
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
        public override string ListingSource { get; init; }

        public YoulaParserService(HttpClient client, IOptions<YoulaListingQueryOptions> options, IRetrier retrier)
            : base(client, options.Value, "https://youla.ru", retrier)
        {
            _youlaOptions = options.Value.YoulaOptions;
            //Prices are implicitly in 1/100 of a rubble 
            _urlTemplate = _baseUrl + "/all/smartfony-planshety/smartfony?attributes[price][from]={0}00&attributes[price][to]={1}00&attributes[sort_field]=date_published&q={2}";

            _isInstanceInitialized = false;

            ListingSource = "Youla";
        }

        public async Task InitializeInstanceAsync()
        {
            var opts = _youlaOptions;

            YoulaLocationData cityData;

            if (opts.Type == YoulaLocationType.City || opts.Type == YoulaLocationType.PointFromCityName)
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
                    form.Add(new StringContent(cityData.Id), "cityId");
                    break;

                case YoulaLocationType.Point:
                case YoulaLocationType.PointFromCityName:
                    form.Add(new StringContent(cityData.Name), "title");
                    form.Add(new StringContent(cityData.Coords.Latitude.ToString()), "lat");
                    form.Add(new StringContent(cityData.Coords.Longitude.ToString()), "lng");
                    break;

                default:
                    throw new NotImplementedException($"Missing implementation for {cityData.LocationType}");
            }
            var type = cityData.LocationType == YoulaLocationType.PointFromCityName ? "point" : cityData.LocationType.ToString();
            form.Add(new StringContent(type), "type");
            form.Add(new StringContent(cityData.Radius.ToString()), "r");

            var resp = await _client.PostAsync(_baseUrl + "/web-api/geo/save_location", form);
            resp.EnsureSuccessStatusCode();
        }

        //The API returns an array of matching cities sorted by (apparently) the number of listings in each
        //The assumption is that longer city names have fewer listings and that you either have one result
        //Or that the needed city is the 1st one.
        //A possible issue: you need "City", but "Bigger City" exists and it is also bigger. 
        //strictCityNameMatching has to be true to get "City" instead of "Bigger City"
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

            return strictCityNameMatching ? cities.First(c => c.Name.Equals(cityName, StringComparison.OrdinalIgnoreCase)) : cities[0];
        }

        public override async Task<IEnumerable<Listing>> GetListings(CancellationToken cancellationToken)
        {
            if (!_isInstanceInitialized)
            {
                await InitializeInstanceAsync();
            }
            return await GetListingsInternal(cancellationToken);
        }

        protected async Task<IEnumerable<Listing>> GetListingsInternal(CancellationToken cancellationToken)
        {
            string formattedQuery = HttpUtility.UrlEncode(Query);
            var urlToGet = string.Format(_urlTemplate, PriceMin, PriceMax, formattedQuery);

            var doc = await GetListingsDocument(urlToGet, cancellationToken);

            var itemListings = doc.QuerySelectorAll("ul.product_list>li:not([data-banner-type])");

            return HtmlNodesToListings(itemListings);
        }

        protected override Listing HtmlNodeToListing(IElement node)
        {
            string name = GetTitleFromNode(node);
            if (StrictQueryMatching && !name.ToLower().Contains(Query.ToLower()))
            {
                return null;
            }
            return new Listing()
            {
                Name = name,
                ID = node.GetAttribute("data-id"),
                Price = int.Parse(GetPriceFromNode(node)),
                Published = GetPublishedStringFromNode(node),
                Link = _baseUrl + GetLinkFromNode(node),
                Source = ListingSource
            };
        }

        protected static string GetTitleFromNode(IElement node)
        {
            var singleNode = QueryNodeAndEnsureSingleResult(node, ".product_item__title");
            return singleNode.TextContent;
        }

        protected static string GetLinkFromNode(IElement node)
        {
            var linkNode = QueryNodeAndEnsureSingleResult(node, "a");
            return linkNode.GetAttribute("href");
        }

        protected static string GetPriceFromNode(IElement node)
        {
            var json = HttpUtility.HtmlDecode(node.GetAttribute("data-discount"));
            dynamic dyn = JsonSerializer.Deserialize<ExpandoObject>(json); //lazy but easier than looking for the needed key manually
            return dyn.price_after_discount.ToString();
        }

        protected static string GetPublishedStringFromNode(IElement node)
        {
            //TODO: check if some listings do not have a date. E.g. shop-related ones
            try
            {
                return QueryNodeAndEnsureSingleResult(node, ".product_item__date .visible-xs").FirstChild.TextContent;
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
