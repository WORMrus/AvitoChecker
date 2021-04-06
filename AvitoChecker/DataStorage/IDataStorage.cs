using AvitoChecker.ListingUtilities;
using System.Collections.Generic;

namespace AvitoChecker.DataStorage
{
    public interface IDataStorage
    {
        Listing[] GetListings();
        Listing GetListingByID(string id);
        void StoreListings(IEnumerable<Listing> listings);
        void StoreListing(Listing listing);
        bool RemoveListingByID(string id);
        Listing[] FindDifferences(IEnumerable<Listing> listing);
    }
}
