using System;

namespace AvitoChecker.ListingUtilities
{
    public class Listing : IEquatable<Listing>
    {
        public string Name { get; init; }
        public int Price { get; init; }
        public string ID { get; init; }
        public string Published { get; init; }
        public string Link { get; set; }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            return Equals((Listing)obj);

        }

        public bool Equals(Listing other)
        {
            //don't care about Published here if this is the same ID and the rest
            return Name == other.Name && Price == other.Price && ID == other.ID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Price, ID);
        }
    }

    public enum AvitoListingType
    {
        All = 0,
        Private = 1,
        Company = 2
    }
}
