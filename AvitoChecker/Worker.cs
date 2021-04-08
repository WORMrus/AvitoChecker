using AvitoChecker.Configuration;
using AvitoChecker.DataStorage;
using AvitoChecker.Extensions;
using AvitoChecker.ListingUtilities;
using AvitoChecker.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AvitoChecker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEnumerable<IListingGetter> _listingGeters;
        private readonly IDataStorage _storage;
        private readonly INotificationSender _notificationSender;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly WorkerOptions _options;

        public Worker(ILogger<Worker> logger,
                      IEnumerable<IListingGetter> listingGeters,
                      IDataStorage storage,
                      INotificationSender notificationSender,
                      IHostApplicationLifetime lifetime,
                      IOptions<WorkerOptions> options
            )
        {
            _logger = logger;
            _listingGeters = listingGeters;
            _storage = storage;
            _notificationSender = notificationSender;
            _lifetime = lifetime;
            _options = options.Value;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExecuteAsyncInternal(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Worker failed. Action on a fail is {_options.OnException}");

                    switch (_options.OnException)
                    {
                        case WorkerOptions.WorkerBehaviorOnException.StopApp:
                            _logger.LogCritical("Stopping the app");
                            _lifetime.StopApplication();
                            break;
                        case WorkerOptions.WorkerBehaviorOnException.StopWorker:
                            _logger.LogError($"Stopping the worker '{GetType().Name}'");
                            return;
                        case WorkerOptions.WorkerBehaviorOnException.Continue:
                        default:
                            break; //for no explicit reason default is going to be continue
                    }
                }
                await Task.Delay(_options.ListingPollingInterval, stoppingToken);
            }
        }
        protected async Task ExecuteAsyncInternal(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            List<Task<Listing[]>> tasks = new();
            foreach (var getter in _listingGeters)
            {
                tasks.Add(getter.GetListings(stoppingToken));
            }

            var listingTable = await Task.WhenAll(tasks);
            List<Listing> listings = new();

            listingTable.ForEach(serviceListings => listings.AddRange(serviceListings));

            var newListings = _storage.FindDifferences(listings);

            _logger.LogInformation($"Found {newListings.Length} new listing{(newListings.Any() ? "(s)" + Environment.NewLine : "s") }");
            foreach (var item in newListings)
            {
                _logger.LogInformation($"{item.Name}, {item.Price}, {item.Published}");
                _notificationSender.SendNotification(item);
            }

            _storage.StoreListings(listings);
        }
    }
}
