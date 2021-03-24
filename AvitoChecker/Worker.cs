using AvitoChecker.DataStorage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AvitoChecker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AvitoParserService _avito;
        private readonly IDataStorage _storage;

        public Worker(ILogger<Worker> logger, AvitoParserService avito, IDataStorage storage)
        {
            _logger = logger;
            _avito = avito;
            _storage = storage;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                AvitoListing[] listings = await _avito.GetAvitoPhoneListings("pixel 5", 48000, 54000, AvitoListingType.Private);

                var newListings = _storage.FindDifferences(listings);

                _logger.LogInformation($"Found {newListings.Length} new listing(s){(newListings.Any() ? Environment.NewLine : "") }");
                foreach (var item in newListings)
                {
                    _logger.LogInformation($"{item.Name}, {item.Price}, {item.Published}");
                }
                _storage.StoreListings((listings));
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
