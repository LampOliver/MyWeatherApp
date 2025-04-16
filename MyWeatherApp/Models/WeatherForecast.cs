using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyWeatherApp.Models
{
    public class WeatherForecast
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("hourly")]
        public HourlyForecast Hourly { get; set; }
    }

    public class HourlyForecast
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; }

        [JsonPropertyName("temperature2m")]
        public List<double> Temperature2m { get; set; }
    }
}
