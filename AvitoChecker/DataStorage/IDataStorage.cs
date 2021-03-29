using AvitoChecker.ListingUtilities;

namespace AvitoChecker.DataStorage
{
    public interface IDataStorage
    {
        Listing[] GetListings();
        Listing GetListingByID(string id);
        void StoreListings(Listing[] listings);
        void StoreListing(Listing listing);
        bool RemoveListingByID(string id);
        Listing[] FindDifferences(Listing[] listing);
    }
}
