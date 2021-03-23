using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AvitoChecker.DataStorage
{
    class JSONFileStorage : IDataStorage
    {

        private StreamWriter _writer;
        private FileStream _fs;

        private List<AvitoListing> _listings;
        private List<AvitoListing> Listings
        {
            get => _listings;
            set
            {
                overrideFileIfNeeded(value);
                _listings = value;
            }
        }

        public string StorageFileLocation { get; init; }

        public JSONFileStorage(string pathToStorage)
        {
            StorageFileLocation = pathToStorage;
            string jsonString;

            _fs = new FileStream(StorageFileLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            _writer = new(_fs);
            _writer.AutoFlush = true;

            StreamReader reader = new(_fs);
            jsonString = reader.ReadToEnd();

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
            Listings.Add(listing);
            overrideFile(Listings);
        }

        public AvitoListing[] FindDifferences(AvitoListing[] listing)
        {
            return listing.ToArray().Except(Listings).ToArray(); //kinda ugly but I duno
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

        private void overrideFileIfNeeded(List<AvitoListing> listings)
        {
            if (Listings == null || Enumerable.SequenceEqual(Listings, listings))
            {
                return;
            }
            overrideFile(listings);
        }

        private void overrideFile(List<AvitoListing> listings)
        {
            _writer.BaseStream.SetLength(0);//effectively overrides the file
            _writer.WriteAsync(JsonSerializer.Serialize(listings, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            })); //don't care if we return before this finishes
        }


        ~JSONFileStorage()
        {
            _fs.Dispose();
            _writer.Dispose();
        }
    }
}
