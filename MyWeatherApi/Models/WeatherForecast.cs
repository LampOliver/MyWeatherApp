using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyWeatherApi.Models
{
    public class WeatherForecast
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("hourly")]
        
        public HourlyForecast Hourly { get; set; } = new HourlyForecast();
    }

    public class HourlyForecast
    {
        [JsonPropertyName("time")]
      
        public List<string> Time { get; set; } = new List<string>();

        [JsonPropertyName("temperature2m")]
        public List<double> Temperature2m { get; set; } = new List<double>();
    }
}
