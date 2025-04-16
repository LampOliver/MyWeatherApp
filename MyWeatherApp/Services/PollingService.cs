using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyWeatherApp.Models;

namespace MyWeatherApp.Services
{
    public class PollingService : BackgroundService
    {
        private readonly ILogger<PollingService> _logger;
        private readonly IWeatherService _weatherService;
        private readonly IResultHandler _resultHandler;
        private readonly IConfiguration _configuration;

        public PollingService(ILogger<PollingService> logger, IWeatherService weatherService, IResultHandler resultHandler, IConfiguration configuration)
        {
            _logger = logger;
            _weatherService = weatherService;
            _resultHandler = resultHandler;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Polling Service gestartet: {Time}", DateTimeOffset.Now);

            int intervalSeconds = _configuration.GetValue<int>("Polling:IntervalSeconds", 60);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Abfrage der Wetter API...");
                    WeatherForecast forecast = await _weatherService.GetForecastAsync(stoppingToken);
                    await _resultHandler.HandleResultAsync(forecast, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler während des Polling-Vorgangs oder der Ergebnisverarbeitung.");
                }

                _logger.LogInformation("Warte {Interval} Sekunden bis zur nächsten Abfrage.", intervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Polling Service beendet: {Time}", DateTimeOffset.Now);
        }
    }
}
