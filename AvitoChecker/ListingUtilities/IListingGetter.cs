using System.Threading;
using System.Threading.Tasks;

namespace AvitoChecker.ListingUtilities
{
    public interface IListingGetter
    {
        public string ListingSource { get; }
        public Task<Listing[]> GetListings(CancellationToken cancellationToken);
    }
}
