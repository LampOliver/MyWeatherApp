using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWeatherApi.Models;
using System;
using System.Collections.Generic;

namespace MyWeatherApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] 
    public class WeatherController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rnd = new Random();
            double randomTemp = rnd.Next(1, 101); 

            var forecast = new WeatherForecast
            {
                Latitude = 52.52,
                Longitude = 13.41,
                Hourly = new HourlyForecast
                {
                    Time = new List<string> { DateTime.UtcNow.AddHours(1).ToString("o") },
                    Temperature2m = new List<double> { randomTemp }
                }
            };

            return new[] { forecast };
        }
    }
}
