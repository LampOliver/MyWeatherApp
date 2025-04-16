using System.Threading;
using System.Threading.Tasks;
using MyWeatherApp.Models;

namespace MyWeatherApp.Services
{
    public interface IWeatherService
    {
        Task<WeatherForecast> GetForecastAsync(CancellationToken cancellationToken);
    }
}
