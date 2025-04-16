using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyWeatherApp.Models;

namespace MyWeatherApp.Services
{
    
    public class WeatherEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ForecastTime { get; set; }
        public double Temperature { get; set; }
    }

    public class ResultHandler : IResultHandler
    {
        private readonly ILogger<ResultHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly VaultService _vaultService;
        private TableClient _tableClient;
        private bool _isTableClientInitialized = false;


        private static readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        public ResultHandler(
            ILogger<ResultHandler> logger,
            IConfiguration configuration,
            VaultService vaultService)
        {
            _logger = logger;
            _configuration = configuration;
            _vaultService = vaultService;
        }

        
        private async Task EnsureTableClientInitializedAsync(CancellationToken cancellationToken)
        {
            if (_isTableClientInitialized)
            {
                return;
            }

           
            await _asyncLock.WaitAsync(cancellationToken);
            try
            {
                
                if (_isTableClientInitialized)
                {
                    return;
                }

                _logger.LogInformation("Initialisiere Azure Table Client...");
                string connectionString = null;
                try
                {
                    _logger.LogInformation("Lese Azure Connection String aus Vault...");
                    connectionString = await _vaultService.GetSecretAsync("myweatherapp", "AzureConnectionString");
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        _logger.LogInformation("Azure Connection String erfolgreich aus Vault geladen.");
                    }
                    else
                    {
                        _logger.LogError("Azure Connection String konnte nicht aus Vault (Pfad: secret/myweatherapp, Key: AzureConnectionString) gelesen werden oder ist leer.");
                        return; 
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Lesen des Azure Connection Strings aus Vault.");
                    return; 
                }

                string tableName = _configuration["AzureStorage:TableName"];
                if (string.IsNullOrEmpty(tableName))
                {
                    _logger.LogError("AzureStorage:TableName ist nicht in der Konfiguration gesetzt.");
                    return; 
                }

                try
                {
                    _tableClient = new TableClient(connectionString, tableName);
                    
                    await _tableClient.CreateIfNotExistsAsync(cancellationToken);
                    _logger.LogInformation("Table Client für Tabelle '{TableName}' initialisiert und Tabelle sichergestellt.", tableName);
                    _isTableClientInitialized = true; 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Initialisieren des Table Clients oder Erstellen der Tabelle '{TableName}'.", tableName);
                  
                }
            }
            finally
            {
                
                _asyncLock.Release();
            }
        }


        public async Task HandleResultAsync(WeatherForecast forecast, CancellationToken cancellationToken)
        {
            await EnsureTableClientInitializedAsync(cancellationToken);

            if (!_isTableClientInitialized || _tableClient == null)
            {
                _logger.LogError("ResultHandler kann nicht fortfahren, da der TableClient nicht initialisiert werden konnte.");
                return;
            }

            Console.WriteLine("======================================");
            Console.WriteLine($"Wettervorhersage für Latitude: {forecast?.Latitude ?? 0}, Longitude: {forecast?.Longitude ?? 0}");

            string forecastTime = string.Empty;
            double temperature = 0;

            if (forecast?.Hourly?.Time != null && forecast.Hourly.Time.Any() &&
                forecast.Hourly.Temperature2m != null && forecast.Hourly.Temperature2m.Count == forecast.Hourly.Time.Count)
            {
                forecastTime = forecast.Hourly.Time.First();
                temperature = forecast.Hourly.Temperature2m.First();

                Console.WriteLine("Nächste Vorhersage:");
                Console.WriteLine($"Zeit: {forecastTime}, Temperatur: {temperature}°C");
            }
            else
            {
                _logger.LogWarning("Keine gültigen Vorhersagedaten im Forecast-Objekt gefunden. Speichere nichts.");
                Console.WriteLine("Keine gültigen Vorhersagedaten verfügbar.");
                Console.WriteLine("======================================");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("======================================");
            Console.WriteLine();

            var weatherEntity = new WeatherEntity
            {
                PartitionKey = DateTime.UtcNow.ToString("yyyyMMdd"),
                RowKey = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                ETag = ETag.All,
                Latitude = forecast.Latitude,
                Longitude = forecast.Longitude,
                ForecastTime = forecastTime,
                Temperature = temperature
            };

            try
            {
                
                await _tableClient.AddEntityAsync(weatherEntity, cancellationToken);
                _logger.LogInformation("Wetterdaten wurden in Azure Table Storage gespeichert.");
            }
            catch (RequestFailedException rfEx) when (rfEx.Status == 409)
            {
                _logger.LogWarning(rfEx, "Fehler beim Speichern: Entity mit PartitionKey {PartitionKey} und RowKey {RowKey} existiert bereits.", weatherEntity.PartitionKey, weatherEntity.RowKey);
            }
            catch (RequestFailedException rfEx)
            {
                _logger.LogError(rfEx, "Fehler (RequestFailedException) beim Speichern der Wetterdaten in Azure Table Storage. Status: {Status}, ErrorCode: {ErrorCode}", rfEx.Status, rfEx.ErrorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allgemeiner Fehler beim Speichern der Wetterdaten in Azure Table Storage.");
            }
        }
    }
}