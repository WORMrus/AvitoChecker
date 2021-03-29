using AvitoChecker.Configuration;
using AvitoChecker.DataStorage;
using AvitoChecker.ListingUtilities;
using AvitoChecker.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Http;

namespace AvitoChecker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>()
                            .Configure<ListingQueryOptions>(hostContext.Configuration.GetSection(nameof(ListingQueryOptions)))
                            .Configure<AvitoListingQueryOptions>(hostContext.Configuration.GetSection("ListingQueryOptions"))
                            .Configure<JSONFileStorageOptions>(hostContext.Configuration.GetSection(nameof(JSONFileStorageOptions)))
                            .AddSingleton<IDataStorage, JSONFileStorage>()
                            .AddSingleton<INotificationSender, WindowsNotificationSender>()
                            .AddHttpClient<AvitoParserService>()
                            .ConfigurePrimaryHttpMessageHandler(() =>
                            {
                                var proxy = new WebProxy
                                {
                                    Address = new Uri("http://host.docker.internal:8888"),
                                    BypassProxyOnLocal = false,
                                };
                                return new HttpClientHandler()
                                {
                                    //Proxy = proxy,
                                    //ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                                };
                            });
                });
    }
}
