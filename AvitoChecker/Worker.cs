using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvitoChecker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AvitoParserService _avito;

        public Worker(ILogger<Worker> logger, AvitoParserService avito)
        {
            _logger = logger;
            _avito = avito;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                AvitoListing[] listings = await _avito.GetAvitoPhoneListings("pixel 5", 48000, 54000, AvitoListingType.Private);
                _logger.LogInformation($"Found {listings.Length} listings:\r\n");
                foreach (var item in listings)
                {
                    _logger.LogInformation($"{item.Name}, {item.Price}, {item.Published}");
                }

                await Task.Delay(10000);
            }
        }
    }
}
