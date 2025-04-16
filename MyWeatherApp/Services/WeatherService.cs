using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyWeatherApp.Models;

namespace MyWeatherApp.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherService> _logger;
        private readonly VaultService _vaultService; 

        public WeatherService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<WeatherService> logger,
            VaultService vaultService) 
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _vaultService = vaultService; 
        }

        public async Task<WeatherForecast> GetForecastAsync(CancellationToken cancellationToken)
        {
           
            var token = await GetAuth0TokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Konnte keinen Auth0-Token abrufen.");
            
                throw new Exception("Authentifizierungstoken für Auth0 konnte nicht abgerufen werden.");
            }


      
            string apiUrl = _configuration.GetValue<string>("WeatherApi:Url");
            if (string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogError("Weather API URL ist nicht konfiguriert.");
                throw new Exception("Weather API URL ist nicht konfiguriert.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("Sende Anfrage an die Wetter API: {ApiUrl}", apiUrl);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

     
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Fehler beim Abrufen der Wetterdaten von API. Status Code: {StatusCode}, Grund: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                response.EnsureSuccessStatusCode(); 
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Leere Antwort von der Wetter API erhalten.");
                return new WeatherForecast();
            }

            try
            {
                var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (forecasts == null || forecasts.Length == 0)
                {
                    _logger.LogWarning("Keine Vorhersagedaten in der API-Antwort gefunden oder Deserialisierung fehlgeschlagen.");
                   
                    return new WeatherForecast();
                }
                return forecasts[0]; 
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Fehler beim Deserialisieren der Wetterdaten-Antwort. Inhalt: {ApiContent}", content);
                throw; 
            }
        }

        private async Task<string> GetAuth0TokenAsync(CancellationToken cancellationToken)
        {
            try
            {
      
                string domain = _configuration["Auth0:Domain"];
                string clientId = _configuration["Auth0:ClientId"];
                string audience = _configuration["Auth0:Audience"];

              
                _logger.LogInformation("Lese Auth0 Client Secret aus Vault...");
                string clientSecret = await _vaultService.GetSecretAsync("myweatherapp", "Auth0ClientSecret");

                if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(audience))
                {
                    _logger.LogError("Auth0 Konfiguration (Domain, ClientId, Audience) in appsettings.json ist unvollständig.");
                    return null;
                }
                if (string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("Auth0 Client Secret konnte nicht aus Vault (Pfad: secret/myweatherapp, Key: Auth0ClientSecret) gelesen werden oder ist leer.");
                    return null;
                }
                _logger.LogInformation("Auth0 Client Secret erfolgreich aus Vault geladen.");


                string tokenUrl = $"https://{domain}/oauth/token";

                var tokenRequestBody = new
                {
                    grant_type = "client_credentials",
                    client_id = clientId,
                    client_secret = clientSecret, 
                    audience = audience
                };

              
                _logger.LogInformation("Fordere Auth0 Token an von {TokenUrl} für Audience {Audience}", tokenUrl, audience);
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(tokenRequestBody),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    )
                };

            
                using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    string errorContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Fehler beim Abrufen des Auth0 Tokens. Status: {StatusCode}, Response: {ErrorResponse}", tokenResponse.StatusCode, errorContent);
                    tokenResponse.EnsureSuccessStatusCode(); 
                }


              
                string responseContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                using var jsonDoc = JsonDocument.Parse(responseContent);

                if (jsonDoc.RootElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                {
                    string accessToken = accessTokenElement.GetString();
                    _logger.LogInformation("Auth0 Access Token erfolgreich erhalten.");
                    return accessToken;
                }
                else
                {
                    _logger.LogError("Feld 'access_token' nicht in der Auth0 Token-Antwort gefunden. Antwort: {TokenResponse}", responseContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unerwarteter Fehler beim Abrufen des Auth0 Tokens.");
                return null; 
            }
        }
    }
}