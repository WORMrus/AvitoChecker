using AvitoChecker.DataStorage;
using AvitoChecker.ListingUtilities;
using AvitoChecker.Notifications;
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
        private readonly YoulaParserService _youla;
        private readonly IDataStorage _storage;
        private readonly INotificationSender _notificationSender;

        public Worker(ILogger<Worker> logger, AvitoParserService avito, YoulaParserService youla, IDataStorage storage, INotificationSender notificationSender)
        {
            _logger = logger;
            _youla = youla;
            _avito = avito;
            _storage = storage;
            _notificationSender = notificationSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogWarning("StartAvito");
                Listing[] listings = await _avito.GetAvitoListings();
                _logger.LogWarning("StartYoula");
                Listing[] youlaListings = await _youla.GetAvitoListings();

                var newListings = _storage.FindDifferences(listings);

                _logger.LogInformation($"Found {newListings.Length} new listing{(newListings.Any() ? "(s)" + Environment.NewLine : "s") }");
                foreach (var item in newListings)
                {
                    _logger.LogInformation($"{item.Name}, {item.Price}, {item.Published}");
                    _notificationSender.SendNotification(item);
                }

                _storage.StoreListings((listings));
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
