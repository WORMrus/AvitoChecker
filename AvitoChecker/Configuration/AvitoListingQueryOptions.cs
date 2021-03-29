using AvitoChecker.ListingUtilities;

namespace AvitoChecker.Configuration
{
    public class AvitoListingQueryOptions : ListingQueryOptions
    {
        public AvitoListingOptions AvitoOptions { get; set; }
        public class AvitoListingOptions
        {
            public AvitoListingType ListingType { get; set; }
            public string SearchArea { get; set; }
            public string Category { get; set; }
        }
    }
}
