using AvitoChecker.Configuration;
using AvitoChecker.ListingUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace AvitoChecker.DataStorage
{
    class JSONFileStorage : IDataStorage, IDisposable
    {

        private readonly StreamWriter _writer;
        private readonly FileStream _fs;

        private List<Listing> _listings;
        private List<Listing> Listings
        {
            get => _listings;
            set
            {
                OverrideFileIfNeeded(value);
                _listings = value;
            }
        }

        public string StorageFileLocation { get; init; }

        public JSONFileStorage(IOptions<JSONFileStorageOptions> options)
        {
            StorageFileLocation = options.Value.Path;
            string jsonString;

            _fs = new FileStream(StorageFileLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            _writer = new(_fs);
            _writer.AutoFlush = true;

            StreamReader reader = new(_fs);
            jsonString = reader.ReadToEnd();

            jsonString = string.IsNullOrEmpty(jsonString) ? "[]" : jsonString;
            Listings = JsonSerializer.Deserialize<List<Listing>>(jsonString);
        }

        public Listing[] GetListings() => Listings.ToArray();

        public Listing GetListingByID(string id) => Listings.Where(x => x.ID == id).First();

        public void StoreListings(Listing[] listings) => Listings = listings.ToList();

        public void StoreListing(Listing listing)
        {
            Listings.Add(listing);
            OverrideFile(Listings);
        }

        public Listing[] FindDifferences(Listing[] listing) => listing.ToArray().Except(Listings).ToArray(); //kinda ugly but I duno

        public bool RemoveListingByID(string id)
        {
            int removed = Listings.RemoveAll(x => x.ID == id);
            return removed switch
            {
                1 => true,
                0 => false,
                _ => throw new Exception($"More than one listing with ID {id} exist ({removed})"),
            };
        }

        private void OverrideFileIfNeeded(List<Listing> listings)
        {
            if (Listings == null || Enumerable.SequenceEqual(Listings, listings))
            {
                return;
            }
            OverrideFile(listings);
        }

        private void OverrideFile(List<Listing> listings)
        {
            _writer.BaseStream.SetLength(0);//effectively overrides the file
            _writer.WriteAsync(JsonSerializer.Serialize(listings, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            })); //don't care if we return before this finishes
        }

        public void Dispose()
        {
            ((IDisposable)_writer).Dispose();
        }

        ~JSONFileStorage()
        {
            _fs.Dispose();
            _writer.Dispose();
        }
    }
}
