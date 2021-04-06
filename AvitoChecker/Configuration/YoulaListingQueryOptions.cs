namespace AvitoChecker.Configuration
{
    public class YoulaListingQueryOptions : ListingQueryOptions
    {

        public YoulaListingOptions YoulaOptions { get; set; }

        public class YoulaListingOptions
        {
            public CityCoordinates Coordinates { get; set; }
            public int SearchRadius { get; set; }
            public string CityName { get; set; }
            public YoulaLocationType Type { get; set; }
            public bool? StrictCityNameMatching { get; set; }
        }

        public enum YoulaLocationType
        {
            City,
            Point
        }
    }

    public class CityCoordinates
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
