using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AvitoChecker.DataStorage
{
    class JSONFileStorage : IDataStorage
    {

        private StreamWriter Writer;
        FileStream Fs;

        private List<AvitoListing> _listings;
        private List<AvitoListing> Listings
        {
            get => _listings;
            set
            {
                _listings = value;
                Writer.WriteAsync(JsonSerializer.Serialize(value));
            }
        }

        public string StorageFileLocation { get; init; }

        public JSONFileStorage(string pathToStorage)
        {
            StorageFileLocation = pathToStorage;
            string jsonString;

            Fs = new FileStream(StorageFileLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            Writer = new(Fs);
            using (StreamReader reader = new(Fs))
            {
                jsonString = reader.ReadToEnd();
            }

            jsonString = string.IsNullOrEmpty(jsonString) ? "[]" : jsonString;
            Listings = JsonSerializer.Deserialize<List<AvitoListing>>(jsonString);

        }

        public AvitoListing[] GetListings()
        {
            return Listings.ToArray();
        }

        public AvitoListing GetListingByID(string id)
        {
            return Listings.Where(x => x.ID == id).First();
        }

        public void StoreListings(AvitoListing[] listings)
        {
            Listings = listings.ToList();
        }

        public void StoreListing(AvitoListing listing)
        {
            Listings.Append(listing);
        }

        public bool RemoveListingByID(string id)
        {
            int removed = Listings.RemoveAll(x => x.ID == id);
            switch (removed)
            {
                case 1:
                    return true;
                case 0:
                    return false;
                default:
                    throw new Exception($"More than one listing with ID {id} exist ({removed})");
            }
        }

        ~JSONFileStorage()
        {
            Writer.Dispose();
            Fs.Dispose();
        }
    }
}
