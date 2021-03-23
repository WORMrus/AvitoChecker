namespace AvitoChecker.DataStorage
{
    public interface IDataStorage
    {
        AvitoListing[] GetListings();
        AvitoListing GetListingByID(string id);
        void StoreListings(AvitoListing[] listings);
        void StoreListing(AvitoListing listing);
        bool RemoveListingByID(string id);
    }
}
