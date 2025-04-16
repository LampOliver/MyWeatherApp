using System;
using System.Threading;      
using System.Threading.Tasks; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyWeatherApp.Services;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace MyWeatherApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<PollingService>();
                    services.AddHttpClient<IWeatherService, WeatherService>();
                    services.AddSingleton<IResultHandler, ResultHandler>();
                    services.AddSingleton<VaultService>();
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var vaultService = host.Services.GetRequiredService<VaultService>();

            int startupDelaySeconds = 10; 
            logger.LogInformation("Warte {DelaySeconds} Sekunden, bevor auf Vault zugegriffen wird...", startupDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(startupDelaySeconds));

           

            logger.LogInformation("Starte den Worker Host...");
            await host.RunAsync();
        }
    }
}