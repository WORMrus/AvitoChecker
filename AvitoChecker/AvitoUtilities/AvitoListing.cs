namespace AvitoChecker
{
    public class AvitoListing
    {
        public string Name { get; init; }
        public int Price { get; init; }
        public string ID { get; init; }
        public string Published { get; init; }
    }

    public enum AvitoListingType
    {
        All = 0,
        Private = 1,
        Company = 2
    }
}
